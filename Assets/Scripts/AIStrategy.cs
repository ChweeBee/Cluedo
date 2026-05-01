using System.Collections.Generic;
using UnityEngine;

public class AIStrategy
{
    readonly AIPlayer player;
    readonly CardManager cardManager;
    readonly RoomManager roomManager;
    readonly TurnManager turnManager;
    readonly GridManager gridManager;
    readonly Pathfinding pathFinder;

    // caches scene managers so per-decision lookups stay cheap.
    public AIStrategy(AIPlayer player)
    {
        this.player = player;
        cardManager = Object.FindAnyObjectByType<CardManager>();
        roomManager = Object.FindAnyObjectByType<RoomManager>();
        turnManager = Object.FindAnyObjectByType<TurnManager>();
        gridManager = Object.FindAnyObjectByType<GridManager>();
        pathFinder = Object.FindAnyObjectByType<Pathfinding>();
    }

    // marks every card the ai already knows about so deductions start from the right baseline.
    public void BootstrapKnowledge()
    {
        // own hand is always known.
        foreach (Card c in player.hand)
            if (c != null) player.MarkCardChecked(c.cardName, true);

        // also flag any public table cards from the active save.
        var save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save != null && save.publicHandCardNames != null)
        {
            foreach (string n in save.publicHandCardNames)
                if (!string.IsNullOrEmpty(n)) player.MarkCardChecked(n, true);
        }
    }

    // returns true and the trio of suspects only when each category has exactly one unknown.
    public bool TryGetAccusation(out Card suspect, out Card weapon, out Card room)
    {
        suspect = OnlyUnknown(cardManager?.suspectDeck);
        weapon = OnlyUnknown(cardManager?.weaponDeck);
        room = OnlyUnknown(cardManager?.roomDeck);
        return suspect != null && weapon != null && room != null;
    }

    // chooses an unknown suspect and weapon to pair with the forced room card.
    public void PickSuggestion(Card forcedRoomCard, out Card suspect, out Card weapon)
    {
        suspect = PickUnknown(cardManager?.suspectDeck) ?? FirstNonNull(cardManager?.suspectDeck);
        weapon = PickUnknown(cardManager?.weaponDeck) ?? FirstNonNull(cardManager?.weaponDeck);
    }

    // returns the closest unknown room as a fallback target when no path is available.
    public Room PickTargetRoom()
    {
        Room currentRoom = roomManager != null ? roomManager.GetPlayerRoom(player.name) : null;
        Room[] allRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

        Room best = null;
        float bestDist = float.MaxValue;
        Vector3 myPos = player.transform.position;

        foreach (Room r in allRooms)
        {
            if (r == null || r == currentRoom) continue;
            if (player.IsCardChecked(r.roomName)) continue;
            float d = Vector3.SqrMagnitude(r.transform.position - myPos);
            if (d < bestDist) { bestDist = d; best = r; }
        }

        if (best != null) return best;

        foreach (Room r in allRooms)
            if (r != null && r != currentRoom) return r;

        return null;
    }

    // bfs-paths to every door of every unknown room and picks the longest reachable one.
    public Vector2Int? PickBestMoveTarget(int stepBudget, out Room targetRoom)
    {
        targetRoom = null;
        if (gridManager == null || pathFinder == null) return null;

        // start from the logical tile if available, else from world position.
        Vector2Int myTile = gridManager.GetCoordinatesFromPosition(player.transform.position);
        if (turnManager != null && turnManager.TryGetLogicalTile(player.transform, out Vector2Int t)) myTile = t;

        Room currentRoom = roomManager != null ? roomManager.GetPlayerRoom(player.name) : null;
        Room[] allRooms = Object.FindObjectsByType<Room>(FindObjectsSortMode.None);

        // track best in-budget door and best out-of-budget door separately.
        Vector2Int? bestReachable = null;
        int bestReachableCost = -1;
        Room bestReachableRoom = null;

        Vector2Int? bestUnreachable = null;
        int bestUnreachableCost = int.MaxValue;
        Room bestUnreachableRoom = null;

        foreach (Room r in allRooms)
        {
            if (r == null || r == currentRoom) continue;
            if (player.IsCardChecked(r.roomName)) continue;
            if (r.DoorTiles == null) continue;

            foreach (Vector2Int door in r.DoorTiles)
            {
                pathFinder.SetNewDestination(myTile, door);
                var p = pathFinder.GetNewPath(myTile);
                if (p == null || p.Count == 0) continue;

                int moveCost = p.Count - 1; // edges walked
                if (moveCost <= stepBudget)
                {
                    // Reachable: prefer the longest walk so we use as much dice as possible.
                    if (moveCost > bestReachableCost)
                    {
                        bestReachableCost = moveCost;
                        bestReachable = door;
                        bestReachableRoom = r;
                    }
                }
                else if (moveCost < bestUnreachableCost)
                {
                    // Not reachable this turn: track closest as a fallback.
                    bestUnreachableCost = moveCost;
                    bestUnreachable = door;
                    bestUnreachableRoom = r;
                }
            }
        }

        if (bestReachable.HasValue)
        {
            targetRoom = bestReachableRoom;
            return bestReachable;
        }
        if (bestUnreachable.HasValue)
        {
            targetRoom = bestUnreachableRoom;
            return bestUnreachable; // RunAIMove truncates to budget; we walk toward it.
        }
        return null;
    }

    // returns the closest door of a given target room by manhattan distance.
    public Vector2Int? PickMoveTarget(Room target, int stepBudget)
    {
        if (target == null || gridManager == null) return null;

        Vector2Int myTile = gridManager.GetCoordinatesFromPosition(player.transform.position);
        if (turnManager != null && turnManager.TryGetLogicalTile(player.transform, out Vector2Int t)) myTile = t;

        Vector2Int? bestDoor = null;
        int bestDist = int.MaxValue;
        foreach (Vector2Int door in target.DoorTiles)
        {
            int d = Mathf.Abs(door.x - myTile.x) + Mathf.Abs(door.y - myTile.y);
            if (d < bestDist) { bestDist = d; bestDoor = door; }
        }
        return bestDoor;
    }

    // legacy debug helper that drops the ai straight into a room slot.
    public void TeleportTo(Room target)
    {
        if (target == null || gridManager == null) return;

        Room current = roomManager != null ? roomManager.GetPlayerRoom(player.name) : null;
        if (current != null) current.PlayerLeft(player.name);

        Vector2Int? slot = target.PlayerEntered(player.name);
        if (!slot.HasValue) return;

        Vector3 worldPos = gridManager.GetPositionFromCoordinates(slot.Value);
        player.transform.position = new Vector3(worldPos.x, player.transform.position.y, worldPos.z);

        if (turnManager != null)
        {
            turnManager.SetLogicalTile(player.transform, slot.Value);
            turnManager.RecordPlayerTile(player.character, slot.Value);
        }
    }

    // picks which card to reveal when disproving, preferring repeats to leak less info.
    public Card PickDisproveCard(Transform asker, Card suspect, Card weapon, Card room, List<Card> matches)
    {
        if (matches == null || matches.Count == 0) return null;

        // pull this player's persisted disprove history from the active save.
        var save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        var setup = save != null ? save.players.Find(p => p.character == player.character) : null;
        var history = setup != null ? setup.aiDisproveHistory : null;

        // same asker plus same combo as before, repeat the same card.
        if (history != null)
        {
            foreach (var e in history)
            {
                if (e.askerCharacter == asker.name &&
                    e.suspectName == suspect?.cardName &&
                    e.weaponName == weapon?.cardName &&
                    e.roomName == room?.cardName)
                {
                    Card prev = matches.Find(m => m.cardName == e.shownCardName);
                    if (prev != null) return prev;
                }
            }

            // otherwise prefer reusing a card already shown to anyone.
            foreach (Card c in matches)
            {
                foreach (var e in history)
                {
                    if (e.shownCardName == c.cardName)
                    {
                        RecordDisprove(setup, asker, suspect, weapon, room, c);
                        return c;
                    }
                }
            }
        }

        Card chosen = matches[0];
        RecordDisprove(setup, asker, suspect, weapon, room, chosen);
        return chosen;
    }

    // marks a revealed card as known so future suggestions exclude it.
    public void OnCardShownToMe(Card card)
    {
        if (card != null) player.MarkCardChecked(card.cardName, true);
    }

    // appends a new disprove entry and persists it back to disk.
    void RecordDisprove(PlayerSetup setup, Transform asker, Card suspect, Card weapon, Card room, Card shown)
    {
        if (setup == null || shown == null) return;
        setup.aiDisproveHistory.Add(new DisproveEntry
        {
            askerCharacter = asker != null ? asker.name : null,
            suspectName = suspect?.cardName,
            weaponName = weapon?.cardName,
            roomName = room?.cardName,
            shownCardName = shown.cardName
        });
        var save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save != null && save.slotIndex >= 0) SaveSystem.Save(save.slotIndex, save);
    }

    // returns the single unknown card in a deck, or null if there is more than one.
    Card OnlyUnknown(List<Card> deck)
    {
        if (deck == null) return null;
        Card result = null;
        foreach (Card c in deck)
        {
            if (c == null) continue;
            if (player.IsCardChecked(c.cardName)) continue;
            if (result != null) return null;
            result = c;
        }
        return result;
    }

    // returns the first unchecked card in a deck.
    Card PickUnknown(List<Card> deck)
    {
        if (deck == null) return null;
        foreach (Card c in deck)
            if (c != null && !player.IsCardChecked(c.cardName)) return c;
        return null;
    }

    // returns the first non-null entry in a deck.
    Card FirstNonNull(List<Card> deck)
    {
        if (deck == null) return null;
        foreach (Card c in deck) if (c != null) return c;
        return null;
    }
}
