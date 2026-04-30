using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerSpawner : MonoBehaviour
{
    [Serializable]
    public class CharacterEntry
    {
        public CharacterId character;
        public GameObject prefab;
        public Vector2Int startTile;
    }

    [SerializeField] List<CharacterEntry> characters = new List<CharacterEntry>();
    [SerializeField] Transform parent;
    [SerializeField] GridManager gridManager;
    [SerializeField] TurnManager turnManager;

    readonly Dictionary<CharacterId, Transform> spawned = new Dictionary<CharacterId, Transform>();

    public IReadOnlyDictionary<CharacterId, Transform> Spawned => spawned;

    void Awake()
    {
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();

        var data = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (data == null || !data.IsValid)
        {
            Debug.LogWarning("[PlayerSpawner] No active save; nothing spawned.");
            return;
        }

        foreach (var setup in data.players)
        {
            var entry = characters.Find(c => c.character == setup.character);
            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning($"[PlayerSpawner] No prefab/entry mapped for {setup.character}.");
                continue;
            }

            Vector2Int tile = setup.HasSavedTile
                ? new Vector2Int(setup.tileX, setup.tileY)
                : entry.startTile;

            Vector3 worldPos = gridManager != null
                ? gridManager.GetPositionFromCoordinates(tile)
                : new Vector3(tile.x, 0f, tile.y);

            var go = Instantiate(entry.prefab, worldPos, Quaternion.identity, parent);
            go.name = setup.character.ToString();

            var cluedoPlayer = go.GetComponent<CluedoPlayer>();
            if (cluedoPlayer != null)
            {
                cluedoPlayer.character = setup.character;
                cluedoPlayer.isAI = setup.isCPU;
            }

            spawned[setup.character] = go.transform;
        }

        if (turnManager != null) turnManager.RegisterSpawnedPlayers(spawned);
    }
}
