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

    [SerializeField] Color defaultColour = Color.white;
    [SerializeField] Color blockedColour = Color.red;
    [SerializeField] Color exploredColour = Color.yellow;
    [SerializeField] Color pathColour = new Color(1f, 0.5f, 0f);

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
        SetLabelColour();
    }
    void SetLabelColour()
    {
        if (gridManager == null) { return; }

        Node node = gridManager.GetNode(cords);

        if (node == null) { return; }

        if (!node.walkable)
        {
            label.color = blockedColour;
        }
        else if (node.path)
        {
            label.color = pathColour;
        }
        else if (node.explored)
        {
            label.color = exploredColour;
        }
        else
        {
            label.color = defaultColour;
        }
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
        if (Input.GetKeyDown(KeyCode.C))
        {
            label.enabled = !label.IsActive();
        }
    }
}
