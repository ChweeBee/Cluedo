using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
<<<<<<< Updated upstream
=======

    [Header("Notebook UI")]
    public GameObject notebookPanel; // The main yellow panel
    public GameObject notebookRowPrefab;
    public Transform contentArea;


    [Header("The Full Deck")]
>>>>>>> Stashed changes
    public List<Card> allCards = new List<Card>();
    public List<Card> suspectDeck = new List<Card>();
    public List<Card> weaponDeck = new List<Card>();
    public List<Card> roomDeck = new List<Card>();
    public List<Card> winningEnvelope = new List<Card>();

    private void Awake()
    {
        SortDeck();
        CreateNotebook();
    }

    public void SortDeck()
    {
        // DO NOT clear the suspect/weapon/room decks here!
        // Instead, we just make sure allCards is built from them.
        allCards.Clear();
        if (suspectDeck != null) allCards.AddRange(suspectDeck);
        if (weaponDeck != null) allCards.AddRange(weaponDeck);
        if (roomDeck != null) allCards.AddRange(roomDeck);

        Debug.Log($"Deck Sync: {allCards.Count} total cards ready for dealing.");
    }

    public void CreateNotebook()
    {
        // 1. Clear out the container
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        // 2. Spawn the rows
        foreach (Card card in allCards)
        {
            GameObject newRow = Instantiate(notebookRowPrefab, contentArea);

            // Ensure it's not microscopic
            newRow.transform.localScale = Vector3.one;

            // 3. Set the text
            NotebookRow rowScript = newRow.GetComponent<NotebookRow>();
            if (rowScript != null)
            {
                rowScript.label.text = card.cardName;
            }
        }
    }

    public void ToggleNotebook()
    {
        // Check if the panel exists to prevent a "Null Reference" crash
        if (notebookPanel != null)
        {
            // Set it to the opposite of whatever it is now
            notebookPanel.SetActive(!notebookPanel.activeSelf);
        }
    }


}