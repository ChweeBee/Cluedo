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
    
    // resolves the room name and sets up walls, doors, and interior slots.
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

    // forces every door tile to be walkable so pathfinding can enter the room.
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

    // makes sure every door tile is also recorded as part of the room.
    void EnsureDoorsAreRoomMembers()
    {
        if (doorTiles == null) return;
        if (roomTiles == null) roomTiles = new List<Vector2Int>();

        foreach (Vector2Int door in doorTiles)
        {
            if (!roomTiles.Contains(door)) roomTiles.Add(door);
        }
    }

    // logs warnings if the room has no doors or doors that are blocked.
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

    // collects only those tiles fully surrounded by other room tiles for player slots.
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

    // returns true when a coord is part of either the room or its doors.
    bool IsRoomMember(Vector2Int cord)
    {
        return (roomTiles != null && roomTiles.Contains(cord)) ||
               (doorTiles != null && doorTiles.Contains(cord));
    }

    // blocks every non-door room tile so they don't appear walkable on the grid.
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

    // returns true if the coord is recorded as part of this room.
    public bool ContainsTile(Vector2Int cord)
    {
        return roomTiles.Contains(cord);
    }

    // returns true if the coord is one of this room's doors.
    public bool IsDoorTile(Vector2Int cord)
    {
        return doorTiles.Contains(cord);
    }

    // returns true if the coord is anywhere inside the room or one of its doors.
    public bool BelongsToRoom(Vector2Int cord)
    {
        return (roomTiles != null && roomTiles.Contains(cord)) ||
               (doorTiles != null && doorTiles.Contains(cord));
    }

    public IReadOnlyList<Vector2Int> DoorTiles => doorTiles;

    // assigns and returns the interior tile a newly arrived player should stand on.
    public Vector2Int? PlayerEntered(string playerName)
    {
        if (playerSlots.TryGetValue(playerName, out Vector2Int existing))
            return existing;

        HashSet<Vector2Int> taken = new HashSet<Vector2Int>(playerSlots.Values);

        // prefer a clean interior tile.
        foreach (Vector2Int tile in interiorTiles)
        {
            if (!taken.Contains(tile))
            {
                playerSlots[playerName] = tile;
                return tile;
            }
        }

        // fall back to any non-door room tile if the interior is full.
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

    // restores a player's room slot from a saved tile after a reload.
    public void RegisterPlayerAt(string playerName, Vector2Int tile)
    {
        playerSlots[playerName] = tile;
    }

    // removes a player from this room's slot map when they leave.
    public void PlayerLeft(string playerName)
    {
        playerSlots.Remove(playerName);
    }

    // returns the names of every player currently inside the room.
    public List<string> GetPlayersInRoom()
    {
        return new List<string>(playerSlots.Keys);
    }



// editor gizmo helper that visualises room tiles in red and doors in blue.
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