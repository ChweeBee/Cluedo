using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] List<Transform> players = new List<Transform>();

    int currentIndex = 0;

    public Transform CurrentPlayer => players.Count == 0 ? null : players[currentIndex];
    public int CurrentIndex => currentIndex;
    public int PlayerCount => players.Count;
    public IReadOnlyList<Transform> Players => players;

    void Start()
    {
        if (players.Count == 0)
        {
            Debug.LogWarning("[TurnManager] No players assigned.");
            return;
        }
        Debug.Log($"[TurnManager] Turn 1: {CurrentPlayer.name}");
    }

    public void NextTurn()
    {
        if (players.Count == 0) return;
        currentIndex = (currentIndex + 1) % players.Count;
        Debug.Log($"[TurnManager] Turn -> {CurrentPlayer.name}");
    }
}
