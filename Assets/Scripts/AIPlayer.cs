using UnityEngine;

public class AIPlayer : CluedoPlayer
{
    private AIStrategy strategy;
    private TurnManager turnManager;

    void Awake()
    {
        turnManager = FindAnyObjectByType<TurnManager>();

        // Checks if player is human
        if(turnManager != null && !turnManager.IsAI(transform))
        {
            // Disables Update() and other coroutines in AI if human
            enabled = false;
            return;
        }

        isAI = true;
        strategy = new AIStrategy(this);
    }

    public void PerformAITurn()
    {
        Debug.Log($"[AIPlayer] {name} is taking its turn.");

        if (strategy == null)
        {
            Debug.LogError("[AIPlayer] Strategy is null!");
            strategy = new AIStrategy(this);
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[AIPlayer] GameManager.Instance is null!");
            return;
        }

        if (GameManager.Instance == null) return;

        Debug.Log($"[AIPlayer] {name} is taking its turn.");

        // Example logic flow:
        strategy.RollDice();
        strategy.MoveToTargetTile();
        strategy.MakeSuggestion();
        strategy.CheckForAccusation();

        EndTurn();
    }

    private void EndTurn()
    {
        GameManager.Instance.EndCurrentTurn();
    }
}
