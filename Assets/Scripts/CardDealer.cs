using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Transform playerHand; // This is your main UI container

    private bool hasDealt = false;

    public bool IsHandVisible => playerHand != null && playerHand.gameObject.activeSelf;

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
        if (hasDealt) { Debug.Log("Cards already dealt"); return; }

        CluedoPlayer[] allPlayers = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        if (allPlayers.Length == 0) { Debug.LogError("No Players found in scene"); return; }

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;

        if (save != null && save.cardsDealt && HasAnySavedHands(save))
        {
            RestoreSavedHands(save, allPlayers);
            hasDealt = true;
            HideAllHands();
            Debug.Log($"Restored saved hands for {allPlayers.Length} players.");
            return;
        }

        hasDealt = true;
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

        PersistDealtHands(save, allPlayers);

        ShowHandByIndex(0);
        Debug.Log($"Dealt {remainingCards.Count} cards across {allPlayers.Length} players.");
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