using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const int SlotCount = 3;

    // builds the on-disk file path for a save slot.
    static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    // returns true if a save file exists for the given slot.
    public static bool Exists(int slot)
    {
        return slot >= 0 && slot < SlotCount && File.Exists(PathFor(slot));
    }

    // serializes a save to disk in pretty-printed json.
    public static void Save(int slot, GameSaveData data)
    {
        if (slot < 0 || slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        if (data == null) throw new ArgumentNullException(nameof(data));

        data.slotIndex = slot;
        data.savedAtUtc = DateTime.UtcNow.ToString("o");
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(PathFor(slot), json);
        Debug.Log($"[SaveSystem] Saved slot {slot + 1} -> {PathFor(slot)}");
    }

    // reads a slot off disk, returning null if missing or corrupt.
    public static GameSaveData Load(int slot)
    {
        if (!Exists(slot)) return null;
        try
        {
            string json = File.ReadAllText(PathFor(slot));
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load slot {slot + 1}: {e.Message}");
            return null;
        }
    }

    // removes the save file for the given slot if it exists.
    public static void Delete(int slot)
    {
        if (Exists(slot)) File.Delete(PathFor(slot));
    }

    // returns a one-line summary string about a save slot for menu display.
    public static string DescribeSlot(int slot)
    {
        if (!Exists(slot)) return $"Slot {slot + 1}: <empty>";
        var data = Load(slot);
        if (data == null) return $"Slot {slot + 1}: <corrupt>";

        string when = data.savedAtUtc;
        if (DateTime.TryParse(when, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            when = parsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return $"Slot {slot + 1}: {data.players.Count} players, turn {data.currentTurnIndex + 1} ({when})";
    }
}
