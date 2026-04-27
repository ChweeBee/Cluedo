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

    static bool labelsHidden = false;
    static int lastToggleFrame = -1;

    private void Awake()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        label = GetComponentInChildren<TextMeshPro>();

        DisplayCords();
    }

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

    private void DisplayCords()
    {
        if (!gridManager) { return; }

        cords.x = Mathf.RoundToInt(transform.position.x / gridManager.UnityGridSize);
        cords.y = Mathf.RoundToInt(transform.position.z / gridManager.UnityGridSize);

        label.text = $"{cords.x}, {cords.y}";
    }

    void ToggleLabels()
    {
        if (Input.GetKeyDown(KeyCode.C) && lastToggleFrame != Time.frameCount)
        {
            labelsHidden = !labelsHidden;
            lastToggleFrame = Time.frameCount;
        }
    }
}
