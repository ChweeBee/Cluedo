using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles distribution, saving, and UI display of cards
/// </summary>

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Envelope envelope;
    public Transform playerHand;
    public Transform PublicHand;
    public GameObject publicHandToggleButton;
    public GameObject cardCanvas;
    public CameraController cameraController;
    public CluedoNotebook notebookManager;

    private bool hasDealt = false;
    private readonly List<Card> publicCards = new List<Card>();

    //checks if any player hand is open on screen
    public bool IsHandVisible => playerHand != null && playerHand.gameObject.activeSelf;

    // wires up scene refs and schedules the deal for the next frame.
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

    // waits one frame so all players are spawned, then deals.
    private IEnumerator DealNextFrame()
    {
        yield return null;
        DealToAllPlayers();
    }

    // returns players in stable save-file order, picking the data-holder over the ai sibling.
    CluedoPlayer[] GetPlayersInTurnOrder()
    {
        CluedoPlayer[] live = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save == null || save.players == null || save.players.Count == 0) return live;

        var byCharacter = new Dictionary<CharacterId, CluedoPlayer>();
        foreach (var cp in live)
        {
            if (cp == null) continue;
            // Prefer the non-AIPlayer "data holder" so hand/notebook lookups are stable.
            bool existing = byCharacter.TryGetValue(cp.character, out var prior);
            if (!existing) byCharacter[cp.character] = cp;
            else if (prior is AIPlayer && !(cp is AIPlayer)) byCharacter[cp.character] = cp;
        }

        var ordered = new List<CluedoPlayer>(save.players.Count);
        foreach (var ps in save.players)
            if (ps != null && byCharacter.TryGetValue(ps.character, out var cp)) ordered.Add(cp);

        return ordered.ToArray();
    }

    // shuffles and distributes the deck once per game, or restores from save if already dealt.
    public void DealToAllPlayers()
    {
        if (hasDealt) { Debug.Log("Cards already dealt"); return; }

        CluedoPlayer[] allPlayers = GetPlayersInTurnOrder();
        if (allPlayers.Length == 0) { Debug.LogError("No Players found in scene"); return; }

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;

        // if a deal already happened, just rebuild from saved data.
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

        // remove the three envelope cards before dealing the rest.
        List<Card> remainingCards = new List<Card>();
        foreach (Card c in cardManager.allCards)
        {
            if (!cardManager.winningEnvelope.Contains(c)) remainingCards.Add(c);
        }

        // fisher-yates shuffle.
        for (int i = 0; i < remainingCards.Count; i++)
        {
            Card temp = remainingCards[i];
            int randomIndex = Random.Range(i, remainingCards.Count);
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

        // split as evenly as possible, leftovers go into the public hand.
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

    // returns true when at least one player has saved hand data.
    bool HasAnySavedHands(GameSaveData save)
    {
        if (save == null || save.players == null) return false;
        foreach (PlayerSetup p in save.players)
        {
            if (p != null && p.handCardNames != null && p.handCardNames.Count > 0) return true;
        }
        return false;
    }

    // rebuilds player hands and the public hand from the save file.
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

    // refills the public-hand area, preferring the save list and falling back to leftovers.
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

    // empties the public hand container.
    void ClearPublicHand()
    {
        publicCards.Clear();
        if (PublicHand == null) return;
        foreach (Transform child in PublicHand) Destroy(child.gameObject);
    }

    // instantiates a single public card under the public hand transform.
    void SpawnPublicCard(Card card)
    {
        if (PublicHand == null || card == null) return;
        GameObject cardObj = Instantiate(card.gameObject, PublicHand);
        cardObj.transform.localScale = Vector3.one;
        cardObj.SetActive(true);
        publicCards.Add(card);
        Debug.Log("Public Card Spawned: " + card.cardName);
    }

    // serializes every player's hand and the public hand back to the save slot.
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

    // looks up a card object in the master deck by display name.
    Card FindCardByName(string name)
    {
        if (cardManager == null || string.IsNullOrEmpty(name)) return null;
        return cardManager.allCards.Find(c => c != null && c.cardName == name);
    }

    // reconciles the live envelope, the scene envelope, and the saved envelope.
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

    // builds and shows the hand panel for the player at the given turn index.
    public void ShowHandByIndex(int index)
    {
        CluedoPlayer[] allPlayers = GetPlayersInTurnOrder();
        if (index < 0 || index >= allPlayers.Length) return;

        playerHand.gameObject.SetActive(true);

        foreach (Transform child in playerHand) Destroy(child.gameObject);

        foreach (Card card in allPlayers[index].hand)
        {
            Instantiate(card, playerHand);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());

        if (notebookManager != null) notebookManager.ShowForPlayer(allPlayers[index]);
    }

    // flips visibility of the public-hand panel.
    public void TogglePublicHand()
    {
        if (PublicHand == null) return;
        bool willShow = !PublicHand.gameObject.activeSelf;
        if (willShow && publicCards.Count == 0) return;
        PublicHand.gameObject.SetActive(willShow);
    }

    // hides the public-hand panel.
    void HidePublicHand()
    {
        if (PublicHand != null) PublicHand.gameObject.SetActive(false);
    }

    // hides the player hand panel and the notebook.
    public void HideAllHands()
    {
        if (playerHand != null) playerHand.gameObject.SetActive(false);
        if (notebookManager != null) notebookManager.Hide();
    }

    // refreshes canvas visibility and listens for hand hotkeys.
    private void Update()
    {
        RefreshCardCanvasVisibility();
        RefreshPublicHandButton();

        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.H)) HideAllHands();

        //hotkeys for toggling player hands
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ShowHandByIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ShowHandByIndex(5);
    }

    // hides the card canvas while the camera is in idle mode.
    void RefreshCardCanvasVisibility()
    {
        if (cardCanvas == null) return;

        bool show = cameraController == null || cameraController.CurrentMode != CameraController.CameraMode.Idle;
        if (cardCanvas.activeSelf != show)
            cardCanvas.SetActive(show);
    }

    // hides the public-hand toggle button when no public cards exist.
    void RefreshPublicHandButton()
    {
        if (publicHandToggleButton == null) return;

        bool show = publicCards.Count > 0;
        if (publicHandToggleButton.activeSelf != show)
            publicHandToggleButton.SetActive(show);
    }
}
