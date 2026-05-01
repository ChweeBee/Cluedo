using System;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[ExecuteAlways]
public class Labeller : MonoBehaviour
{
    TextMeshPro label;
    public Vector2Int cords = new Vector2Int();
    GridManager gridManager;

    static bool labelsHidden = true;
    static int lastToggleFrame = -1;

    // grabs the gridmanager and label, then writes initial coords.
    private void Awake()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        label = GetComponentInChildren<TextMeshPro>();

        DisplayCords();
    }

    // refreshes label text and listens for the c hotkey toggle.
    private void Update()
    {
        if (!Application.isPlaying)
        {
            label.enabled = true;
        }

        DisplayCords();
        transform.name = cords.ToString();

        ToggleLabels();

        if (Application.isPlaying) label.enabled = !labelsHidden;
    }

    // recomputes the coords from world position and pushes them into the label.
    private void DisplayCords()
    {
        if (!gridManager) { return; }

        cords.x = Mathf.RoundToInt(transform.position.x / gridManager.UnityGridSize);
        cords.y = Mathf.RoundToInt(transform.position.z / gridManager.UnityGridSize);

        label.text = $"{cords.x}, {cords.y}";
    }

    // flips the global label visibility flag when c is pressed.
    void ToggleLabels()
    {
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.C) && lastToggleFrame != Time.frameCount)
        {
            labelsHidden = !labelsHidden;
            lastToggleFrame = Time.frameCount;
        }
    }
}
