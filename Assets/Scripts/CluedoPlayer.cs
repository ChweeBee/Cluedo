using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CluedoPlayer : MonoBehaviour
{
    public bool isAI;
    public CharacterId character;
    public List<Card> hand = new List<Card>();
    public Transform handUI;

    [HideInInspector]
    public HashSet<string> notebookChecked = new HashSet<string>();

    // returns true if a card is ticked off in this player's notebook.
    public bool IsCardChecked(string cardName) => notebookChecked.Contains(cardName);

    // toggles a card in the notebook and writes the change to the save file.
    public void MarkCardChecked(string cardName, bool isChecked)
    {
        if (string.IsNullOrEmpty(cardName)) return;

        bool changed = isChecked
            ? notebookChecked.Add(cardName)
            : notebookChecked.Remove(cardName);

        if (changed) PersistNotebookToSave();
    }

    // restores the notebook state from a list of saved card names.
    public void HydrateNotebookFromSave(IEnumerable<string> cardNames)
    {
        notebookChecked.Clear();
        if (cardNames == null) return;
        foreach (string n in cardNames)
            if (!string.IsNullOrEmpty(n)) notebookChecked.Add(n);
    }

    // pushes the in-memory notebook back into the player's save slot.
    void PersistNotebookToSave()
    {
        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save == null || save.slotIndex < 0) return;

        PlayerSetup setup = save.players.Find(p => p.character == character);
        if (setup == null) return;

        setup.notebookCheckedCardNames.Clear();
        setup.notebookCheckedCardNames.AddRange(notebookChecked);
        SaveSystem.Save(save.slotIndex, save);
    }
}
