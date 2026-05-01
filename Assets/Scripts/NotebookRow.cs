using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotebookRow : MonoBehaviour
{
    public TMP_Text label;
    public Toggle checkbox;

    // wires this row to a card name and an external toggle callback.
    public void Setup(string cardName, bool isChecked, Action<bool> onChanged)
    {
        if (label != null) label.text = cardName;
        if (checkbox == null) return;

        checkbox.onValueChanged.RemoveAllListeners();
        checkbox.SetIsOnWithoutNotify(isChecked);
        checkbox.onValueChanged.AddListener(v => onChanged?.Invoke(v));
    }
}
