using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotMenuController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[SaveSystem.SlotCount];
    [SerializeField] Button[] loadButtons = new Button[SaveSystem.SlotCount];
    [SerializeField] Button[] deleteButtons = new Button[SaveSystem.SlotCount];

    [Header("Navigation")]
    [SerializeField] Button backButton;
    [SerializeField] GameObject loadPanel;
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] string boardSceneName = "Board";

    void Awake()
    {
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            int slot = i;
            if (slot < loadButtons.Length && loadButtons[slot] != null)
                loadButtons[slot].onClick.AddListener(() => OnLoad(slot));
            if (slot < deleteButtons.Length && deleteButtons[slot] != null)
                deleteButtons[slot].onClick.AddListener(() => OnDelete(slot));
        }
        if (backButton != null) backButton.onClick.AddListener(OnBack);
    }

    void OnEnable()
    {
        Refresh();
    }

    void Refresh()
    {
        for (int i = 0; i < SaveSystem.SlotCount; i++)
        {
            bool exists = SaveSystem.Exists(i);
            if (i < slotLabels.Length && slotLabels[i] != null)
                slotLabels[i].text = SaveSystem.DescribeSlot(i);
            if (i < loadButtons.Length && loadButtons[i] != null)
                loadButtons[i].interactable = exists;
            if (i < deleteButtons.Length && deleteButtons[i] != null)
                deleteButtons[i].interactable = exists;
        }
    }

    void OnLoad(int slot)
    {
        var data = SaveSystem.Load(slot);
        if (data == null || !data.IsValid)
        {
            Debug.LogWarning($"[Load] Slot {slot + 1} is empty or invalid.");
            Refresh();
            return;
        }
        var bootstrap = GameBootstrap.EnsureExists();
        bootstrap.SetActive(data);
        SceneManager.LoadScene(boardSceneName);
    }

    void OnDelete(int slot)
    {
        SaveSystem.Delete(slot);
        Refresh();
    }

    void OnBack()
    {
        if (loadPanel != null) loadPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
}
