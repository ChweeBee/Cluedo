using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;  

    [Header("References")]
    public CardManager cardManager;
    public TurnManager turnManager;
    public SuggestionManager suggestionManager;
    public RoomManager roomManager;

    [Header("Envelope (Secret Solution)")]
    public Card envelopeSuspect;
    public Card envelopeWeapon;
    public Card envelopeRoom;

    // Each player's hand: playerName -> list of cards
    public Dictionary<string, List<Card>> playerHands = new Dictionary<string, List<Card>>();

    // Eliminated players
    public List<string> eliminatedPlayers = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();
        
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
        
        if (suggestionManager == null)
            suggestionManager = FindAnyObjectByType<SuggestionManager>();
        
        if (roomManager == null)
            roomManager = FindAnyObjectByType<RoomManager>();
    }

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        if (turnManager == null)
        {
            Debug.LogError("[GameManager] TurnManager is null!");
            return;
        }
        
        if (turnManager.PlayerCount == 0)
        {
            Debug.LogWarning("[GameManager] No players assigned in TurnManager! Please assign players in the inspector.");
            return;
        }

        Debug.Log("[GameManager] Starting game setup");

        if (cardManager == null)
        {
            Debug.LogError("[GameManager] CardManager is null!");
            return;
        }
        
        cardManager.SortDeck();

        Debug.Log(
            "Suspects: " + cardManager.suspectDeck.Count +
            ", Weapons: " + cardManager.weaponDeck.Count +
            ", Rooms: " + cardManager.roomDeck.Count
        );

        if (
            cardManager.suspectDeck.Count == 0 ||
            cardManager.weaponDeck.Count == 0 ||
            cardManager.roomDeck.Count == 0
        )
        {
            Debug.LogError("[GameManager] A deck is empty - check that all cards have their CardType set correctly!");
            return;
        }

        // Copy decks
        List<Card> suspects = new List<Card>(cardManager.suspectDeck);
        List<Card> weapons = new List<Card>(cardManager.weaponDeck);
        List<Card> rooms = new List<Card>(cardManager.roomDeck);

        // Shuffle each deck
        Shuffle(suspects);
        Shuffle(weapons);
        Shuffle(rooms);

        // Select envelope cards (one from each deck)
        envelopeSuspect = suspects[0];
        suspects.RemoveAt(0);

        envelopeWeapon = weapons[0];
        weapons.RemoveAt(0);

        envelopeRoom = rooms[0];
        rooms.RemoveAt(0);

        Debug.Log(
            "[GameManager] Envelope set: " +
            envelopeSuspect.cardName + ", " +
            envelopeWeapon.cardName + ", " +
            envelopeRoom.cardName
        );

        // Combine remaining cards
        List<Card> remaining = new List<Card>();
        remaining.AddRange(suspects);
        remaining.AddRange(weapons);
        remaining.AddRange(rooms);

        Shuffle(remaining);
        
        Debug.Log($"[GameManager] Remaining cards to deal: {remaining.Count}");

        // Initialize player hands
        int playerCount = turnManager.PlayerCount;
        playerHands.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            if (turnManager.Players[i] == null)
            {
                Debug.LogError($"[GameManager] Player at index {i} is null!");
                continue;
            }
            string playerName = turnManager.Players[i].name;
            playerHands[playerName] = new List<Card>();
            Debug.Log($"[GameManager] Created hand for {playerName}");
        }

        // Deal cards to players
        int index = 0;
        foreach (Card card in remaining)
        {
            if (playerCount == 0) break;
            string playerName = turnManager.Players[index % playerCount].name;
            
            if (playerHands.ContainsKey(playerName))
            {
                playerHands[playerName].Add(card);
                Debug.Log($"[GameManager] Dealt {card.cardName} to {playerName}");
            }
            else
            {
                Debug.LogError($"[GameManager] Player {playerName} not found in playerHands dictionary!");
            }
            index++;
        }

        // Log each player's hand
        foreach (var kvp in playerHands)
        {
            string hand = "";
            foreach (Card card in kvp.Value)
                hand += card.cardName + ", ";
            Debug.Log($"[GameManager] {kvp.Key}'s hand ({kvp.Value.Count} cards): " + hand);
        }

        Debug.Log("[GameManager] Game setup complete");
        
        if (turnManager != null)
            turnManager.StartGame();
        else
            Debug.LogError("[GameManager] TurnManager is null after setup!");
    }

    public void EliminatePlayer(string playerName)
    {
        if (!eliminatedPlayers.Contains(playerName))
        {
            eliminatedPlayers.Add(playerName);
            Debug.Log($"[GameManager] {playerName} has been eliminated!");
            
            /*
            Transform playerTransform = turnManager?.GetPlayerByName(playerName);
            if (playerTransform != null)
            {
                playerTransform.gameObject.SetActive(false);
            }
            */
        }
    }

    public bool IsEliminated(string playerName)
    {
        return eliminatedPlayers.Contains(playerName);
    }

    public bool CheckAccusation(string suspectName, string weaponName, string roomName)
    {
        if (envelopeSuspect == null || envelopeWeapon == null || envelopeRoom == null)
        {
            Debug.LogError("[GameManager] Envelope cards are not set!");
            return false;
        }
        
        return envelopeSuspect.cardName == suspectName &&
               envelopeWeapon.cardName == weaponName &&
               envelopeRoom.cardName == roomName;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}