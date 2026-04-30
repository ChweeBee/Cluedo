using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Data;

public class CluedoNotebook : MonoBehaviour
{
    public CardManager CardManager;
    public GameObject notebookRowPrefab;
    public Transform contentArea;
    public GameObject notebookPanel;

    private List<NotebookRow> activeRows = new List<NotebookRow>();
    private int currentPlayerIndex = 0;

    private void Start()
    {
        GenerateNotebook();
        notebookPanel.SetActive(false);
    }

    void GenerateNotebook()
    {
        foreach (Transform child in contentArea) Destroy(child.gameObject);
        activeRows.Clear();

        if (CardManager != null)
        {
            foreach (Card card in CardManager.allCards)
            {
                GameObject obj = Instantiate(notebookRowPrefab, contentArea);
                NotebookRow row = obj.GetComponent<NotebookRow>();
                row.Setup(card.cardName, this);
                activeRows.Add(row);
            }
        }
    }

    public void OpenNotebookForPlayer(int playerIndex)
    {
        currentPlayerIndex = playerIndex;
        CluedoPlayer[] players = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);

        if (playerIndex >= players.Length) return;

        CluedoPlayer activePlayer = players[playerIndex];

        foreach (NotebookRow row in activeRows)
        {
            row.SetState(activePlayer.markedCards.Contains(row.label.text));
        }
    }

    public void UpdatePlayerMemory(string cardName, bool isChecked)
    {
        CluedoPlayer[] players = FindObjectsByType<CluedoPlayer>(FindObjectsSortMode.None);
        CluedoPlayer activePlayer = players[currentPlayerIndex];

        if (isChecked)
        {
            if (!activePlayer.markedCards.Contains(cardName))
            {
                activePlayer.markedCards.Add(cardName);
            }
        }
        
        else
        {
            activePlayer.markedCards.Remove(cardName);
        }
        
    }

    public void TogglePanel()
    {
        notebookPanel.SetActive(!notebookPanel.activeSelf);
    }
}
