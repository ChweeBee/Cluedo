using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    public GameSaveData Active { get; private set; }

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

    public static GameBootstrap EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("GameBootstrap");
        return go.AddComponent<GameBootstrap>();
    }

    public void SetActive(GameSaveData data)
    {
        Active = data;
    }

    public void Clear()
    {
        Active = null;
    }
}
