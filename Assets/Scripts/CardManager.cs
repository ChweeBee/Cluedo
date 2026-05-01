using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    [Header("The Full Deck")]
    public List<Card> allCards = new List<Card>();

    [Header("Game Setup")]
    public List<Card> suspectDeck = new List<Card>();
    public List<Card> weaponDeck = new List<Card>();
    public List<Card> roomDeck = new List<Card>();

    [Header("The Winning Combo")]
    public List<Card> winningEnvelope = new List<Card>();

    // Snapshot of every card taken before Envelope.Start removes the 3 winners.
    // Use this for anything that needs the full 21 (e.g. the notebook).
    [HideInInspector]
    public List<Card> fullDeck = new List<Card>();

    private void Awake()
    {
        SortDeck();
        fullDeck = new List<Card>(allCards);
    }

    public void SortDeck()
    {
        suspectDeck.Clear();
        weaponDeck.Clear();
        roomDeck.Clear();

        foreach (Card card in allCards)
        {
            if (card.cardType == Card.CardType.Suspect)
                suspectDeck.Add(card);
            else if (card.cardType == Card.CardType.Weapon)
                weaponDeck.Add(card);
            else if (card.cardType == Card.CardType.Room)
                roomDeck.Add(card);
        }

        Debug.Log(
            "Cards sorted. Suspects: " + suspectDeck.Count +
            ", Weapons: " + weaponDeck.Count +
            ", Rooms: " + roomDeck.Count
        );
    }
}
