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

    public List<Node> GetNewPath()
    {
        return GetNewPath(startCords);
    }

    public List<Node> GetNewPath(Vector2Int coordinates)
    {
        gridManager.ResetNodes();

        BreadthFirstSearch(coordinates);
        return BuildPath();
    }

    void BreadthFirstSearch(Vector2Int coordinates)
    {
        startNode.walkable = true;
        targetNode.walkable = true;

        frontier.Clear();
        reached.Clear();

        bool isRunning = true;

        frontier.Enqueue(grid[coordinates]);
        reached.Add(coordinates, grid[coordinates]);

        while (frontier.Count > 0 && isRunning == true)
        {
            currentNode = frontier.Dequeue();
            currentNode.explored = true;
            ExploreNeighbours();

            if (currentNode.cords == targetCords)
            {
                isRunning = false;
                currentNode.walkable = false;
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

    public void SetNewDestination(Vector2Int startCoordinates, Vector2Int targetCoordinates)
    {
        startCords = startCoordinates;
        targetCords = targetCoordinates;
        startNode = grid[this.startCords];
        targetNode = grid[this.TargetCords];
        GetNewPath();
    }
}
