using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Fallback (used when no save data is loaded)")]
    [SerializeField] List<Transform> players = new List<Transform>();

    GameSaveData data;
    int currentIndex = 0;
    bool gameEnded = false;

    readonly Dictionary<CharacterId, Transform> liveTransforms = new Dictionary<CharacterId, Transform>();

    DiceManager _diceManager;
    RoomManager _roomManager;
    SuggestionManager _suggestionManager;

    public int CurrentIndex => currentIndex;
    public int PlayerCount => data != null ? data.players.Count : players.Count;

    public IReadOnlyList<Transform> Players
    {
        get
        {
            if (data == null) return players;
            var list = new List<Transform>(data.players.Count);
            foreach (var p in data.players)
            {
                var t = ResolveTransform(p.character);
                if (t != null) list.Add(t);
            }
            return list;
        }
    }

    public PlayerSetup CurrentSetup =>
        (data != null && data.players.Count > 0) ? data.players[currentIndex] : null;

    public Transform CurrentPlayer
    {
        get
        {
            if (data != null && data.players.Count > 0)
                return ResolveTransform(data.players[currentIndex].character);
            return players.Count == 0 ? null : players[currentIndex];
        }
    }

    DiceManager DiceManagerInstance
    {
        get
        {
            if (_diceManager == null) _diceManager = FindAnyObjectByType<DiceManager>();
            return _diceManager;
        }
    }

    RoomManager RoomManagerInstance
    {
        get
        {
            if (_roomManager == null) _roomManager = FindAnyObjectByType<RoomManager>();
            return _roomManager;
        }
    }

    SuggestionManager SuggestionManagerInstance
    {
        get
        {
            if (_suggestionManager == null) _suggestionManager = FindAnyObjectByType<SuggestionManager>();
            return _suggestionManager;
        }
    }

    void Start()
    {
        if (GameBootstrap.Instance != null
            && GameBootstrap.Instance.Active != null
            && GameBootstrap.Instance.Active.IsValid)
        {
            data = GameBootstrap.Instance.Active;
            currentIndex = Mathf.Clamp(data.currentTurnIndex, 0, data.players.Count - 1);

            Debug.Log($"[TurnManager] Loaded {data.players.Count} players (slot {data.slotIndex + 1}). Turn order:");
            for (int i = 0; i < data.players.Count; i++)
            {
                var p = data.players[i];
                string marker = i == currentIndex ? " <-- current" : "";
                Debug.Log($"  {i + 1}. {p.character} {(p.isCPU ? "(CPU)" : "(Human)")}{marker}");
            }
            return;
        }

        if (players.Count == 0)
        {
            Debug.LogWarning("[TurnManager] No save data loaded and no fallback players assigned.");
            return;
        }
        Debug.Log($"[TurnManager] Turn 1: {CurrentPlayer.name}");
    }

    public void RegisterSpawnedPlayers(IReadOnlyDictionary<CharacterId, Transform> spawned)
    {
        liveTransforms.Clear();
        if (spawned == null) return;
        foreach (var kv in spawned) liveTransforms[kv.Key] = kv.Value;
    }

    public void RecordPlayerTile(CharacterId character, Vector2Int tile)
    {
        if (data == null) return;
        var setup = data.players.Find(p => p.character == character);
        if (setup == null) return;

        setup.tileX = tile.x;
        setup.tileY = tile.y;
        if (data.slotIndex >= 0) SaveSystem.Save(data.slotIndex, data);
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

        if (PlayerCount == 0)
        {
            Debug.LogError("[TurnManager] No players in the game");
            EndGame("Nobody");
            return;
        }

        var roster = Players;
        int activePlayers = 0;
        Transform lastActivePlayer = null;

        foreach (Transform player in roster)
        {
            if (player != null
                && GameManager.Instance != null
                && !GameManager.Instance.IsEliminated(player.name))
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
        while (attempts < PlayerCount
            && CurrentPlayer != null
            && GameManager.Instance != null
            && GameManager.Instance.IsEliminated(CurrentPlayer.name))
        {
            AdvanceToNextTurn();
            attempts++;
        }

        if (CurrentPlayer == null
            || GameManager.Instance == null
            || GameManager.Instance.IsEliminated(CurrentPlayer.name))
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

        if (GameManager.Instance != null && GameManager.Instance.IsEliminated(playerName))
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
        int count = PlayerCount;
        if (count == 0) return;

        currentIndex = (currentIndex + 1) % count;

        if (data != null)
        {
            data.currentTurnIndex = currentIndex;
            if (data.slotIndex >= 0) SaveSystem.Save(data.slotIndex, data);

            var p = data.players[currentIndex];
            Debug.Log($"[TurnManager] Turn -> {p.character} {(p.isCPU ? "(CPU)" : "(Human)")}");
        }
        else if (CurrentPlayer != null)
        {
            Debug.Log($"[TurnManager] Turn -> {CurrentPlayer.name}");
        }
    }

    Transform ResolveTransform(CharacterId id)
    {
        if (liveTransforms.TryGetValue(id, out var t) && t != null) return t;
        var go = GameObject.Find(id.ToString());
        return go != null ? go.transform : null;
    }
}
