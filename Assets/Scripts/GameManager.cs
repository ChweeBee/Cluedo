using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    
    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Setup;
    [SerializeField] private bool isGameActive = true;
    
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private CardDealer cardDealer;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private SuggestionManager suggestionManager;
    [SerializeField] private UnitController unitController;
    [SerializeField] private Envelope envelope;
    
    [Header("Game Settings")]
    [SerializeField] private float postMoveDelay = 1f;
    [SerializeField] private float postSuggestionDelay = 2f;
    
    [Header("UI")]
    [SerializeField] private GameObject turnIndicator;
    [SerializeField] private Text turnText;
    [SerializeField] private Text rollResultText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Button showEnvelopeButton;
    
    private bool waitingForMove = false;
    private bool gamePaused = false;
    
    public static GameManager Instance;
    private HashSet<string> eliminatedPlayers = new HashSet<string>();


    public enum GameState
    {
        Setup,
        WaitingForRoll,
        WaitingForMove,
        SuggestionPhase,
        AccusationPhase,
        GameOver
    }

        void Awake()
    {
        Instance = this;
    }

    
    void Start()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (diceManager == null) diceManager = FindAnyObjectByType<DiceManager>();
        if (cardDealer == null) cardDealer = FindAnyObjectByType<CardDealer>();
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
        if (suggestionManager == null) suggestionManager = FindAnyObjectByType<SuggestionManager>();
        if (unitController == null) unitController = FindAnyObjectByType<UnitController>();
        if (envelope == null) envelope = FindAnyObjectByType<Envelope>();
        
        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.onClick.AddListener(ShowEnvelope);
            showEnvelopeButton.gameObject.SetActive(false);
        }
        
        StartCoroutine(InitializeGame());
    }
    
    private IEnumerator InitializeGame()
    {
        currentState = GameState.Setup;
        Debug.Log("Initializing game...");
        yield return null;
        yield return StartCoroutine(DealCardsToPlayers());
        currentState = GameState.WaitingForRoll;
        UpdateTurnUI();
        Debug.Log($"Game initialized. {turnManager.PlayerCount} players in game.");
    }
    
    private IEnumerator DealCardsToPlayers()
    {
        if (turnManager == null || cardDealer == null) yield break;
        
        if (envelope != null)
        {
            if (envelope.SuspectCard != null && cardManager.allCards.Contains(envelope.SuspectCard))
                cardManager.allCards.Remove(envelope.SuspectCard);
            if (envelope.WeaponCard != null && cardManager.allCards.Contains(envelope.WeaponCard))
                cardManager.allCards.Remove(envelope.WeaponCard);
            if (envelope.RoomCard != null && cardManager.allCards.Contains(envelope.RoomCard))
                cardManager.allCards.Remove(envelope.RoomCard);
            cardManager.SortDeck();
        }
        
        cardDealer.DealToAllPlayers();
        yield return null;

        Debug.Log($"Finished dealing. Cards remaining: {cardManager.allCards.Count}");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) turnManager.NextTurn();
        if (Input.GetKeyDown(KeyCode.E)) envelope.ShowEnvelope();

        if (!isGameActive || gamePaused) return;
        
        switch (currentState)
        {
            case GameState.WaitingForRoll:
                HandleRollPhase();
                break;
            case GameState.WaitingForMove:
                HandleMovePhase();
                break;
        }
    }
    
    private void HandleRollPhase()
    {
        if (diceManager.totalResult > 0)
        {
            waitingForMove = true;
            currentState = GameState.WaitingForMove;
            UpdateRollUI(diceManager.totalResult);
            Debug.Log($"Roll result: {diceManager.totalResult}. Ready to move.");
        }
    }
    
    private void HandleMovePhase()
    {
        if (waitingForMove && diceManager.totalResult == 0)
        {
            waitingForMove = false;
            StartCoroutine(DelayBeforeNextTurn());
        }
    }
    
    private IEnumerator DelayBeforeNextTurn()
    {
        gamePaused = true;
        yield return new WaitForSeconds(postMoveDelay);
        currentState = GameState.WaitingForRoll;
        UpdateTurnUI();
        waitingForMove = false;
        gamePaused = false;
    }

    public void OnSuggestionMade()
    {
        if (currentState == GameState.WaitingForMove)
        {
            currentState = GameState.SuggestionPhase;
            StartCoroutine(HandleSuggestionPhase());
        }
    }
    
    private IEnumerator HandleSuggestionPhase()
    {
        gamePaused = true;
        yield return new WaitForSeconds(postSuggestionDelay);
        currentState = GameState.WaitingForRoll;
        waitingForMove = false;
        if (turnManager != null) { turnManager.NextTurn(); UpdateTurnUI(); }
        gamePaused = false;
    }
    
    public void OnAccusationMade(bool isCorrect, Transform accuser)
    {
        if (isCorrect) EndGame(true, accuser);
        else StartCoroutine(HandleIncorrectAccusation(accuser));
    }
    
    private IEnumerator HandleIncorrectAccusation(Transform eliminatedPlayer)
    {
        currentState = GameState.AccusationPhase;
        gamePaused = true;
        Debug.Log($"{eliminatedPlayer.name} made an incorrect accusation and is eliminated!");
        yield return new WaitForSeconds(2f);
        
        if (turnManager.PlayerCount <= 1) EndGame(true, turnManager.Players[0]);
        else
        {
            currentState = GameState.WaitingForRoll;
            waitingForMove = false;
            UpdateTurnUI();
        }
        gamePaused = false;
    }

    public bool IsEliminated(string playerName)
{
    return eliminatedPlayers.Contains(playerName);
}

public void EliminatePlayer(string playerName)
{
    if (!eliminatedPlayers.Contains(playerName))
    {
        eliminatedPlayers.Add(playerName);
        Debug.Log(playerName + " has been eliminated");
    }
}

    
    private void EndGame(bool playerWon, Transform winner)
    {
        currentState = GameState.GameOver;
        isGameActive = false;
        string message = playerWon ? $"{winner.name} wins!" : "Game Over!";
        Debug.Log(message);
        if (envelope != null) envelope.SetGameOver(true);
        if (showEnvelopeButton != null) showEnvelopeButton.gameObject.SetActive(true);
        if (gameOverPanel != null)
        {
            if (gameOverText != null)
            {
                string solutionText = envelope != null ? $"\n\nSolution:\n{envelope.SuspectCard?.cardName}\n{envelope.WeaponCard?.cardName}\n{envelope.RoomCard?.cardName}" : "";
                gameOverText.text = message + solutionText;
            }
            gameOverPanel.SetActive(true);
        }
    }
    
    private void ShowEnvelope()
    {
        if (envelope != null) envelope.ShowEnvelope();
    }
    
    private void UpdateTurnUI()
    {
        if (turnManager != null && turnManager.CurrentPlayer != null)
        {
            if (turnText != null) turnText.text = $"{turnManager.CurrentPlayer.name}'s Turn";
            if (turnIndicator != null) turnIndicator.transform.position = turnManager.CurrentPlayer.position + Vector3.up * 2f;
        }
        if (rollResultText != null)
        {
            rollResultText.text = currentState == GameState.WaitingForRoll ? "Press SPACE to roll dice" : "";
            rollResultText.gameObject.SetActive(currentState == GameState.WaitingForRoll || currentState == GameState.WaitingForMove);
        }
    }
    
    private void UpdateRollUI(int rollResult)
    {
        if (rollResultText != null) rollResultText.text = $"You rolled: {rollResult}\nClick on tiles to move";
    }
    
    public void ResetGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}