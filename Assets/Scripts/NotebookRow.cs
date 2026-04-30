using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookRow : MonoBehaviour
{
    public TMP_Text label;      
    public Toggle checkbox; 

    private string cardName;
    private CluedoNotebook notebookManager;

    public void Setup(string name, CluedoNotebook manager)
    {
        cardName = name;
        label.text = name;
        notebookManager = manager;

        checkbox.onValueChanged.RemoveAllListeners();
        checkbox.onValueChanged.AddListener(OnToggleChanged);
    }

    public void SetState(bool isOn)
    {
        checkbox.SetIsOnWithoutNotify(isOn);
    }

    void OnToggleChanged(bool isOn)
    {
        notebookManager.UpdatePlayerMemory(cardName, isOn);
    }
}