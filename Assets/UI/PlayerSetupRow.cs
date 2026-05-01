using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSetupRow : MonoBehaviour
{
    [SerializeField] TMP_Dropdown characterDropdown;
    [SerializeField] Toggle cpuToggle;
    [SerializeField] Button moveUpButton;
    [SerializeField] Button moveDownButton;
    [SerializeField] Button removeButton;

    SetupMenuController owner;

    public CharacterId Character => (CharacterId)characterDropdown.value;
    public bool IsCPU => cpuToggle.isOn;

    // wires the row to its owner controller and seeds dropdown values.
    public void Initialize(CharacterId character, bool isCPU, SetupMenuController owner)
    {
        this.owner = owner;

        characterDropdown.ClearOptions();
        var options = new List<string>();
        foreach (CharacterId id in System.Enum.GetValues(typeof(CharacterId)))
            options.Add(PrettyName(id));
        characterDropdown.AddOptions(options);
        characterDropdown.value = (int)character;
        characterDropdown.RefreshShownValue();

        cpuToggle.isOn = isCPU;

        moveUpButton.onClick.AddListener(() => owner.MoveRow(this, -1));
        moveDownButton.onClick.AddListener(() => owner.MoveRow(this, +1));
        removeButton.onClick.AddListener(() => owner.RemoveRow(this));
    }

    // forces the dropdown to a specific character id.
    public void SetCharacter(CharacterId id)
    {
        characterDropdown.value = (int)id;
        characterDropdown.RefreshShownValue();
    }

    // returns a spaced version of the camelcase enum name for display.
    static string PrettyName(CharacterId id)
    {
        string raw = id.ToString();
        var sb = new StringBuilder(raw.Length + 4);
        for (int i = 0; i < raw.Length; i++)
        {
            if (i > 0 && char.IsUpper(raw[i])) sb.Append(' ');
            sb.Append(raw[i]);
        }
        return sb.ToString();
    }
}
