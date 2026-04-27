using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private List<Room> rooms;

    public Room GetRoom(Room.RoomType type)
    {
        return rooms.Find(r => r.roomType == type);
    }

    public Room GetPlayerRoom(string playerName)
    {
        foreach (Room room in rooms)
        {
            if (room.GetPlayersInRoom().Contains(playerName))
                return room;
        }
        return null;
    }

    public void HandlePlayerMovement(string playerName, Vector2Int tile)
    {
        // Check if player has left their current room
        Room currentRoom = GetPlayerRoom(playerName);
        if (currentRoom != null && !currentRoom.ContainsTile(tile))
        {
            currentRoom.PlayerLeft(playerName);
        }

        // Check if player has entered a new room
        foreach (Room room in rooms)
        {
            if (room.ContainsTile(tile))
            {
                room.PlayerEntered(playerName);
                Debug.Log(playerName + " entered " + room.roomName);
                break;
            }
        }
    }
}