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

    // spawns every saved player at their start tile and registers them with turnmanager.
    void Awake()
    {
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();

        // bail out cleanly when there is no active save to spawn from.
        var data = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (data == null || !data.IsValid)
        {
            Debug.LogWarning("[PlayerSpawner] No active save; nothing spawned.");
            return;
        }

        // build one transform per saved player and stash it in the spawned dictionary.
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

            // Prefabs carry both a CluedoPlayer and an AIPlayer (derives from CluedoPlayer);
            // set fields on every one so neither holds default values, and hydrate notebook on the data-holder.
            var cluedoComponents = go.GetComponents<CluedoPlayer>();
            CluedoPlayer dataHolder = null;
            foreach (var cp in cluedoComponents)
            {
                if (cp == null) continue;
                cp.character = setup.character;
                cp.isAI = setup.isCPU;
                if (dataHolder == null && !(cp is AIPlayer)) dataHolder = cp;
            }
            if (dataHolder == null && cluedoComponents.Length > 0) dataHolder = cluedoComponents[0];
            if (dataHolder != null) dataHolder.HydrateNotebookFromSave(setup.notebookCheckedCardNames);

            // Set AIPlayer.enabled directly here — TurnManager.RegisterSpawnedPlayers does it too,
            // but at that point TurnManager.data isn't loaded yet, so it sees isAI=false for all players.
            var aiComp = go.GetComponent<AIPlayer>();
            if (aiComp != null) aiComp.enabled = setup.isCPU;

            spawned[setup.character] = go.transform;
        }

        if (turnManager != null) turnManager.RegisterSpawnedPlayers(spawned);
    }
}
