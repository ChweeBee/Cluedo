using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupMenuController : MonoBehaviour
{
    [Header("Player Rows")]
    [SerializeField] PlayerSetupRow rowPrefab;
    [SerializeField] Transform rowContainer;
    [SerializeField] Button addPlayerButton;
    [SerializeField] int minPlayers = 2;
    [SerializeField] int maxPlayers = 8;

    [Header("Save Slot")]
    [SerializeField] TMP_Dropdown saveSlotDropdown;

    [Header("Actions")]
    [SerializeField] Button startGameButton;
    [SerializeField] Button backButton;
    [SerializeField] TextMeshProUGUI statusLabel;

    [Header("Navigation")]
    [SerializeField] GameObject setupPanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] string boardSceneName = "Board";

    readonly List<PlayerSetupRow> rows = new List<PlayerSetupRow>();

    // wires up the setup buttons before the panel is shown.
    void Awake()
    {
        if (addPlayerButton != null) addPlayerButton.onClick.AddListener(OnAddPlayer);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
    }

    // populates the slot dropdown and seeds default player rows when shown.
    void OnEnable()
    {
        RefreshSlotDropdown();

        if (rows.Count == 0)
        {
            CharacterId[] defaults = { CharacterId.MissScarlet, CharacterId.ColonelMustard };
            for (int i = 0; i < minPlayers; i++)
            {
                CharacterId pick = i < defaults.Length ? defaults[i] : NextUnusedCharacter();
                AddRow(pick, false);
            }
        }
        UpdateAddButton();
        SetStatus("");
    }

    // rebuilds the save-slot dropdown options from disk.
    void RefreshSlotDropdown()
    {
        if (saveSlotDropdown == null) return;
        int previous = saveSlotDropdown.value;
        saveSlotDropdown.ClearOptions();
        var options = new List<string>(SaveSystem.SlotCount);
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            options.Add(SaveSystem.DescribeSlot(i));
        saveSlotDropdown.AddOptions(options);
        saveSlotDropdown.value = Mathf.Clamp(previous, 0, SaveSystem.SlotCount - 1);
        saveSlotDropdown.RefreshShownValue();
    }

    // appends a new player row using the next unused character.
    void OnAddPlayer()
    {
        if (rows.Count >= maxPlayers) return;
        AddRow(NextUnusedCharacter(), false);
        UpdateAddButton();
    }

    // returns the first character id not already assigned to a row.
    CharacterId NextUnusedCharacter()
    {
        var used = new HashSet<CharacterId>();
        foreach (var r in rows) used.Add(r.Character);
        foreach (CharacterId id in System.Enum.GetValues(typeof(CharacterId)))
            if (!used.Contains(id)) return id;
        return CharacterId.MissScarlet;
    }

    // instantiates a player row prefab and tracks it.
    void AddRow(CharacterId character, bool isCPU)
    {
        var row = Instantiate(rowPrefab, rowContainer);
        row.Initialize(character, isCPU, this);
        rows.Add(row);
    }

    // removes a player row, blocked once the minimum player count is reached.
    public void RemoveRow(PlayerSetupRow row)
    {
        if (rows.Count <= minPlayers)
        {
            SetStatus($"Need at least {minPlayers} players.");
            return;
        }
        rows.Remove(row);
        Destroy(row.gameObject);
        UpdateAddButton();
    }

    // shifts a row up or down in the visible list, used by the row reorder buttons.
    public void MoveRow(PlayerSetupRow row, int delta)
    {
        int idx = rows.IndexOf(row);
        if (idx < 0) return;
        int target = idx + delta;
        if (target < 0 || target >= rows.Count) return;

        rows.RemoveAt(idx);
        rows.Insert(target, row);
        for (int i = 0; i < rows.Count; i++)
            rows[i].transform.SetSiblingIndex(i);
    }

    // disables the add-player button once the player cap is hit.
    void UpdateAddButton()
    {
        if (addPlayerButton != null) addPlayerButton.interactable = rows.Count < maxPlayers;
    }

    // validates the setup, builds a new save, and loads the board scene.
    void OnStartGame()
    {
        if (rows.Count < minPlayers)
        {
            SetStatus($"Need at least {minPlayers} players.");
            return;
        }

        // detect duplicate character picks before saving anything.
        var seen = new HashSet<CharacterId>();
        foreach (var r in rows)
        {
            if (!seen.Add(r.Character))
            {
                SetStatus($"Duplicate character: {r.Character}. Each player must pick a different one.");
                return;
            }
        }

        var data = new GameSaveData();
        foreach (var r in rows)
            data.players.Add(new PlayerSetup(r.Character, r.IsCPU));
        data.currentTurnIndex = 0;

        int slot = saveSlotDropdown != null ? saveSlotDropdown.value : 0;
        SaveSystem.Save(slot, data);

        var bootstrap = GameBootstrap.EnsureExists();
        bootstrap.SetActive(data);

        SceneManager.LoadScene(boardSceneName);
    }

    // returns from the setup panel to the main menu.
    void OnBack()
    {
        if (setupPanel != null) setupPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // updates the inline status label and mirrors the message to the console.
    void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
        if (!string.IsNullOrEmpty(message)) Debug.Log($"[Setup] {message}");
    }
}
