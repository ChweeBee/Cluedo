using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(50)]
public class Room : MonoBehaviour
{
    public enum RoomType
{
    None,
    Kitchen,
    Ballroom,
    Conservatory,
    BilliardRoom,
    Library,
    Study,
    Hall,
    Lounge,
    DiningRoom
}
    [SerializeField] public RoomType roomType;
    [SerializeField] private List<Vector2Int> roomTiles;
    [SerializeField] private List<Vector2Int> doorTiles;
    [SerializeField] public string roomName;

    private readonly Dictionary<string, Vector2Int> playerSlots = new Dictionary<string, Vector2Int>();
    private readonly List<Vector2Int> interiorTiles = new List<Vector2Int>();
    private static readonly Vector2Int[] Neighbours = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    
    void Start()
    {
        if (string.IsNullOrEmpty(roomName))
            roomName = roomType.ToString();

        EnsureDoorsAreRoomMembers();
        BlockInteriorTiles();
        ForceDoorsWalkable();
        ComputeInteriorTiles();
        ValidateConfiguration();
    }

    void ForceDoorsWalkable()
    {
        if (doorTiles == null) return;
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null) return;

        foreach (Vector2Int door in doorTiles)
        {
            Node node = gridManager.GetNode(door);
            if (node != null) node.walkable = true;
        }
    }

    void EnsureDoorsAreRoomMembers()
    {
        if (doorTiles == null) return;
        if (roomTiles == null) roomTiles = new List<Vector2Int>();

        foreach (Vector2Int door in doorTiles)
        {
            if (!roomTiles.Contains(door)) roomTiles.Add(door);
        }
    }

    void ValidateConfiguration()
    {
        if (doorTiles == null || doorTiles.Count == 0)
        {
            Debug.LogWarning($"[Room] {roomName} has no door tiles configured — players will not be able to enter or exit.");
            return;
        }

        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null) return;

        foreach (Vector2Int door in doorTiles)
        {
            Node node = gridManager.GetNode(door);
            if (node == null)
            {
                Debug.LogWarning($"[Room] {roomName} door tile {door} is outside the grid.");
                continue;
            }
            if (!node.walkable)
            {
                Debug.LogWarning($"[Room] {roomName} door tile {door} is blocked. Make sure no Tile at this coord has 'blocked' = true.");
            }
        }
    }

    void ComputeInteriorTiles()
    {
        interiorTiles.Clear();
        if (roomTiles == null) return;

        foreach (Vector2Int tile in roomTiles)
        {
            if (doorTiles != null && doorTiles.Contains(tile)) continue;

            bool allNeighboursInRoom = true;
            foreach (Vector2Int dir in Neighbours)
            {
                Vector2Int n = tile + dir;
                if (!IsRoomMember(n))
                {
                    allNeighboursInRoom = false;
                    break;
                }
            }

            if (allNeighboursInRoom) interiorTiles.Add(tile);
        }
    }

    bool IsRoomMember(Vector2Int cord)
    {
        return (roomTiles != null && roomTiles.Contains(cord)) ||
               (doorTiles != null && doorTiles.Contains(cord));
    }

    void BlockInteriorTiles()
    {
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null || roomTiles == null) return;

        foreach (Vector2Int cord in roomTiles)
        {
            if (doorTiles != null && doorTiles.Contains(cord)) continue;
            gridManager.BlockNode(cord);
        }
    }

    public bool ContainsTile(Vector2Int cord)
    {
        return roomTiles.Contains(cord);
    }

    public bool IsDoorTile(Vector2Int cord)
    {
        return doorTiles.Contains(cord);
    }

    public bool BelongsToRoom(Vector2Int cord)
    {
        return (roomTiles != null && roomTiles.Contains(cord)) ||
               (doorTiles != null && doorTiles.Contains(cord));
    }

    public IReadOnlyList<Vector2Int> DoorTiles => doorTiles;

    // Returns the assigned interior slot tile for the player (allocating one on first entry).
    public Vector2Int? PlayerEntered(string playerName)
    {
        if (playerSlots.TryGetValue(playerName, out Vector2Int existing))
            return existing;

        HashSet<Vector2Int> taken = new HashSet<Vector2Int>(playerSlots.Values);

        foreach (Vector2Int tile in interiorTiles)
        {
            if (!taken.Contains(tile))
            {
                playerSlots[playerName] = tile;
                return tile;
            }
        }

        if (roomTiles != null)
        {
            foreach (Vector2Int tile in roomTiles)
            {
                if (doorTiles != null && doorTiles.Contains(tile)) continue;
                if (taken.Contains(tile)) continue;
                playerSlots[playerName] = tile;
                return tile;
            }
        }

        return null;
    }

    // Used on scene reload to register a player at the tile they were saved on.
    public void RegisterPlayerAt(string playerName, Vector2Int tile)
    {
        playerSlots[playerName] = tile;
    }

    public void PlayerLeft(string playerName)
    {
        playerSlots.Remove(playerName);
    }

    public List<string> GetPlayersInRoom()
    {
        return new List<string>(playerSlots.Keys);
    }



// scene assistance for rooms
private void OnDrawGizmos()
{
    Gizmos.color = Color.red;

    GridManager gridManager = FindAnyObjectByType<GridManager>();
    if (gridManager == null) return;
    
    foreach (Vector2Int cord in roomTiles)
    {
        Vector3 worldPos = new Vector3(
            cord.x * gridManager.UnityGridSize, 
            0, 
            cord.y * gridManager.UnityGridSize
        );
        Gizmos.DrawCube(worldPos, Vector3.one);
    }

    Gizmos.color = Color.blue;
    foreach (Vector2Int cord in doorTiles)
    {
        Vector3 worldPos = new Vector3(
            cord.x * gridManager.UnityGridSize, 
            0, 
            cord.y * gridManager.UnityGridSize
        );
        Gizmos.DrawCube(worldPos, Vector3.one);
    }
}
}