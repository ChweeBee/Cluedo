using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Envelope envelope;
    public Transform playerHand; // This is your main UI container
    public Transform PublicHand;  // Optional UI container for the leftover "table" cards
    public GameObject cardCanvas; // Hidden when the camera enters Idle mode
    public CameraController cameraController;
    public CluedoNotebook notebookManager;

    private bool hasDealt = false;
    private readonly List<Card> publicCards = new List<Card>();

    public bool IsHandVisible => playerHand != null && playerHand.gameObject.activeSelf;

    private void Start()
    {
        if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();
        if (envelope == null) envelope = FindAnyObjectByType<Envelope>();
        if (notebookManager == null)
        {
            notebookManager = FindAnyObjectByType<CluedoNotebook>();
            if (notebookManager == null) notebookManager = gameObject.AddComponent<CluedoNotebook>();
        }
        if (notebookManager != null && notebookManager.cardManager == null)
            notebookManager.cardManager = cardManager;

        StartCoroutine(DealNextFrame());
    }

    private IEnumerator DealNextFrame()
    {
        yield return null;
        DealToAllPlayers();
    }

    CluedoPlayer[] GetPlayersInTurnOrder()
    {
        CluedoPlayer[] live = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save == null || save.players == null || save.players.Count == 0) return live;

        var byCharacter = new Dictionary<CharacterId, CluedoPlayer>();
        foreach (var cp in live)
            if (cp != null) byCharacter[cp.character] = cp;

        var ordered = new List<CluedoPlayer>(save.players.Count);
        foreach (var ps in save.players)
            if (ps != null && byCharacter.TryGetValue(ps.character, out var cp)) ordered.Add(cp);

        return ordered.ToArray();
    }

    public void DealToAllPlayers()
    {
        if (hasDealt) { Debug.Log("Cards already dealt"); return; }

        CluedoPlayer[] allPlayers = GetPlayersInTurnOrder();
        if (allPlayers.Length == 0) { Debug.LogError("No Players found in scene"); return; }

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;

        if (save != null && save.cardsDealt && HasAnySavedHands(save))
        {
            RestoreSavedHands(save, allPlayers);
            hasDealt = true;
            HideAllHands();
            HidePublicHand();
            Debug.Log($"Restored saved hands for {allPlayers.Length} players.");
            return;
        }

        hasDealt = true;
        SyncEnvelopeFromGameState(save);

        List<Card> remainingCards = new List<Card>();
        foreach (Card c in cardManager.allCards)
        {
            if (!cardManager.winningEnvelope.Contains(c)) remainingCards.Add(c);
        }

        for (int i = 0; i < remainingCards.Count; i++)
        {
            Card temp = remainingCards[i];
            int randomIndex = Random.Range(i, remainingCards.Count);
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

        // Standard Cluedo math: deal evenly; any leftover go to the public table.
        // If PublicHand isn't wired, fall back to round-robin so no card is lost.
        int playerCount = allPlayers.Length;
        int cardsPerPlayer = PublicHand != null ? remainingCards.Count / playerCount : remainingCards.Count;
        int totalFairCards = PublicHand != null ? cardsPerPlayer * playerCount : remainingCards.Count;

        ClearPublicHand();

        for (int i = 0; i < remainingCards.Count; i++)
        {
            if (i < totalFairCards)
            {
                allPlayers[i % playerCount].hand.Add(remainingCards[i]);
            }
            else if (PublicHand != null)
            {
                SpawnPublicCard(remainingCards[i]);
            }
        }

        PersistDealtHands(save, allPlayers);

        HideAllHands();
        HidePublicHand();
        Debug.Log($"Dealt {totalFairCards} cards across {allPlayers.Length} players ({publicCards.Count} public).");
    }

    bool HasAnySavedHands(GameSaveData save)
    {
        if (save == null || save.players == null) return false;
        foreach (PlayerSetup p in save.players)
        {
            if (p != null && p.handCardNames != null && p.handCardNames.Count > 0) return true;
        }
        return false;
    }

    void RestoreSavedHands(GameSaveData save, CluedoPlayer[] allPlayers)
    {
        foreach (CluedoPlayer cp in allPlayers) cp.hand.Clear();

        for (int i = 0; i < allPlayers.Length && i < save.players.Count; i++)
        {
            PlayerSetup setup = save.players[i];
            if (setup == null || setup.handCardNames == null) continue;

            foreach (string cardName in setup.handCardNames)
            {
                Card card = FindCardByName(cardName);
                if (card != null) allPlayers[i].hand.Add(card);
            }
        }

        SyncEnvelopeFromGameState(save);
        RebuildPublicHand(save, allPlayers);
    }

    void RebuildPublicHand(GameSaveData save, CluedoPlayer[] allPlayers)
    {
        ClearPublicHand();
        if (PublicHand == null || cardManager == null) return;

        if (save != null && save.publicHandCardNames != null && save.publicHandCardNames.Count > 0)
        {
            foreach (string cardName in save.publicHandCardNames)
            {
                Card card = FindCardByName(cardName);
                if (card != null) SpawnPublicCard(card);
            }
            return;
        }

        // Fallback for older saves: compute leftover from allCards minus envelope minus dealt hands.
        HashSet<Card> dealt = new HashSet<Card>(cardManager.winningEnvelope);
        foreach (CluedoPlayer cp in allPlayers)
        {
            foreach (Card c in cp.hand) dealt.Add(c);
        }

        foreach (Card c in cardManager.allCards)
        {
            if (c != null && !dealt.Contains(c)) SpawnPublicCard(c);
        }
    }

    void ClearPublicHand()
    {
        publicCards.Clear();
        if (PublicHand == null) return;
        foreach (Transform child in PublicHand) Destroy(child.gameObject);
    }

    void SpawnPublicCard(Card card)
    {
        if (PublicHand == null || card == null) return;
        GameObject cardObj = Instantiate(card.gameObject, PublicHand);
        cardObj.transform.localScale = Vector3.one;
        cardObj.SetActive(true);
        publicCards.Add(card);
        Debug.Log("Public Card Spawned: " + card.cardName);
    }

    void PersistDealtHands(GameSaveData save, CluedoPlayer[] allPlayers)
    {
        if (save == null || save.slotIndex < 0) return;

        for (int i = 0; i < allPlayers.Length && i < save.players.Count; i++)
        {
            PlayerSetup setup = save.players[i];
            if (setup == null) continue;

            setup.handCardNames.Clear();
            foreach (Card c in allPlayers[i].hand)
            {
                if (c != null) setup.handCardNames.Add(c.cardName);
            }
        }

        if (save.publicHandCardNames == null) save.publicHandCardNames = new List<string>();
        save.publicHandCardNames.Clear();
        foreach (Card c in publicCards)
        {
            if (c != null) save.publicHandCardNames.Add(c.cardName);
        }

        save.cardsDealt = true;
        SaveSystem.Save(save.slotIndex, save);
    }

    Card FindCardByName(string name)
    {
        if (cardManager == null || string.IsNullOrEmpty(name)) return null;
        return cardManager.allCards.Find(c => c != null && c.cardName == name);
    }

    // Sync cardManager.winningEnvelope to the single source of truth (Envelope component or save.envelope).
    // Envelope.Start runs before this (DealNextFrame yields a frame), so its cards are already chosen
    // and removed from cardManager.allCards.
    void SyncEnvelopeFromGameState(GameSaveData save)
    {
        if (cardManager == null) return;
        cardManager.winningEnvelope.Clear();

        if (envelope != null)
        {
            if (envelope.SuspectCard != null) cardManager.winningEnvelope.Add(envelope.SuspectCard);
            if (envelope.WeaponCard != null) cardManager.winningEnvelope.Add(envelope.WeaponCard);
            if (envelope.RoomCard != null) cardManager.winningEnvelope.Add(envelope.RoomCard);
        }

        // Fallback: pull from save.envelope by name (e.g., if no Envelope component is in the scene yet).
        if (cardManager.winningEnvelope.Count < 3 && save != null && save.envelope != null && save.envelope.IsValid)
        {
            cardManager.winningEnvelope.Clear();
            Card s = FindCardByName(save.envelope.suspectCardName);
            Card w = FindCardByName(save.envelope.weaponCardName);
            Card r = FindCardByName(save.envelope.roomCardName);
            if (s != null) cardManager.winningEnvelope.Add(s);
            if (w != null) cardManager.winningEnvelope.Add(w);
            if (r != null) cardManager.winningEnvelope.Add(r);
        }

        // Ensure save.envelope reflects the chosen cards (covers fresh-game runs where Envelope had no slot to persist to).
        if (save != null && save.slotIndex >= 0 && cardManager.winningEnvelope.Count == 3)
        {
            EnvelopeSolution sol = new EnvelopeSolution
            {
                suspectCardName = cardManager.winningEnvelope[0]?.cardName,
                weaponCardName = cardManager.winningEnvelope[1]?.cardName,
                roomCardName = cardManager.winningEnvelope[2]?.cardName
            };
            if (sol.IsValid) save.envelope = sol;
        }
    }

    // This clears the UI and refills it with a specific player's cards
    public void ShowHandByIndex(int index)
    {
        CluedoPlayer[] allPlayers = GetPlayersInTurnOrder();
        if (index < 0 || index >= allPlayers.Length) return;

        // Ensure the hand is actually visible when we switch
        playerHand.gameObject.SetActive(true);

        // Clear existing cards
        foreach (Transform child in playerHand) Destroy(child.gameObject);

        // Add new cards
        foreach (Card card in allPlayers[index].hand)
        {
            Instantiate(card, playerHand);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());

        if (notebookManager != null) notebookManager.ShowForPlayer(allPlayers[index]);
    }

    // Wire to a UI Button OnClick (no-runtime dropdown). Toggles the public hand.
    // Only opens when there are actually public cards to show.
    public void TogglePublicHand()
    {
        if (PublicHand == null) return;
        bool willShow = !PublicHand.gameObject.activeSelf;
        if (willShow && publicCards.Count == 0) return;
        PublicHand.gameObject.SetActive(willShow);
    }

    void HidePublicHand()
    {
        if (PublicHand != null) PublicHand.gameObject.SetActive(false);
    }

    // Hide-only: H always hides the hand, never reveals it.
    public void HideAllHands()
    {
        if (playerHand != null) playerHand.gameObject.SetActive(false);
        if (notebookManager != null) notebookManager.Hide();
    }

    private void Update()
    {
        RefreshCardCanvasVisibility();

        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.H)) HideAllHands();

        // 1 and 2 now trigger the "Refill UI" logic
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ShowHandByIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ShowHandByIndex(5);
    }

    void RefreshCardCanvasVisibility()
    {
        if (cardCanvas == null) return;

        bool show = cameraController == null || cameraController.CurrentMode != CameraController.CameraMode.Idle;
        if (cardCanvas.activeSelf != show)
            cardCanvas.SetActive(show);
    }
}
