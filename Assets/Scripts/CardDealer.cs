using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Transform playerHand; // Main UI container (single-player display)

    private bool hasDealt = false;

    public void DealHand(int amount)
    {
        while (playerHand.childCount > 0)
        {
            DestroyImmediate(playerHand.GetChild(0).gameObject);
        }

        List<Card> pickedThisTurn = new List<Card>();

        for (int i = 0; i < amount; i++)
        {
            if (cardManager.allCards.Count > 0)
            {
                int randomIndex = Random.Range(0, cardManager.allCards.Count);
                Card cardPrefab = cardManager.allCards[randomIndex];
                if (!pickedThisTurn.Contains(cardPrefab))
                {
                    Instantiate(cardPrefab, playerHand);
                    pickedThisTurn.Add(cardPrefab);
                }
                else
                {
                    i--;
                }
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());
    }

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

        for (int i = 0; i < remainingCards.Count; i++)
        {
            Card temp = remainingCards[i];
            int randomIndex = Random.Range(i, remainingCards.Count);
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

        int playerIndex = 0;
        foreach (Card card in remainingCards)
        {
            allPlayers[playerIndex].hand.Add(card);
            playerIndex = (playerIndex + 1) % allPlayers.Length;
        }

        ShowHandByIndex(0);
        Debug.Log($"Dealt {remainingCards.Count} cards across {allPlayers.Length} players.");
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

    public void ToggleAllHands()
    {
        playerHand.gameObject.SetActive(!playerHand.gameObject.activeSelf);
    }

    private void Update()
    {
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.K)) DealToAllPlayers();
        if (Input.GetKeyDown(KeyCode.H)) ToggleAllHands();

        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ShowHandByIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ShowHandByIndex(5);
    }
}
