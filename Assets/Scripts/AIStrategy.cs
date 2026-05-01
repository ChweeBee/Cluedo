using UnityEngine;

public class AIStrategy : MonoBehaviour
{
    private readonly AIPlayer player;

    public AIStrategy(AIPlayer player)
    {
        this.player = player;
    }

    public void RollDice()
    {
        int roll = Random.Range(1, 7);
        Debug.Log($"[AIStrategy] Rolled {roll}");
        // Move logic can use TurnManager.SetLogicalTile()
    }
    public void MoveToTargetTile()
    {
        // Example: pick a random reachable tile
        Vector2Int target = new Vector2Int(Random.Range(0, 10), Random.Range(0, 10));
        TurnManager tm = FindFirstObjectByType<TurnManager>();
        tm.SetLogicalTile(player.transform, target);
    }

    public void MakeSuggestion()
    {
        // Example placeholder
        Debug.Log("[AIStrategy] Making a suggestion...");
    }

    public void CheckForAccusation()
    {
        // Example placeholder
        Debug.Log("[AIStrategy] Deciding whether to accuse...");
    }
}
