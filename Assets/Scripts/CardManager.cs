using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    //list containing all cards in the game
    [Header("The Full Deck")]
    public List<Card> allCards = new List<Card>();

    //sub-lists for each card type
    [Header("Game Setup")]
    public List<Card> suspectDeck = new List<Card>();
    public List<Card> weaponDeck = new List<Card>();
    public List<Card> roomDeck = new List<Card>();

    //tracks the 3 winning cards
    [Header("The Winning Combo")]
    public List<Card> winningEnvelope = new List<Card>();

    //backup of all cards
    [HideInInspector]
    public List<Card> fullDeck = new List<Card>();

    private void Awake()
    {
        //organises the deck when the game starts
        SortDeck();
        fullDeck = new List<Card>(allCards);
    }

    //interates through allCards and sorts them into sub-decks
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
