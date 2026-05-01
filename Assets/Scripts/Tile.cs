using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Tile : MonoBehaviour
{
    [SerializeField] bool blocked;
    [SerializeField] Color reachableColor = new Color(1f, 0.6399195f, 0f, 1f);
    [SerializeField] Color doorReachableColor = new Color(0.25f, 0.55f, 1f, 1f);
    [SerializeField] Color pathColor = new Color(1f, 0.2f, 0.2f, 1f);

    public Vector2Int cords;

    GridManager gridManager;
    RoomManager roomManager;
    Renderer cachedRenderer;
    string colorProperty;
    Color baseColor;
    bool isDoor;

    // caches grid coords, the renderer, and whether this tile is a door.
    void Start()
    {
        SetCords();

        // pick the first renderer that has a tintable colour property.
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r.sharedMaterial == null) continue;
            if (r.sharedMaterial.HasProperty("_BaseColor"))
            {
                cachedRenderer = r;
                colorProperty = "_BaseColor";
                baseColor = r.material.GetColor(colorProperty);
                break;
            }
            if (r.sharedMaterial.HasProperty("_Color"))
            {
                cachedRenderer = r;
                colorProperty = "_Color";
                baseColor = r.material.GetColor(colorProperty);
                break;
            }
        }

        // tiles flagged blocked are removed from the walkable graph.
        if(blocked)
        {
            gridManager.BlockNode(cords);
        }

        roomManager = FindAnyObjectByType<RoomManager>();
        isDoor = roomManager != null && roomManager.IsDoorTile(cords);
    }

    // tints the tile each frame based on path and explored flags from pathfinding.
    void Update()
    {
        if (cachedRenderer == null || gridManager == null) return;
        Node node = gridManager.GetNode(cords);
        if (node == null) return;

        Color target = baseColor;
        if (node.path) target = pathColor;
        else if (node.explored) target = isDoor ? doorReachableColor : reachableColor;

        if (cachedRenderer.material.GetColor(colorProperty) != target) cachedRenderer.material.SetColor(colorProperty, target);
    }

    // resolves and stores the grid coordinate based on world position.
    private void SetCords()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        cords = gridManager.GetCoordinatesFromPosition(transform.position);
    }
}
