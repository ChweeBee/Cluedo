using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Fallback Players")]
    [SerializeField] private List<Transform> players = new List<Transform>();

    private GameSaveData data;
    private int currentIndex = 0;

    private readonly Dictionary<CharacterId, Transform> liveTransforms = new Dictionary<CharacterId, Transform>();
    private readonly Dictionary<Transform, Vector2Int> logicalTiles = new Dictionary<Transform, Vector2Int>();

    public int CurrentIndex => currentIndex;
    public int PlayerCount => data != null ? data.players.Count : players.Count;

    public IReadOnlyList<Transform> Players
    {
        get
        {
            if (data == null) return players;

            List<Transform> list = new List<Transform>();

            foreach (PlayerSetup player in data.players)
            {
                Transform t = ResolveTransform(player.character);
                if (t != null) list.Add(t);
            }

            return list;
        }
    }

    public Transform CurrentPlayer
    {
        get
        {
            if (data != null && data.players.Count > 0)
                return ResolveTransform(data.players[currentIndex].character);

            return players.Count == 0 ? null : players[currentIndex];
        }
    }

    public bool IsCurrentPlayerAI => IsAI(CurrentPlayer);

    // returns true if the given transform belongs to an ai player.
    public bool IsAI(Transform unit)
    {
        if (unit == null) return false;
        var cp = unit.GetComponent<CluedoPlayer>();
        if (cp != null) return cp.isAI;
        return IsAI(NameToCharacter(unit.name));
    }

    // returns true if the given character is configured as ai in the save.
    public bool IsAI(CharacterId character)
    {
        if (data == null) return false;
        var setup = data.players.Find(p => p.character == character);
        return setup != null && setup.isCPU;
    }

    // converts a transform name back into a characterid enum value.
    static CharacterId NameToCharacter(string name)
    {
        return System.Enum.TryParse(name, out CharacterId id) ? id : default;
    }

    // loads the save before playerspawner runs so isai lookups work.
    void Awake()
    {
        LoadSaveDataIfAvailable();
    }

    // re-loads the save and logs the starting player.
    void Start()
    {
        LoadSaveDataIfAvailable();

        if (PlayerCount == 0)
            Debug.LogWarning("[TurnManager] No players assigned.");
        else if (CurrentPlayer != null)
            Debug.Log("[TurnManager] Starting player: " + CurrentPlayer.name);
    }

    // pulls the active save in and clamps the turn index to a valid range.
    private void LoadSaveDataIfAvailable()
    {
        if (
            GameBootstrap.Instance != null &&
            GameBootstrap.Instance.Active != null &&
            GameBootstrap.Instance.Active.IsValid
        )
        {
            data = GameBootstrap.Instance.Active;
            currentIndex = Mathf.Clamp(data.currentTurnIndex, 0, data.players.Count - 1);

            Debug.Log("[TurnManager] Loaded save data.");
        }
    }

    // accepts the live transforms from playerspawner and reconciles them with save data.
    public void RegisterSpawnedPlayers(IReadOnlyDictionary<CharacterId, Transform> spawned)
    {
        // playerspawner runs before our start so the save may not be loaded yet.
        if (data == null) LoadSaveDataIfAvailable();

        liveTransforms.Clear();
        logicalTiles.Clear();

        if (spawned == null) return;

        foreach (var kvp in spawned)
        {
            liveTransforms[kvp.Key] = kvp.Value;
            Transform t = kvp.Value;
            var aiComp = t.GetComponent<AIPlayer>();

            // toggle the aiplayer component based on save flag.
            if (aiComp != null)
            {
                aiComp.enabled = IsAI(kvp.Key);
            }

            if (data != null)
            {
                PlayerSetup setup = data.players.Find(p => p.character == kvp.Key);
                if (setup != null && setup.HasSavedTile)
                    logicalTiles[kvp.Value] = new Vector2Int(setup.tileX, setup.tileY);
            }
        }
    }

    // stores the logical tile for a unit so future lookups bypass world-position math.
    public void SetLogicalTile(Transform unit, Vector2Int tile)
    {
        if (unit == null) return;
        logicalTiles[unit] = tile;
    }

    // tries to read the logical tile for a unit, returns false when unknown.
    public bool TryGetLogicalTile(Transform unit, out Vector2Int tile)
    {
        if (unit != null && logicalTiles.TryGetValue(unit, out tile)) return true;
        tile = default;
        return false;
    }

    // updates the logical tile and persists the new position into the save file.
    public void RecordPlayerTile(CharacterId character, Vector2Int tile)
    {
        if (liveTransforms.TryGetValue(character, out Transform t) && t != null)
            logicalTiles[t] = tile;

        if (data == null) return;

        PlayerSetup setup = data.players.Find(p => p.character == character);

        if (setup == null) return;

        setup.tileX = tile.x;
        setup.tileY = tile.y;

        SaveCurrentData();
    }

    // advances to the next player and writes the new turn index to disk.
    public void NextTurn()
    {
        if (PlayerCount == 0) return;

        currentIndex = (currentIndex + 1) % PlayerCount;

        if (data != null)
        {
            data.currentTurnIndex = currentIndex;
            SaveCurrentData();
        }

        if (CurrentPlayer != null)
            Debug.Log("[TurnManager] Turn -> " + CurrentPlayer.name);
    }

    // bumps the turn index past any eliminated players.
    public void SkipEliminatedPlayers()
    {
        if (GameManager.Instance == null) return;

        int attempts = 0;

        while (
            attempts < PlayerCount &&
            CurrentPlayer != null &&
            GameManager.Instance.IsEliminated(CurrentPlayer.name)
        )
        {
            NextTurn();
            attempts++;
        }
    }

    // persists the current save back to disk if a slot is set.
    private void SaveCurrentData()
    {
        if (data != null && data.slotIndex >= 0)
            SaveSystem.Save(data.slotIndex, data);
    }

    // resolves a characterid to a live transform, falling back to a scene name lookup.
    private Transform ResolveTransform(CharacterId id)
    {
        if (liveTransforms.TryGetValue(id, out Transform t) && t != null)
            return t;

        GameObject go = GameObject.Find(id.ToString());
        return go != null ? go.transform : null;
    }
}