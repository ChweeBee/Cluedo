using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    public GameSaveData Active { get; private set; }

    // enforces the singleton and survives scene reloads.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // returns the singleton, creating it on the fly if no instance exists yet.
    public static GameBootstrap EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameBootstrap");
        return go.AddComponent<GameBootstrap>();
    }

    // marks a save as the active one for the rest of the session.
    public void SetActive(GameSaveData data)
    {
        Active = data;
    }

    // forgets the active save, used on returning to the main menu.
    public void Clear()
    {
        Active = null;
    }
}
