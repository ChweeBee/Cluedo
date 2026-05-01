using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Card;
using static SuggestionManager;
using static DiceManager;

public class AIPlayerController : MonoBehaviour
{
    public CluedoPlayer cluedoPlayer;
    public List<Card> knownCards;
    public Dictionary<Card, float> suspicionScores = new();

    private SuggestionManager suggestionManager;
    private DiceManager diceManager;

    void Awake()
    {
        if (cluedoPlayer == null) cluedoPlayer = GetComponent<CluedoPlayer>();
        suggestionManager = FindFirstObjectByType<SuggestionManager>();
        diceManager = FindAnyObjectByType<DiceManager>();
    }

    public void TakeTurn()
    {
        diceManager.RollDice();
        diceManager.GetResults();
        MoveToTargetRoom();
        MakeSuggestion();
    }

    void MoveToTargetRoom()
    {
        // Simple heuristic: pick random room not recently visited
    }

    void MakeSuggestion() => suggestionManager.MakeSuggestion();

    Card PickMostSuspicious(CardType type)
    {
        return suspicionScores
            //.Where(kv => kv.Key.Type == type)
            .OrderByDescending(kv => kv.Value)
            .First().Key;
    }

    void UpdateSuspicion(List<Card> suggestedCards, bool disproved, Card shownCard = null)
    {
        foreach (var card in suggestedCards)
        {
            if (shownCard != null && card == shownCard)
            {
                suspicionScores[card] = 0f;
                MarkInNotebook(card, true);
            }
            else if (disproved)
                suspicionScores[card] = Mathf.Max(0f, suspicionScores[card] - 0.1f);
            else
                suspicionScores[card] = Mathf.Min(1f, suspicionScores[card] + 0.2f);
        }
    }

    void MarkInNotebook(Card card, bool isChecked)
    {
        if (cluedoPlayer == null || card == null) return;
        cluedoPlayer.MarkCardChecked(card.cardName, isChecked);
    }
}
