using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const int SlotCount = 3;

    static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    public static bool Exists(int slot)
    {
        return slot >= 0 && slot < SlotCount && File.Exists(PathFor(slot));
    }

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

    public static void Delete(int slot)
    {
        if (Exists(slot)) File.Delete(PathFor(slot));
    }

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
