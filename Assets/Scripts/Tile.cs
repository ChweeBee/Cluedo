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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetCords();

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

        if(blocked)
        {
            gridManager.BlockNode(cords);
        }

        roomManager = FindAnyObjectByType<RoomManager>();
        isDoor = roomManager != null && roomManager.IsDoorTile(cords);
    }

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

    private void SetCords()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        int x = (int)transform.position.x;
        int z = (int)transform.position.z;

        cords = new Vector2Int(x / gridManager.UnityGridSize, z / gridManager.UnityGridSize);
    }
}
