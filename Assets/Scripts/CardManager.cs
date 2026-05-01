using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // Make sure this is here for the text to work!

public class CardManager : MonoBehaviour
{
    [Header("Notebook UI")]
    public GameObject notebookPanel;
    public GameObject notebookRowPrefab;
    public Transform contentArea;

    [Header("Decks")]
    public List<Card> allCards = new List<Card>();
    public List<Card> suspectDeck = new List<Card>();
    public List<Card> weaponDeck = new List<Card>();
    public List<Card> roomDeck = new List<Card>();
    public List<Card> winningEnvelope = new List<Card>();

    private void Awake()
    {
        SortDeck();
    }

    public void SortDeck()
    {
        allCards.Clear();
        if (suspectDeck != null) allCards.AddRange(suspectDeck);
        if (weaponDeck != null) allCards.AddRange(weaponDeck);
        if (roomDeck != null) allCards.AddRange(roomDeck);
    }

    public void CreateNotebook()
    {
        foreach (Transform child in contentArea) { Destroy(child.gameObject); }

        foreach (Card card in allCards)
        {
            GameObject newRow = Instantiate(notebookRowPrefab, contentArea);
            newRow.transform.localScale = Vector3.one;

            NotebookRow rowScript = newRow.GetComponent<NotebookRow>();
            if (rowScript != null)
            {
                // Call the Setup function we just created!
                rowScript.Setup(card.cardName, this);
            }
        }
    }

    public void ToggleNotebook()
    {
        if (notebookPanel != null) notebookPanel.SetActive(!notebookPanel.activeSelf);
    }
}