using System;
using System.IO;
using UnityEngine;

[Serializable]
public class ClientSettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float idleAfterSeconds = 5f;
    public bool fullscreen = true;
}

public class ClientSettings : MonoBehaviour
{
    public static ClientSettings Instance { get; private set; }

    public ClientSettingsData Data { get; private set; } = new ClientSettingsData();

    public event Action Changed;

    static string FilePath => Path.Combine(Application.persistentDataPath, "client_settings.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
        Apply();
    }

    public static ClientSettings EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ClientSettings");
        return go.AddComponent<ClientSettings>();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var loaded = JsonUtility.FromJson<ClientSettingsData>(json);
                if (loaded != null) Data = loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ClientSettings] Load failed, using defaults: {e.Message}");
            Data = new ClientSettingsData();
        }
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ClientSettings] Save failed: {e.Message}");
        }
    }

    public void SetMasterVolume(float v) { Data.masterVolume = Mathf.Clamp01(v); Apply(); }
    public void SetMusicVolume(float v)  { Data.musicVolume  = Mathf.Clamp01(v); Apply(); }
    public void SetSfxVolume(float v)    { Data.sfxVolume    = Mathf.Clamp01(v); Apply(); }
    public void SetIdleAfterSeconds(float s) { Data.idleAfterSeconds = Mathf.Max(0.5f, s); Apply(); }
    public void SetFullscreen(bool on)   { Data.fullscreen = on; Apply(); }

    public void Apply()
    {
        AudioListener.volume = Data.masterVolume;
        if (Screen.fullScreen != Data.fullscreen)
            Screen.fullScreen = Data.fullscreen;
        Changed?.Invoke();
    }
}
