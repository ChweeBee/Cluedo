using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsSceneController : MonoBehaviour
{
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [Header("Client — Audio")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;

    [Header("Client — Display")]
    [SerializeField] Toggle fullscreenToggle;

    [Header("Client — Idle")]
    [SerializeField] Slider idleSlider;
    [SerializeField] TextMeshProUGUI idleValueLabel;
    [SerializeField] float idleMin = 1f;
    [SerializeField] float idleMax = 30f;

    [Header("Per-Save — Slot picker")]
    [SerializeField] TMP_Dropdown slotDropdown;

    [Header("Per-Save — Roster")]
    [SerializeField] Transform rowContainer;
    [SerializeField] PlayerCpuRow rowPrefab;
    [SerializeField] TextMeshProUGUI emptySlotLabel;

    [Header("Buttons")]
    [SerializeField] Button saveSlotButton;
    [SerializeField] Button backButton;

    ClientSettings settings;

    int currentSlot = -1;
    GameSaveData currentData;
    readonly List<PlayerCpuRow> rows = new List<PlayerCpuRow>();

    void Awake()
    {
        if (saveSlotButton != null) saveSlotButton.onClick.AddListener(OnSaveSlot);
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (slotDropdown != null) slotDropdown.onValueChanged.AddListener(OnSlotChanged);
    }

    void Start()
    {
        BindClientSettings();
        BindSlotPicker();
    }

    // ---------------- Client settings ----------------

    void BindClientSettings()
    {
        settings = ClientSettings.EnsureExists();
        var d = settings.Data;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.SetValueWithoutNotify(d.masterVolume);
            masterVolumeSlider.onValueChanged.AddListener(settings.SetMasterVolume);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.SetValueWithoutNotify(d.musicVolume);
            musicVolumeSlider.onValueChanged.AddListener(settings.SetMusicVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.SetValueWithoutNotify(d.sfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(settings.SetSfxVolume);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(d.fullscreen);
            fullscreenToggle.onValueChanged.AddListener(settings.SetFullscreen);
        }
        if (idleSlider != null)
        {
            idleSlider.minValue = idleMin;
            idleSlider.maxValue = idleMax;
            idleSlider.SetValueWithoutNotify(d.idleAfterSeconds);
            idleSlider.onValueChanged.AddListener(OnIdleChanged);
            UpdateIdleLabel(d.idleAfterSeconds);
        }
    }

    void OnIdleChanged(float v)
    {
        settings.SetIdleAfterSeconds(v);
        UpdateIdleLabel(v);
    }

    void UpdateIdleLabel(float v)
    {
        if (idleValueLabel != null) idleValueLabel.text = $"{v:0.0}s";
    }

    // ---------------- Per-save game settings ----------------

    void BindSlotPicker()
    {
        RefreshSlotDropdown();
        int initial = 0;
        for (int i = 0; i < SaveSystem.SlotCount; i++) { if (SaveSystem.Exists(i)) { initial = i; break; } }
        if (slotDropdown != null) slotDropdown.value = initial;
        LoadSlot(initial);
    }

    void RefreshSlotDropdown()
    {
        if (slotDropdown == null) return;
        int previous = slotDropdown.value;
        slotDropdown.ClearOptions();
        var options = new List<string>(SaveSystem.SlotCount);
        for (int i = 0; i < SaveSystem.SlotCount; i++) options.Add(SaveSystem.DescribeSlot(i));
        slotDropdown.AddOptions(options);
        slotDropdown.value = Mathf.Clamp(previous, 0, SaveSystem.SlotCount - 1);
        slotDropdown.RefreshShownValue();
    }

    void OnSlotChanged(int slot) => LoadSlot(slot);

    void LoadSlot(int slot)
    {
        currentSlot = slot;
        currentData = SaveSystem.Exists(slot) ? SaveSystem.Load(slot) : null;
        Rebuild();
    }

    void Rebuild()
    {
        foreach (var r in rows) if (r != null) Destroy(r.gameObject);
        rows.Clear();

        bool hasData = currentData != null && currentData.IsValid;
        if (emptySlotLabel != null) emptySlotLabel.gameObject.SetActive(!hasData);
        if (saveSlotButton != null) saveSlotButton.interactable = hasData;
        if (!hasData || rowPrefab == null || rowContainer == null) return;

        for (int i = 0; i < currentData.players.Count; i++)
        {
            var row = Instantiate(rowPrefab, rowContainer);
            row.Bind(i, currentData.players[i]);
            rows.Add(row);
        }
    }

    void OnSaveSlot()
    {
        if (currentData == null || currentSlot < 0) return;
        for (int i = 0; i < rows.Count && i < currentData.players.Count; i++)
            currentData.players[i].isCPU = rows[i].IsCPU;
        SaveSystem.Save(currentSlot, currentData);
        RefreshSlotDropdown();
    }

    // ---------------- Exit ----------------

    public void Back()
    {
        if (settings != null) settings.Save();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}

public class PlayerCpuRow : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameLabel;
    [SerializeField] Toggle cpuToggle;

    public bool IsCPU => cpuToggle != null && cpuToggle.isOn;

    public void Bind(int index, PlayerSetup setup)
    {
        if (nameLabel != null) nameLabel.text = $"{index + 1}. {setup.character}";
        if (cpuToggle != null) cpuToggle.SetIsOnWithoutNotify(setup.isCPU);
    }
}
