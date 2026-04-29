using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Fallback (used when no save data is loaded)")]
    [SerializeField] List<Transform> players = new List<Transform>();

    GameSaveData data;
    int currentIndex = 0;

    readonly Dictionary<CharacterId, Transform> liveTransforms = new Dictionary<CharacterId, Transform>();

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

    public void NextTurn()
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
