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

    // enforces singleton behaviour and loads persisted settings.
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

    // returns the singleton, creating it on the fly if it doesn't exist.
    public static ClientSettings EnsureExists()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ClientSettings");
        return go.AddComponent<ClientSettings>();
    }

    // reads settings from disk, falling back to defaults on failure.
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

    // writes the in-memory settings out to disk as json.
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

    // updates master volume and re-applies all settings.
    public void SetMasterVolume(float v) { Data.masterVolume = Mathf.Clamp01(v); Apply(); }
    // updates music volume and re-applies all settings.
    public void SetMusicVolume(float v)  { Data.musicVolume  = Mathf.Clamp01(v); Apply(); }
    // updates sfx volume and re-applies all settings.
    public void SetSfxVolume(float v)    { Data.sfxVolume    = Mathf.Clamp01(v); Apply(); }
    // updates the idle camera timeout and re-applies all settings.
    public void SetIdleAfterSeconds(float s) { Data.idleAfterSeconds = Mathf.Max(0.5f, s); Apply(); }
    // toggles fullscreen mode and re-applies all settings.
    public void SetFullscreen(bool on)   { Data.fullscreen = on; Apply(); }

    // pushes the current settings into engine systems and notifies listeners.
    public void Apply()
    {
        AudioListener.volume = Data.masterVolume;
        if (Screen.fullScreen != Data.fullscreen)
            Screen.fullScreen = Data.fullscreen;
        Changed?.Invoke();
    }
}
