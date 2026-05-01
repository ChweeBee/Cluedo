using UnityEngine;

[DefaultExecutionOrder(-200)]
public class BoardSceneBootstrap : MonoBehaviour
{
    [Tooltip("If no save is active when this scene loads, fall back to this slot index (0 = Slot 1). Useful for hitting Play directly on the Board scene.")]
    [SerializeField] int fallbackSlot = 0;

    // when the board scene starts without an active save, auto-load the fallback slot.
    void Awake()
    {
        var bootstrap = GameBootstrap.EnsureExists();
        if (bootstrap.Active != null && bootstrap.Active.IsValid) return;

        if (!SaveSystem.Exists(fallbackSlot))
        {
            Debug.LogWarning($"[BoardSceneBootstrap] No active save and slot {fallbackSlot + 1} is empty. Start a game from MainMenu to populate it.");
            return;
        }

        var data = SaveSystem.Load(fallbackSlot);
        if (data == null || !data.IsValid)
        {
            Debug.LogWarning($"[BoardSceneBootstrap] Slot {fallbackSlot + 1} is corrupt or invalid.");
            return;
        }

        bootstrap.SetActive(data);
        Debug.Log($"[BoardSceneBootstrap] Auto-loaded slot {fallbackSlot + 1} for direct Play.");
    }
}
