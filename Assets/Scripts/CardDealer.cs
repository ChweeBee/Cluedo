using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Transform playerHand; // This is your main UI container

    private bool hasDealt = false;

    public void DealToAllPlayers()
    {
        if (hasDealt) { Debug.Log("Cards already dealt"); return; }
        hasDealt = true;

        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        if (allPlayers.Length == 0) { Debug.LogError("No Players found in scene"); return; }

        PickWinners();

        List<Card> remainingCards = new List<Card>();
        foreach (Card c in cardManager.allCards)
        {
            if (!cardManager.winningEnvelope.Contains(c)) remainingCards.Add(c);
        }

        // Shuffle
        for (int i = 0; i < remainingCards.Count; i++)
        {
            Card temp = remainingCards[i];
            int randomIndex = Random.Range(i, remainingCards.Count);
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

        // Round-robin internal data assignment
        int playerIndex = 0;
        foreach (Card card in remainingCards)
        {
            allPlayers[playerIndex].hand.Add(card);
            playerIndex = (playerIndex + 1) % allPlayers.Length;
        }

        // Initially show Player 1's hand
        ShowHandByIndex(0);
        Debug.Log($"Dealt 18 cards across {allPlayers.Length} players.");
    }

    void PickWinners()
    {
        cardManager.winningEnvelope.Clear();
        cardManager.winningEnvelope.Add(cardManager.suspectDeck[Random.Range(0, cardManager.suspectDeck.Count)]);
        cardManager.winningEnvelope.Add(cardManager.weaponDeck[Random.Range(0, cardManager.weaponDeck.Count)]);
        cardManager.winningEnvelope.Add(cardManager.roomDeck[Random.Range(0, cardManager.roomDeck.Count)]);
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

    // THE MASTER TOGGLE: Simply hides or shows the playerHand object
    public void ToggleAllHands()
    {
        playerHand.gameObject.SetActive(!playerHand.gameObject.activeSelf);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) DealToAllPlayers();

        // H now strictly toggles visibility
        if (Input.GetKeyDown(KeyCode.H)) ToggleAllHands();

        // 1 and 2 now trigger the "Refill UI" logic
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
    }
}