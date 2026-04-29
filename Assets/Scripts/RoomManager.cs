using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private List<Room> rooms;

    private static readonly Dictionary<Room.RoomType, Room.RoomType> SecretPassages = new Dictionary<Room.RoomType, Room.RoomType>
    {
        { Room.RoomType.Kitchen, Room.RoomType.Study },
        { Room.RoomType.Study, Room.RoomType.Kitchen },
        { Room.RoomType.Lounge, Room.RoomType.Conservatory },
        { Room.RoomType.Conservatory, Room.RoomType.Lounge }
    };

    public bool HasSecretPassage(Room source)
    {
        return source != null && SecretPassages.ContainsKey(source.roomType);
    }

    public Room GetSecretPassageTarget(Room source)
    {
        if (source == null) return null;
        if (!SecretPassages.TryGetValue(source.roomType, out Room.RoomType targetType)) return null;
        return GetRoom(targetType);
    }

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

    public bool IsRoomTile(Vector2Int tile)
    {
        if (rooms == null) return false;
        foreach (Room room in rooms)
        {
            if (room != null && room.ContainsTile(tile)) return true;
        }
        return false;
    }

    public bool IsDoorTile(Vector2Int tile)
    {
        if (rooms == null) return false;
        foreach (Room room in rooms)
        {
            if (room != null && room.IsDoorTile(tile)) return true;
        }
        return false;
    }

    public Room GetRoomContainingTile(Vector2Int tile)
    {
        if (rooms == null) return null;
        foreach (Room room in rooms)
        {
            if (room != null && room.BelongsToRoom(tile)) return room;
        }
        return null;
    }

    public Vector2Int? HandlePlayerMovement(string playerName, Vector2Int tile)
    {
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

    void Start()
    {
        ReconcilePlayers();
    }

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