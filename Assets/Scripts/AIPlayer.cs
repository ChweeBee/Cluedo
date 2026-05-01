using System.Collections;
using UnityEngine;

public class AIPlayer : CluedoPlayer
{
    public float thinkDelay = 0.6f;
    public float postRollDelay = 3f;

    AIStrategy strategy;
    TurnManager turnManager;
    SuggestionManager suggestionManager;
    RoomManager roomManager;
    DiceManager diceManager;
    UnitController unitController;

    public AIStrategy Strategy => strategy;

    // grabs scene managers and tags this player as ai.
    void Awake()
    {
        turnManager = FindAnyObjectByType<TurnManager>();
        suggestionManager = FindAnyObjectByType<SuggestionManager>();
        roomManager = FindAnyObjectByType<RoomManager>();
        diceManager = FindAnyObjectByType<DiceManager>();
        unitController = FindAnyObjectByType<UnitController>();
        isAI = true;
    }

    // alias the data sibling and prepare the strategy when the component first runs.
    void Start()
    {
        if (!enabled) return;
        AliasFromDataSibling();
        EnsureStrategy();
    }

    // re-alias whenever the component is enabled, since data may have changed.
    void OnEnable()
    {
        AliasFromDataSibling();
    }

    // copies fields from the regular cluedoplayer sibling so both share state.
    void AliasFromDataSibling()
    {
        var siblings = GetComponents<CluedoPlayer>();
        foreach (var s in siblings)
        {
            if (s == null || s == this || s is AIPlayer) continue;
            character = s.character;
            isAI = s.isAI;
            hand = s.hand;
            notebookChecked = s.notebookChecked;
            return;
        }
    }

    // lazily creates the strategy helper.
    void EnsureStrategy()
    {
        if (strategy == null) strategy = new AIStrategy(this);
    }

    // public entry point used by turnmanager to start an ai turn.
    public void PerformAITurn()
    {
        if (!enabled) return;
        StartCoroutine(RunTurn());
    }

    // sequences the full ai turn from accusation check through movement to suggestion.
    IEnumerator RunTurn()
    {
        // Wait so CardDealer's one-frame-delayed deal has finished on the very first turn.
        yield return new WaitForSeconds(thinkDelay);

        EnsureStrategy();
        strategy.BootstrapKnowledge();

        // 1. Accuse if we know the answer.
        if (strategy.TryGetAccusation(out Card aS, out Card aW, out Card aR))
        {
            Debug.Log($"[AI] {name} accusing: {aS.cardName} / {aW.cardName} / {aR.cardName}");
            // makeaiaccusation routes through gamemanager.onaccusationmade which ends the game or the turn.
            if (suggestionManager != null) suggestionManager.MakeAIAccusation(aS, aW, aR);
            yield break;
        }

        // 2. Roll the dice (skip if a saved roll was restored mid-turn).
        bool alreadyMoved = GameManager.Instance != null && GameManager.Instance.HasRolledThisTurn && diceManager != null && diceManager.totalResult == 0;

        if (!alreadyMoved)
        {
            if (diceManager != null && diceManager.totalResult <= 0)
            {
                Debug.Log($"[AI] {name} rolling dice");
                diceManager.RollDice();
                yield return new WaitUntil(() => diceManager.totalResult > 0);
                yield return new WaitForSeconds(postRollDelay);
            }

            int budget = diceManager != null ? diceManager.totalResult : 0;

            // 3. Pick the longest reachable target (or closest unreachable as fallback).
            Room target = null;
            Vector2Int? moveTo = strategy.PickBestMoveTarget(budget, out target);

            if (moveTo.HasValue && unitController != null && target != null)
            {
                Debug.Log($"[AI] {name} walking toward {target.roomName} (budget {budget})");
                bool moveDone = false;
                bool started = unitController.RunAIMove(transform, moveTo.Value, budget, () => moveDone = true);
                if (started) yield return new WaitUntil(() => moveDone);
            }
            else
            {
                Debug.Log($"[AI] {name} no reachable target — skipping move");
                if (diceManager != null) diceManager.totalResult = 0;
                if (GameManager.Instance != null) GameManager.Instance.OnPlayerMoved();
            }

            yield return new WaitForSeconds(thinkDelay);
        }

        // 4. If we landed in a room, suggest from it.
        Room currentRoom = roomManager != null ? roomManager.GetPlayerRoom(name) : null;
        bool alreadyActed = GameManager.Instance != null && GameManager.Instance.HasSuggestedOrAccusedThisTurn;
        if (currentRoom != null && !alreadyActed)
        {
            Card roomCard = FindRoomCard(currentRoom.roomName);
            if (roomCard != null)
            {
                strategy.PickSuggestion(roomCard, out Card suspect, out Card weapon);
                Debug.Log($"[AI] {name} suggesting: {suspect.cardName} / {weapon.cardName} / {roomCard.cardName}");

                bool resolved = false;
                if (suggestionManager != null)
                    suggestionManager.MakeAISuggestion(suspect, weapon, roomCard, () => resolved = true);
                else
                    resolved = true;
                yield return new WaitUntil(() => resolved);
                yield return new WaitForSeconds(thinkDelay);
            }
        }

        EndTurn();
    }

    // looks up a room card by display name in the global deck.
    Card FindRoomCard(string roomName)
    {
        var cm = FindAnyObjectByType<CardManager>();
        if (cm == null || cm.roomDeck == null) return null;
        return cm.roomDeck.Find(c => c != null && c.cardName == roomName);
    }

    // hands control back to the gamemanager so the next player can go.
    void EndTurn()
    {
        if (GameManager.Instance != null) GameManager.Instance.EndCurrentTurn();
    }
}
