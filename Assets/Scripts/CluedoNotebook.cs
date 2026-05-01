using UnityEngine;
using System.Collections.Generic;

public class CluedoNotebook : MonoBehaviour
{
    public Transform contentArea;
    public CardManager cardManager;

    // This fills your 21 manual rows with the names from the CardManager
    public void RefreshNotebookNames()
    {
        if (contentArea == null || cardManager == null) return;

        // Get the rows you manually duplicated in the hierarchy
        NotebookRow[] rows = contentArea.GetComponentsInChildren<NotebookRow>(true);
        List<Card> allCards = cardManager.allCards;

        for (int i = 0; i < allCards.Count; i++)
        {
            if (i < rows.Length)
            {
                rows[i].label.text = allCards[i].cardName;
            }
        }
    }
}