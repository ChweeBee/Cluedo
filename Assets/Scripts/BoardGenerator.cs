using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int width = 24;
    [SerializeField] private int height = 25;
    [SerializeField] private float spacing = 1f;

    // editor menu helper that lays out a fresh grid of tile prefabs.
    [ContextMenu("Generate Board")]
    private void GenerateBoard()
    {
        ClearBoard();

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 spawnPosition = new Vector3(x * spacing, 0f, z * spacing);
                GameObject newTile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity, transform);
                newTile.name = $"Tile_{x}_{z}";
            }
        }
    }

    // editor menu helper that wipes every child tile under this transform.
    [ContextMenu("Clear Board")]
    private void ClearBoard()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}