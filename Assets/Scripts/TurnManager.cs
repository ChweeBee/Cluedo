using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [SerializeField] List<Transform> players = new List<Transform>();

    int currentIndex = 0;
    bool gameEnded = false;

    DiceManager _diceManager;
    RoomManager _roomManager;
    SuggestionManager _suggestionManager;

    public Transform CurrentPlayer => players.Count == 0 ? null : players[currentIndex];
    public int CurrentIndex => currentIndex;
    public int PlayerCount => players.Count;
    public IReadOnlyList<Transform> Players => players;

    DiceManager DiceManagerInstance
    {
        get
        {
            if (_diceManager == null)
                _diceManager = FindAnyObjectByType<DiceManager>();

            return _diceManager;
        }
    }

    RoomManager RoomManagerInstance
    {
        get
        {
            if (_roomManager == null)
                _roomManager = FindAnyObjectByType<RoomManager>();

            return _roomManager;
        }
    }

    SuggestionManager SuggestionManagerInstance
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
        Debug.Log("[TurnManager] Game started");
        BeginTurn();
    }

    public void EndGame(string winner)
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log("GAME OVER - " + winner + " WINS");
    }

    public void BeginTurn()
    {
        if (gameEnded) return;

        if (players.Count == 0)
        {
            Debug.LogError("[TurnManager] No players in the game");
            EndGame("Nobody");
            return;
        }

        int activePlayers = 0;
        Transform lastActivePlayer = null;

        foreach (Transform player in players)
        {
            if (
                player != null &&
                GameManager.Instance != null &&
                !GameManager.Instance.IsEliminated(player.name)
            )
            {
                activePlayers++;
                lastActivePlayer = player;
            }
        }

        if (activePlayers == 0)
        {
            EndGame("Nobody");
            return;
        }

        if (activePlayers == 1 && lastActivePlayer != null)
        {
            EndGame(lastActivePlayer.name);
            return;
        }

        int attempts = 0;

        while (
            attempts < players.Count &&
            CurrentPlayer != null &&
            GameManager.Instance != null &&
            GameManager.Instance.IsEliminated(CurrentPlayer.name)
        )
        {
            AdvanceToNextTurn();
            attempts++;
        }

        if (
            CurrentPlayer == null ||
            GameManager.Instance == null ||
            GameManager.Instance.IsEliminated(CurrentPlayer.name)
        )
        {
            EndGame("Nobody");
            return;
        }

        if (DiceManagerInstance == null)
        {
            Debug.LogWarning("[TurnManager] DiceManager not found");
            Invoke(nameof(BeginTurn), 0.5f);
            return;
        }

        Debug.Log(CurrentPlayer.name + "'s TURN");

        DiceManagerInstance.RollDice();
    }

    public void OnPlayerMoved()
    {
        if (gameEnded) return;

        if (CurrentPlayer == null)
        {
            Debug.LogError("[TurnManager] CurrentPlayer is null");
            NextTurn();
            return;
        }

        string playerName = CurrentPlayer.name;

        if (
            GameManager.Instance != null &&
            GameManager.Instance.IsEliminated(playerName)
        )
        {
            NextTurn();
            return;
        }

        if (RoomManagerInstance == null)
        {
            Debug.LogError("[TurnManager] RoomManager is missing");
            NextTurn();
            return;
        }

        Room currentRoom = RoomManagerInstance.GetPlayerRoom(playerName);

        if (currentRoom != null)
        {
            if (SuggestionManagerInstance == null)
            {
                Debug.LogError("[TurnManager] SuggestionManager is missing");
                NextTurn();
                return;
            }

            SuggestionManagerInstance.StartSuggestion(playerName, currentRoom);
        }
        else
        {
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