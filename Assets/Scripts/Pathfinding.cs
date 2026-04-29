using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    
    [SerializeField] Vector2Int startCords;
    [SerializeField] Vector2Int targetCords;

    Node startNode;
    Node targetNode;
    Node currentNode;

    Queue<Node> frontier = new Queue<Node>();
    Dictionary<Vector2Int, Node> reached = new Dictionary<Vector2Int, Node>();

    GridManager gridManager;
    RoomManager roomManager;
    Dictionary<Vector2Int, Node> grid = new Dictionary<Vector2Int, Node>();

    // Prioritises right, left, up, down when finding new paths
    Vector2Int[] searchOrder = {Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down};

    public Vector2Int StartCords
    {
        get { return startCords; }
    }

    public Vector2Int TargetCords
    { 
        get { return targetCords; } 
    }

    private void Awake()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager != null)
        {
            grid = gridManager.Grid;
        }
    }

    private void EnsureRoomManager()
    {
        if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();
    }

    List<Vector2Int> GetSeedTiles(Vector2Int origin)
    {
        List<Vector2Int> seeds = new List<Vector2Int>();
        if (!grid.ContainsKey(origin)) return seeds;

        EnsureRoomManager();
        Room room = roomManager != null ? roomManager.GetRoomContainingTile(origin) : null;

        if (room != null && room.DoorTiles != null && room.DoorTiles.Count > 0)
        {
            foreach (Vector2Int door in room.DoorTiles)
            {
                if (grid.ContainsKey(door) && grid[door].walkable && !seeds.Contains(door))
                    seeds.Add(door);
            }
            if (seeds.Count > 0) return seeds;
        }

        if (grid[origin].walkable) seeds.Add(origin);
        return seeds;
    }

    public List<Node> GetNewPath()
    {
        return GetNewPath(startCords);
    }

    public List<Node> GetNewPath(Vector2Int coordinates)
    {
        gridManager.ResetPath();

        BreadthFirstSearch(coordinates);
        return BuildPath();
    }

    void BreadthFirstSearch(Vector2Int coordinates)
    {
        frontier.Clear();
        reached.Clear();

        bool isRunning = true;

        List<Vector2Int> seeds = GetSeedTiles(coordinates);
        if (seeds.Count == 0) return;

        HashSet<Vector2Int> seedSet = new HashSet<Vector2Int>(seeds);

        foreach (Vector2Int s in seeds)
        {
            if (reached.ContainsKey(s)) continue;
            frontier.Enqueue(grid[s]);
            reached.Add(s, grid[s]);
        }

        while (frontier.Count > 0 && isRunning == true)
        {
            currentNode = frontier.Dequeue();
            if (!seedSet.Contains(currentNode.cords))
                currentNode.explored = true;
            ExploreNeighbours();

            if (currentNode.cords == targetCords)
            {
                isRunning = false;
            }
        }
    }

    void ExploreNeighbours()
    {
        List<Node> neighbours = new List<Node>();

        foreach (Vector2Int direction in searchOrder)
        {
            Vector2Int neighboursCords = currentNode.cords + direction;

            if (grid.ContainsKey(neighboursCords))
            {
                neighbours.Add(grid[neighboursCords]);
            }
        }

        foreach (Node neighbour in neighbours)
        {
            if (!reached.ContainsKey(neighbour.cords) && neighbour.walkable)
            {
                neighbour.connectTo = currentNode;
                reached.Add(neighbour.cords, neighbour);
                frontier.Enqueue(neighbour);
            }
        }
    }

    List<Node> BuildPath()
    {
        List <Node> path = new List<Node>();
        Node currentNode = targetNode;

        path.Add(currentNode);
        currentNode.path = true;

        while (currentNode.connectTo != null)
        {
            currentNode = currentNode.connectTo;
            path.Add(currentNode);
            currentNode.path = true;
        }

        path.Reverse();
        return path;
    }

    public void NotifyRecievers()
    {
        BroadcastMessage("RecalculatePath", false, SendMessageOptions.DontRequireReceiver);
    }

public void MarkReachable(Vector2Int origin, int maxSteps)
{
    gridManager.ResetNodes();
    if (!grid.ContainsKey(origin) || maxSteps <= 0) return;

    List<Vector2Int> seeds = GetSeedTiles(origin);
    if (seeds.Count == 0) return;

    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
    Queue<KeyValuePair<Vector2Int, int>> queue = new Queue<KeyValuePair<Vector2Int, int>>();

    foreach (Vector2Int s in seeds)
    {
        if (visited.Contains(s)) continue;
        visited.Add(s);
        queue.Enqueue(new KeyValuePair<Vector2Int, int>(s, 0));
    }

    while (queue.Count > 0)
    {
        var entry = queue.Dequeue();
        Vector2Int cords = entry.Key;
        int dist = entry.Value;

        if (dist > 0) grid[cords].explored = true;
        if (dist >= maxSteps) continue;

        foreach (Vector2Int dir in searchOrder)
        {
            Vector2Int n = cords + dir;
            if (!grid.ContainsKey(n)) continue;
            if (!grid[n].walkable) continue;
            if (visited.Contains(n)) continue;
            visited.Add(n);
            queue.Enqueue(new KeyValuePair<Vector2Int, int>(n, dist + 1));
        }
    }
}

public void SetNewDestination(Vector2Int startCoordinates, Vector2Int targetCoordinates)
{
    startCords = startCoordinates;
    targetCords = targetCoordinates;
    if (!grid.ContainsKey(startCords))
    {
        Debug.LogError("Start coordinate not found in grid: " + startCords);
        return;
    }
    if (!grid.ContainsKey(targetCords))
    {
        Debug.LogError("Target coordinate not found in grid: " + targetCords);
        return;
    }
    startNode = grid[startCords];
    targetNode = grid[targetCords];
    GetNewPath();
}
}
