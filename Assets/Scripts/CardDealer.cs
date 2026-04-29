using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Transform playerHand; // This is your main UI container
    public Transform PublicHand;

    private bool hasDealt = false;

    private void Start()
    {
        StartCoroutine(DealNextFrame());
    }

    private IEnumerator DealNextFrame()
    {
        yield return null;
        DealToAllPlayers();
    }

    public void DealToAllPlayers()
    {
        if (hasDealt) return;

        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        if (allPlayers.Length == 0) return;

        hasDealt = true;
        cardManager.SortDeck();
        PickWinners();

        List<Card> remainingCards = new List<Card>();
        foreach (Card c in cardManager.allCards)
        {
            if (!cardManager.winningEnvelope.Contains(c)) remainingCards.Add(c);
        }

        int playerCount = allPlayers.Length;
        int cardsPerPlayer = remainingCards.Count / playerCount;
        int totalFairCards = cardsPerPlayer * playerCount;

        // CLEAR PUBLIC HAND FIRST
        if (PublicHand != null)
        {
            foreach (Transform child in PublicHand) DestroyImmediate(child.gameObject);
        }

        for (int i = 0; i < remainingCards.Count; i++)
        {
            if (i < totalFairCards)
            {
                int pIndex = i % playerCount;
                allPlayers[pIndex].hand.Add(remainingCards[i]);
            }
            else
            {
                // --- THE EXTRAS ---
                if (PublicHand != null)
                {
                    Debug.Log("Spawning Extra Card: " + remainingCards[i].cardName);
                    GameObject cardObj = Instantiate(remainingCards[i].gameObject, PublicHand);
                    cardObj.transform.localScale = Vector3.one;
                    cardObj.SetActive(true); // Ensure it's not starting disabled
                }
                else
                {
                    Debug.LogError("FATAL: PublicHand is empty in the Inspector! Drag the object in!");
                }
            }
        }

        ShowHandByIndex(0);

        if (PublicHand != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(PublicHand.GetComponent<RectTransform>());
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

        save.cardsDealt = true;
        SaveSystem.Save(save.slotIndex, save);
    }

    Card FindCardByName(string name)
    {
        if (cardManager == null || string.IsNullOrEmpty(name)) return null;
        return cardManager.allCards.Find(c => c != null && c.cardName == name);
    }

    void PickWinners()
    {
        cardManager.winningEnvelope.Clear();

        // Check Suspects
        if (cardManager.suspectDeck.Count > 0)
            cardManager.winningEnvelope.Add(cardManager.suspectDeck[Random.Range(0, cardManager.suspectDeck.Count)]);
        else
            Debug.LogError("Suspect Deck is empty! Drag cards into the CardManager in the Inspector.");

        // Check Weapons
        if (cardManager.weaponDeck.Count > 0)
            cardManager.winningEnvelope.Add(cardManager.weaponDeck[Random.Range(0, cardManager.weaponDeck.Count)]);
        else
            Debug.LogError("Weapon Deck is empty!");

        // Check Rooms
        if (cardManager.roomDeck.Count > 0)
            cardManager.winningEnvelope.Add(cardManager.roomDeck[Random.Range(0, cardManager.roomDeck.Count)]);
        else
            Debug.LogError("Room Deck is empty!");
    }

    // This clears the UI and refills it with a specific player's cards
    public void ShowHandByIndex(int index)
    {
        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
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
    }

    // Hide-only: H always hides the hand, never reveals it.
    public void HideAllHands()
    {
        if (playerHand != null) playerHand.gameObject.SetActive(false);
    }

    private void Update()
    {
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
}