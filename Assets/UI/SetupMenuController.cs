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

    void Awake()
    {
        if (addPlayerButton != null) addPlayerButton.onClick.AddListener(OnAddPlayer);
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGame);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
    }

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

    void OnAddPlayer()
    {
        if (rows.Count >= maxPlayers) return;
        AddRow(NextUnusedCharacter(), false);
        UpdateAddButton();
    }

    CharacterId NextUnusedCharacter()
    {
        var used = new HashSet<CharacterId>();
        foreach (var r in rows) used.Add(r.Character);
        foreach (CharacterId id in System.Enum.GetValues(typeof(CharacterId)))
            if (!used.Contains(id)) return id;
        return CharacterId.MissScarlet;
    }

    void AddRow(CharacterId character, bool isCPU)
    {
        var row = Instantiate(rowPrefab, rowContainer);
        row.Initialize(character, isCPU, this);
        rows.Add(row);
    }

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

    void UpdateAddButton()
    {
        if (addPlayerButton != null) addPlayerButton.interactable = rows.Count < maxPlayers;
    }

    void OnStartGame()
    {
        if (rows.Count < minPlayers)
        {
            SetStatus($"Need at least {minPlayers} players.");
            return;
        }

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

    void OnBack()
    {
        if (setupPanel != null) setupPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    void SetStatus(string message)
    {
        if (statusLabel != null) statusLabel.text = message;
        if (!string.IsNullOrEmpty(message)) Debug.Log($"[Setup] {message}");
    }
}
