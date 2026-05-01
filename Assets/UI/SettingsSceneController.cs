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

    // hooks up the save, back, and slot-changed callbacks.
    void Awake()
    {
        if (saveSlotButton != null) saveSlotButton.onClick.AddListener(OnSaveSlot);
        if (backButton != null) backButton.onClick.AddListener(Back);
        if (slotDropdown != null) slotDropdown.onValueChanged.AddListener(OnSlotChanged);
    }

    // populates client-wide and per-save settings on first show.
    void Start()
    {
        BindClientSettings();
        BindSlotPicker();
    }

    // wires every client-wide control to the persistent settings object.
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

    // forwards idle slider changes to the settings object and label.
    void OnIdleChanged(float v)
    {
        settings.SetIdleAfterSeconds(v);
        UpdateIdleLabel(v);
    }

    // formats the idle slider value for the inline label.
    void UpdateIdleLabel(float v)
    {
        if (idleValueLabel != null) idleValueLabel.text = $"{v:0.0}s";
    }

    // populates the slot picker and selects the first non-empty slot.
    void BindSlotPicker()
    {
        RefreshSlotDropdown();
        int initial = 0;
        for (int i = 0; i < SaveSystem.SlotCount; i++) { if (SaveSystem.Exists(i)) { initial = i; break; } }
        if (slotDropdown != null) slotDropdown.value = initial;
        LoadSlot(initial);
    }

    // rebuilds the dropdown's slot descriptions while preserving the previous selection.
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

    // dropdown selection callback that loads the chosen slot.
    void OnSlotChanged(int slot) => LoadSlot(slot);

    // reads a slot off disk and rebuilds the per-save row list.
    void LoadSlot(int slot)
    {
        currentSlot = slot;
        currentData = SaveSystem.Exists(slot) ? SaveSystem.Load(slot) : null;
        Rebuild();
    }

    // refreshes the per-save row container based on the loaded slot data.
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

    // pushes per-row toggle state back into the slot data and writes it out.
    void OnSaveSlot()
    {
        if (currentData == null || currentSlot < 0) return;
        for (int i = 0; i < rows.Count && i < currentData.players.Count; i++)
            currentData.players[i].isCPU = rows[i].IsCPU;
        SaveSystem.Save(currentSlot, currentData);
        RefreshSlotDropdown();
    }

    // saves client settings and returns to the main menu.
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

    // shows the player's display index and seeds the cpu toggle from save data.
    public void Bind(int index, PlayerSetup setup)
    {
        if (nameLabel != null) nameLabel.text = $"{index + 1}. {setup.character}";
        if (cpuToggle != null) cpuToggle.SetIsOnWithoutNotify(setup.isCPU);
    }
}
