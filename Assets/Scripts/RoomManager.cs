using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private List<Room> rooms;
    [SerializeField] private Room room;
    private static readonly Dictionary<Room.RoomType, Room.RoomType> SecretPassages = new Dictionary<Room.RoomType, Room.RoomType>
    {
        { Room.RoomType.Kitchen, Room.RoomType.Study },
        { Room.RoomType.Study, Room.RoomType.Kitchen },
        { Room.RoomType.Lounge, Room.RoomType.Conservatory },
        { Room.RoomType.Conservatory, Room.RoomType.Lounge }
    };

    // returns true if the given room has a secret passage exit.
    public bool HasSecretPassage(Room source)
    {
        return source != null && SecretPassages.ContainsKey(source.roomType);
    }

    // looks up the destination room of a secret passage from the source.
    public Room GetSecretPassageTarget(Room source)
    {
        if (source == null) return null;
        if (!SecretPassages.TryGetValue(source.roomType, out Room.RoomType targetType)) return null;
        return GetRoom(targetType);
    }

    // finds the room object for a given room type.
    public Room GetRoom(Room.RoomType type)
    {
        return rooms.Find(r => r.roomType == type);
    }

    // returns the room a player is currently inside, or null if outside.
    public Room GetPlayerRoom(string playerName)
    {
        foreach (Room room in rooms)
        {
            if (room.GetPlayersInRoom().Contains(playerName))
                return room;
        }
        return null;
    }

    // returns true when any room contains the given tile.
    public bool IsRoomTile(Vector2Int tile)
    {
        if (rooms == null) return false;
        foreach (Room room in rooms)
        {
            if (room != null && room.ContainsTile(tile)) return true;
        }
        return false;
    }

    // returns true when any room treats the given tile as a door.
    public bool IsDoorTile(Vector2Int tile)
    {
        if (rooms == null) return false;
        foreach (Room room in rooms)
        {
            if (room != null && room.IsDoorTile(tile)) return true;
        }
        return false;
    }

    // returns the room that owns the given tile, or null if none does.
    public Room GetRoomContainingTile(Vector2Int tile)
    {
        if (rooms == null) return null;
        foreach (Room room in rooms)
        {
            if (room != null && room.BelongsToRoom(tile)) return room;
        }
        return null;
    }

    // updates room membership when a player lands on a tile, returning a room slot if they entered one.
    public Vector2Int? HandlePlayerMovement(string playerName, Vector2Int tile)
    {
        // remove the player from any prior room before checking entry.
        Room currentRoom = GetPlayerRoom(playerName);
        if (currentRoom != null && !currentRoom.BelongsToRoom(tile))
        {
            currentRoom.PlayerLeft(playerName);
        }

        foreach (Room room in rooms)
        {
            if (room == null) continue;
            if (room.BelongsToRoom(tile))
            {
                Vector2Int? slot = room.PlayerEntered(playerName);
                Debug.Log(playerName + " entered " + room.roomName);
                return slot;
            }
        }

        return null;
    }

    // reconciles room membership from the saved state at scene start.
    void Start()
    {
        ReconcilePlayers();
    }

    // walks every player and registers them with the room they currently stand in.
    void ReconcilePlayers()
    {
        TurnManager turnManager = FindAnyObjectByType<TurnManager>();
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (turnManager == null || gridManager == null || rooms == null) return;

        foreach (Transform p in turnManager.Players)
        {
            if (p == null) continue;
            Vector2Int tile = gridManager.GetCoordinatesFromPosition(p.position);
            foreach (Room room in rooms)
            {
                if (room != null && room.BelongsToRoom(tile))
                {
                    room.RegisterPlayerAt(p.name, tile);
                    break;
                }
            }
        }
    }
}