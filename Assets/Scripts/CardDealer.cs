using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;

    [Header("UI References")]
    public Transform playerHand;
    public Transform PublicHand;
    public GameObject cardCanvas;
    public CameraController cameraController;
    public CluedoNotebook notebookManager;
    public Envelope envelope;

    private readonly List<Card> publicCards = new List<Card>();

    public bool IsHandVisible => playerHand != null && playerHand.gameObject.activeSelf;

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

        // Shuffle the remaining cards
        for (int i = 0; i < remainingCards.Count; i++)
        {
            Card temp = remainingCards[i];
            int randomIndex = Random.Range(i, remainingCards.Count);
            remainingCards[i] = remainingCards[randomIndex];
            remainingCards[randomIndex] = temp;
        }

        int playerCount = allPlayers.Length;
        int cardsPerPlayer = remainingCards.Count / playerCount;
        int totalFairCards = cardsPerPlayer * playerCount;

        // Clear existing hands and public cards
        foreach (CluedoPlayer cp in allPlayers) cp.hand.Clear();
        ClearPublicHand();

        for (int i = 0; i < remainingCards.Count; i++)
        {
            if (i < totalFairCards)
            {
                allPlayers[i % playerCount].hand.Add(remainingCards[i]);
            }
            else
            {
                SpawnPublicCard(remainingCards[i]);
            }
        }
        ShowHandByIndex(0);
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

        if (playerHand == null) return;

        playerHand.gameObject.SetActive(true);
        foreach (Transform child in playerHand) Destroy(child.gameObject);

        foreach (Card card in allPlayers[index].hand)
        {
            Instantiate(card, playerHand);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());

        if (notebookManager != null) notebookManager.RefreshNotebookNames();
    }

    public void TogglePublicHand()
    {
        if (PublicHand == null) return;
        bool willShow = !PublicHand.gameObject.activeSelf;
        if (willShow && publicCards.Count == 0) return;
        PublicHand.gameObject.SetActive(willShow);
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
    }

    public void HideAllHands()
    {
        if (playerHand != null) playerHand.gameObject.SetActive(false);
        if (PublicHand != null) PublicHand.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) HideAllHands();
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);

        RefreshCardCanvasVisibility();
    }

    void RefreshCardCanvasVisibility()
    {
        if (cardCanvas == null) return;
        bool show = cameraController == null || cameraController.CurrentMode != CameraController.CameraMode.Idle;
        if (cardCanvas.activeSelf != show)
            cardCanvas.SetActive(show);
    }
}