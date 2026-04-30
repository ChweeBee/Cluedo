using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Transform playerHand;
    public Transform PublicHand;

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

        // Standard Cluedo math: 18 cards / 4 players = 4 each, 2 for the table
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
        cardManager.winningEnvelope.Clear();
        cardManager.winningEnvelope.Add(cardManager.suspectDeck[Random.Range(0, cardManager.suspectDeck.Count)]);
        cardManager.winningEnvelope.Add(cardManager.weaponDeck[Random.Range(0, cardManager.weaponDeck.Count)]);
        cardManager.winningEnvelope.Add(cardManager.roomDeck[Random.Range(0, cardManager.roomDeck.Count)]);
    }

    public void ShowHandByIndex(int index)
    {
        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        if (index < 0 || index >= allPlayers.Length) return;

        playerHand.gameObject.SetActive(true);
        foreach (Transform child in playerHand) Destroy(child.gameObject);
        foreach (Card card in allPlayers[index].hand)
        {
            Instantiate(card, playerHand);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());
    }

    public void HideAllHands()
    {
        if (playerHand != null) playerHand.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) HideAllHands();
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);
    }
}