using UnityEngine;
using static RoomManager;

public class AIPlayer : CluedoPlayer
{
    private RoomManager roomManager;

    public AIPlayer(string playerName)
    {
        isHuman = false;
        name = playerName;
        roomManager = FindFirstObjectByType<RoomManager>();
    }

    public Room GetCurrentRoom() => roomManager.GetPlayerRoom(name);
}