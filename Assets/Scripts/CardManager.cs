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

    public void SortDeck()
    {
        suspectDeck.Clear();
        weaponDeck.Clear();
        roomDeck.Clear();

        foreach (Card c in allCards)
        {
            if (c.cardType == Card.CardType.Suspect) suspectDeck.Add(c);
            else if (c.cardType == Card.CardType.Weapon) weaponDeck.Add(c);
            else if (c.cardType == Card.CardType.Room) roomDeck.Add(c);
        }
        Debug.Log("Cards sorted into Suspects, Weapons, and Rooms!");
    }

    private void Start()
    {
        SortDeck();
    }
}
