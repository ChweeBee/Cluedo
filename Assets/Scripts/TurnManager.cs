using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Fallback Players")]
    [SerializeField] private List<Transform> players = new List<Transform>();

    private GameSaveData data;
    private int currentIndex = 0;

    private readonly Dictionary<CharacterId, Transform> liveTransforms = new Dictionary<CharacterId, Transform>();

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

    void Start()
    {
        LoadSaveDataIfAvailable();

        if (PlayerCount == 0)
            Debug.LogWarning("[TurnManager] No players assigned.");
        else if (CurrentPlayer != null)
            Debug.Log("[TurnManager] Starting player: " + CurrentPlayer.name);
    }

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

    public void RegisterSpawnedPlayers(IReadOnlyDictionary<CharacterId, Transform> spawned)
    {
        liveTransforms.Clear();

        if (spawned == null) return;

        foreach (var kvp in spawned)
            liveTransforms[kvp.Key] = kvp.Value;
    }

    public void RecordPlayerTile(CharacterId character, Vector2Int tile)
    {
        if (data == null) return;

        PlayerSetup setup = data.players.Find(p => p.character == character);

        if (setup == null) return;

        setup.tileX = tile.x;
        setup.tileY = tile.y;

        SaveCurrentData();
    }

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

    private void SaveCurrentData()
    {
        if (data != null && data.slotIndex >= 0)
            SaveSystem.Save(data.slotIndex, data);
    }

    private Transform ResolveTransform(CharacterId id)
    {
        if (liveTransforms.TryGetValue(id, out Transform t) && t != null)
            return t;

        GameObject go = GameObject.Find(id.ToString());
        return go != null ? go.transform : null;
    }
}