using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;

public class CluedoPlayer : MonoBehaviour
{
    public TMP_Text label;
    public Toggle checkbox;

    private string cardName;
    private CluedoNotebook notebookManager;

    [Header("Settings")]
    public bool isHuman;
    public string title;

    [Header("Cards")]
    public List<Card> hand = new List<Card>();
<<<<<<< Updated upstream
    public Transform handUI; //only assign UI for human player

   
=======

    [Header("Notebook Memory")]
    public List<string> markedCards = new List<string>();

    public void Setup(string name, CluedoNotebook manager)
    {
        cardName = name;
        label.text = name;
        notebookManager = manager;

        checkbox.onValueChanged.RemoveAllListeners();
        checkbox.onValueChanged.AddListener(OnToggleChanged);
    }

    public void SetState(bool isOn)
    {
        checkbox.SetIsOnWithoutNotify(isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        notebookManager.UpdatePlayerMemory(cardName, isOn);
    }

    public void AddToHand(Card card)
    {
        if (!hand.Contains(card)) hand.Add(card);
    }
>>>>>>> Stashed changes
}