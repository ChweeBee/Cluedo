using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
<<<<<<< Updated upstream
    public Transform playerHand;
    public Transform PublicHand;
=======
    public Envelope envelope;
    public Transform playerHand; 
    public Transform PublicHand; 
    public GameObject cardCanvas; 
    public CameraController cameraController;
    public CluedoNotebook notebookManager;

    private bool hasDealt = false;
    private readonly List<Card> publicCards = new List<Card>();

    public bool IsHandVisible => playerHand != null && playerHand.gameObject.activeSelf;
>>>>>>> Stashed changes

    private void Start()
    {
        // Start a fresh deal every time the game runs
        DealToAllPlayers();
    }

    public void DealToAllPlayers()
    {
        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        if (allPlayers.Length == 0) { Debug.LogError("No Players found!"); return; }

        cardManager.SortDeck();
        PickWinners();

        List<Card> remainingCards = new List<Card>();
        foreach (Card c in cardManager.allCards)
        {
            if (!cardManager.winningEnvelope.Contains(c)) remainingCards.Add(c);
        }

        // Shuffle the 18 remaining cards
        for (int i = 0; i < remainingCards.Count; i++)
        {
            Card temp = remainingCards[i];
            int randomIndex = Random.Range(i, remainingCards.Count);
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

<<<<<<< Updated upstream
        // Standard Cluedo math: 18 cards / 4 players = 4 each, 2 for the table
=======
>>>>>>> Stashed changes
        int playerCount = allPlayers.Length;
        int cardsPerPlayer = remainingCards.Count / playerCount;
        int totalFairCards = cardsPerPlayer * playerCount;

        // Clear existing UI cards
        if (PublicHand != null)
        {
            foreach (Transform child in PublicHand) Destroy(child.gameObject);
        }

        for (int i = 0; i < remainingCards.Count; i++)
        {
            if (i < totalFairCards)
            {
                allPlayers[i % playerCount].hand.Add(remainingCards[i]);
            }
            else
            {
                if (PublicHand != null)
                {
                    GameObject cardObj = Instantiate(remainingCards[i].gameObject, PublicHand);
                    cardObj.transform.localScale = Vector3.one;
                    cardObj.SetActive(true);
                    Debug.Log("Public Card Spawned: " + remainingCards[i].cardName);
                }
            }
        }
        ShowHandByIndex(0);
    }

    public void TogglePublicHand()
    {
        if (PublicHand != null)
            PublicHand.gameObject.SetActive(!PublicHand.gameObject.activeSelf);
    }

    void PickWinners()
    {
<<<<<<< Updated upstream
        cardManager.winningEnvelope.Clear();
        cardManager.winningEnvelope.Add(cardManager.suspectDeck[Random.Range(0, cardManager.suspectDeck.Count)]);
        cardManager.winningEnvelope.Add(cardManager.weaponDeck[Random.Range(0, cardManager.weaponDeck.Count)]);
        cardManager.winningEnvelope.Add(cardManager.roomDeck[Random.Range(0, cardManager.roomDeck.Count)]);
    }

=======
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


>>>>>>> Stashed changes
    public void ShowHandByIndex(int index)
    {
        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        if (index < 0 || index >= allPlayers.Length) return;

<<<<<<< Updated upstream
        playerHand.gameObject.SetActive(true);
        foreach (Transform child in playerHand) Destroy(child.gameObject);
=======
        
        playerHand.gameObject.SetActive(true);

        foreach (Transform child in playerHand) Destroy(child.gameObject);

>>>>>>> Stashed changes
        foreach (Card card in allPlayers[index].hand)
        {
            Instantiate(card, playerHand);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());
    }

<<<<<<< Updated upstream
=======
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

>>>>>>> Stashed changes
    public void HideAllHands()
    {
        if (playerHand != null) playerHand.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) HideAllHands();
<<<<<<< Updated upstream
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);
    }
}
=======

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ShowHandByIndex(0);
            notebookManager.OpenNotebookForPlayer(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ShowHandByIndex(1);
            notebookManager.OpenNotebookForPlayer(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ShowHandByIndex(2);
            notebookManager.OpenNotebookForPlayer(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ShowHandByIndex(3);
            notebookManager.OpenNotebookForPlayer(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            ShowHandByIndex(4);
            notebookManager.OpenNotebookForPlayer(4);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            ShowHandByIndex(5);
            notebookManager.OpenNotebookForPlayer(0);
        }
        }

    void RefreshCardCanvasVisibility()
    {
        if (cardCanvas == null) return;

        bool show = cameraController == null || cameraController.CurrentMode != CameraController.CameraMode.Idle;
        if (cardCanvas.activeSelf != show)
            cardCanvas.SetActive(show);
    }
}
>>>>>>> Stashed changes
