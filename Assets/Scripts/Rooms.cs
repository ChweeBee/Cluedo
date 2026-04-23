using UnityEngine;
using System.Collections.Generic;

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
    
    private List<string> playersInRoom = new List<string>();
    
    void Start()
    {
        if (string.IsNullOrEmpty(roomName))
            roomName = roomType.ToString();
    }

    public bool ContainsTile(Vector2Int cord)
    {
        return roomTiles.Contains(cord);
    }

    public bool IsDoorTile(Vector2Int cord)
    {
        return doorTiles.Contains(cord);
    }

    // keeping track of player position to make suggestions and stuff easier
    public void PlayerEntered(string playerName)
    {
        if (!playersInRoom.Contains(playerName))
            playersInRoom.Add(playerName);
    }

    public void PlayerLeft(string playerName)
    {
        playersInRoom.Remove(playerName);
    }

    public List<string> GetPlayersInRoom()
    {
        return playersInRoom;
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