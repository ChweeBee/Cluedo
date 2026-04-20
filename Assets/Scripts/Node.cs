using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Node 
{
    public Vector2Int cords;
    public bool walkable;
    public bool explored;
    public bool path;
    public Node connectTo;

    public Node(Vector2Int cords, bool walkable)
    {
        this.cords = cords;
        this.walkable = walkable;
    }
}
