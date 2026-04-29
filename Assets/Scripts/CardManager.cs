using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
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
        // DO NOT clear the suspect/weapon/room decks here!
        // Instead, we just make sure allCards is built from them.
        allCards.Clear();
        if (suspectDeck != null) allCards.AddRange(suspectDeck);
        if (weaponDeck != null) allCards.AddRange(weaponDeck);
        if (roomDeck != null) allCards.AddRange(roomDeck);

        Debug.Log($"Deck Sync: {allCards.Count} total cards ready for dealing.");
    }
}