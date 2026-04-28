using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [SerializeField] List<Transform> players = new List<Transform>();

    int currentIndex = 0;

    public Transform CurrentPlayer => players.Count == 0 ? null : players[currentIndex];
    public int CurrentIndex => currentIndex;
    public int PlayerCount => players.Count;
    public IReadOnlyList<Transform> Players => players;

    private DiceManager _diceManager;
    private RoomManager _roomManager;
    private SuggestionManager _suggestionManager;
    
    private bool gameEnded = false;
    
    private DiceManager DiceManagerInstance
    {
        get
        {
            if (_diceManager == null)
                _diceManager = FindAnyObjectByType<DiceManager>();
            return _diceManager;
        }
    }
    
    private RoomManager RoomManagerInstance
    {
        get
        {
            if (_roomManager == null)
                _roomManager = FindAnyObjectByType<RoomManager>();
            return _roomManager;
        }
    }
    
    private SuggestionManager SuggestionManagerInstance
    {
        get
        {
            if (_suggestionManager == null)
                _suggestionManager = FindAnyObjectByType<SuggestionManager>();
            return _suggestionManager;
        }
    }

    void Start()
    {
        Debug.Log("[TurnManager] Initialized");
    }

    public void StartGame()
    {
        Debug.Log("[TurnManager] Game started!");
        BeginTurn();
    }
    
    public void EndGame(string winner)
    {
        gameEnded = true;
        Debug.Log($" GAME OVER - {winner} WINS! ");
        // TODO: Show win screen or reload scene
    }

    public void BeginTurn()
    {
        if (gameEnded) return;
        
        if (players.Count == 0)
        {
            Debug.LogError("[TurnManager] No players in the game!");
            EndGame("Nobody");
            return;
        }
        
        // Check if game is over (0 or 1 players left)
        int activePlayers = 0;
        Transform lastActivePlayer = null;
        
        foreach (Transform p in players)
        {
            if (p != null && !GameManager.Instance.IsEliminated(p.name))
            {
                activePlayers++;
                lastActivePlayer = p;
            }
        }
        
        if (activePlayers == 0)
        {
            Debug.Log("[TurnManager] All players eliminated! No winner!");
            EndGame("Nobody");
            return;
        }
        
        if (activePlayers == 1 && lastActivePlayer != null)
        {
            EndGame(lastActivePlayer.name);
            return;
        }
        
        // Skip eliminated players until we find an active one
        int attempts = 0;
        while (attempts < players.Count && CurrentPlayer != null && GameManager.Instance.IsEliminated(CurrentPlayer.name))
        {
            AdvanceToNextTurn();
            attempts++;
        }
        
        // If we couldn't find an active player, game over
        if (CurrentPlayer == null || GameManager.Instance.IsEliminated(CurrentPlayer.name))
        {
            Debug.Log("[TurnManager] No active players found! Game over.");
            EndGame("Nobody");
            return;
        }
        
        if (DiceManagerInstance == null)
        {
            Debug.LogWarning("[TurnManager] DiceManager not found yet, retrying in 0.5 seconds...");
            Invoke(nameof(BeginTurn), 0.5f);
            return;
        }

        string playerName = CurrentPlayer.name;
        Debug.Log($" {playerName}'s TURN");
        
        DiceManagerInstance.RollDice();
    }

    public void OnPlayerMoved()
    {
        if (gameEnded) return;
        
        if (CurrentPlayer == null)
        {
            Debug.LogError("[TurnManager] CurrentPlayer is null in OnPlayerMoved!");
            NextTurn();
            return;
        }
        
        string playerName = CurrentPlayer.name;
        
        // Check if player was eliminated during their move (shouldn't happen, but safety check)
        if (GameManager.Instance.IsEliminated(playerName))
        {
            Debug.Log($"[TurnManager] {playerName} was eliminated. Skipping turn end.");
            NextTurn();
            return;
        }
        
        if (RoomManagerInstance == null)
        {
            Debug.LogError("[TurnManager] RoomManager is missing!");
            NextTurn();
            return;
        }
        
        Room currentRoom = RoomManagerInstance.GetPlayerRoom(playerName);

        if (currentRoom != null)
        {
            Debug.Log($"[TurnManager] {playerName} is in {currentRoom.roomName}. Triggering suggestion.");
            
            if (SuggestionManagerInstance == null)
            {
                Debug.LogError("[TurnManager] SuggestionManager is missing!");
                NextTurn();
                return;
            }
            
            SuggestionManagerInstance.StartSuggestion(playerName, currentRoom);
        }
        else
        {
            Debug.Log($"[TurnManager] {playerName} is not in a room. Ending turn.");
            NextTurn();
        }
    }

    public void NextTurn()
    {
        if (gameEnded) return;
        AdvanceToNextTurn();
        BeginTurn();
    }

    void AdvanceToNextTurn()
    {
        if (players.Count == 0) return;
        currentIndex = (currentIndex + 1) % players.Count;
    }
}