using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookRow : MonoBehaviour
{
    public TMP_Text label;
    public Toggle checkbox;

    // We add this to store which card this row belongs to
    private string cardName;
    private CardManager manager;

    public void Setup(string name, CardManager m)
    {
        cardName = name;
        label.text = name;
        manager = m;

        // Clear old listeners to avoid double-firing
        checkbox.onValueChanged.RemoveAllListeners();
        checkbox.onValueChanged.AddListener(delegate {
            OnToggleValueChange();
        });
    }

    void OnToggleValueChange()
    {
        // This is where we will eventually save the 'Check' state
        Debug.Log(cardName + " is now " + checkbox.isOn);
    }
}