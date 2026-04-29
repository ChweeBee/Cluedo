using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        Setup,
        WaitingForRoll,
        WaitingForMove,
        SuggestionPhase,
        AccusationPhase,
        GameOver
    }

    [Header("Game Settings")]
    [SerializeField] private float postMoveDelay = 1f;

    private bool isGameActive = true;
    private bool gamePaused = false;
    private bool waitingForRoll = true;
    private bool waitingForMove = false;

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Setup;


    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private SuggestionManager suggestionManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private Envelope envelope;

    [Header("UI")]
    [SerializeField] private Text turnText;
    [SerializeField] private Text rollResultText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Button showEnvelopeButton;

    private readonly HashSet<string> eliminatedPlayers = new HashSet<string>();

    public GameState CurrentState => currentState;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        FindReferences();

        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.onClick.AddListener(ShowEnvelope);
            showEnvelopeButton.gameObject.SetActive(false);
        }

        StartGame();
    }


   void Update()
{
#if UNITY_EDITOR

    if (Input.GetKeyDown(KeyCode.T))
        turnManager.NextTurn();

    if (Input.GetKeyDown(KeyCode.E))
        envelope.ShowEnvelope();

    if (Input.GetKeyDown(KeyCode.S))
    {
        if (
            suggestionManager != null &&
            turnManager.CurrentPlayer != null &&
            roomManager != null
        )
        {
            Room room = roomManager.GetPlayerRoom(turnManager.CurrentPlayer.name);

            if (room != null)
                suggestionManager.StartSuggestion(
                    turnManager.CurrentPlayer.name,
                    room
                );
            else
                Debug.Log("Cannot suggest: player is not in a room.");
        }
    }

    if (Input.GetKeyDown(KeyCode.A))
    {
        if (suggestionManager != null)
            suggestionManager.ShowAccusationPanel();
    }

    if (Input.GetKeyDown(KeyCode.N))
        EndCurrentTurn();

#endif

    if (!isGameActive || gamePaused)
        return;

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

    private void FindReferences()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (diceManager == null) diceManager = FindAnyObjectByType<DiceManager>();
        if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();
        if (suggestionManager == null) suggestionManager = FindAnyObjectByType<SuggestionManager>();
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
        if (envelope == null) envelope = FindAnyObjectByType<Envelope>();
    }

    public void StartGame()
    {
        SetState(GameState.Setup);

        if (cardManager != null)
            cardManager.SortDeck();

        BeginTurn();
    }

    public void BeginTurn()
    {
        if (currentState == GameState.GameOver) return;

        if (turnManager == null || turnManager.PlayerCount == 0)
        {
            EndGame("Nobody");
            return;
        }

        turnManager.SkipEliminatedPlayers();

        if (ShouldEndGame())
            return;

        SetState(GameState.WaitingForRoll);
        UpdateTurnUI();

        if (diceManager != null)
            diceManager.RollDice();
    }

    private void HandleRollPhase()
    {
        if (diceManager == null) return;

        if (diceManager.totalResult <= 0) return;

        SetState(GameState.WaitingForMove);

        if (rollResultText != null)
            rollResultText.text = "Rolled: " + diceManager.totalResult + "\nMove your character.";

        Debug.Log("[GameManager] Waiting for player movement.");
    }

    public void OnPlayerMoved()
    {
        if (currentState != GameState.WaitingForMove) return;

        Transform player = turnManager.CurrentPlayer;

        if (player == null)
        {
            EndCurrentTurn();
            return;
        }

        Room currentRoom = null;

        if (roomManager != null)
            currentRoom = roomManager.GetPlayerRoom(player.name);

        if (currentRoom != null && suggestionManager != null)
        {
            SetState(GameState.SuggestionPhase);
            suggestionManager.StartSuggestion(player.name, currentRoom);
        }
        else
        {
            EndCurrentTurn();
        }
    }

    public void EndCurrentTurn()
    {
        if (currentState == GameState.GameOver) return;

        turnManager.NextTurn();
        BeginTurn();
    }

    public void OnSuggestionFinished()
    {
        EndCurrentTurn();
    }

    public void OnAccusationMade(bool correct, string playerName)
    {
        SetState(GameState.AccusationPhase);

        if (correct)
        {
            EndGame(playerName);
        }
        else
        {
            EliminatePlayer(playerName);

            if (!ShouldEndGame())
                EndCurrentTurn();
        }
    }

    public bool IsEliminated(string playerName)
    {
        return eliminatedPlayers.Contains(playerName);
    }

    public void EliminatePlayer(string playerName)
    {
        if (eliminatedPlayers.Contains(playerName)) return;

        eliminatedPlayers.Add(playerName);
        Debug.Log("[GameManager] " + playerName + " has been eliminated.");
    }

    public bool CheckAccusation(string suspect, string weapon, string room)
    {
        if (envelope == null) return false;

        return
            envelope.SuspectCard != null &&
            envelope.WeaponCard != null &&
            envelope.RoomCard != null &&
            envelope.SuspectCard.cardName == suspect &&
            envelope.WeaponCard.cardName == weapon &&
            envelope.RoomCard.cardName == room;
    }

    private bool ShouldEndGame()
    {
        int activePlayers = 0;
        string lastPlayer = "";

        foreach (Transform player in turnManager.Players)
        {
            if (player == null) continue;

            if (!IsEliminated(player.name))
            {
                activePlayers++;
                lastPlayer = player.name;
            }
        }

        if (activePlayers == 0)
        {
            EndGame("Nobody");
            return true;
        }

        if (activePlayers == 1)
        {
            EndGame(lastPlayer);
            return true;
        }

        return false;
    }

    private void HandleMovePhase()
{
    if (waitingForMove && diceManager.totalResult == 0)
    {
        waitingForMove = false;
        waitingForRoll = true;

        StartCoroutine(DelayBeforeNextTurn());
    }
}

private IEnumerator DelayBeforeNextTurn()
{
    gamePaused = true;

    yield return new WaitForSeconds(postMoveDelay);

    currentState = GameState.WaitingForRoll;
    UpdateTurnUI();

    waitingForRoll = true;
    waitingForMove = false;
    gamePaused = false;
}


    public void EndGame(string winner)
    {
        SetState(GameState.GameOver);

        Debug.Log("[GameManager] GAME OVER - " + winner + " wins.");

        if (envelope != null)
            envelope.SetGameOver(true);

        if (showEnvelopeButton != null)
            showEnvelopeButton.gameObject.SetActive(true);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = winner + " wins!";
    }
    private void ShowEnvelope()
    {
        Debug.Log("Envelope button/debug key pressed");

        if (envelope != null)
            envelope.ShowEnvelope();
        else
            Debug.LogWarning("Envelope reference missing");
    }

    private void UpdateTurnUI()
    {
        if (turnText != null && turnManager.CurrentPlayer != null)
            turnText.text = turnManager.CurrentPlayer.name + "'s Turn";

        if (rollResultText != null)
            rollResultText.text = "Rolling dice...";
    }

    private void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log("[GameState] " + newState);
    }


    private void DebugTestSuggestion()
{
    if (turnManager == null || suggestionManager == null || roomManager == null)
    {
        Debug.LogWarning("Missing manager reference.");
        return;
    }

    Transform player = turnManager.CurrentPlayer;

    if (player == null)
    {
        Debug.LogWarning("No current player.");
        return;
    }

    Room room = roomManager.GetPlayerRoom(player.name);

    if (room == null)
    {
        Debug.LogWarning(player.name + " is not in a room, so cannot suggest.");
        return;
    }

    Debug.Log("DEBUG: Starting suggestion for " + player.name);
    suggestionManager.StartSuggestion(player.name, room);
}

private void DebugCorrectAccusation()
{
    if (envelope == null)
    {
        Debug.LogWarning("Envelope reference missing.");
        return;
    }

    if (turnManager == null || turnManager.CurrentPlayer == null)
    {
        Debug.LogWarning("No current player.");
        return;
    }

    string playerName = turnManager.CurrentPlayer.name;

    bool correct = CheckAccusation(
        envelope.SuspectCard.cardName,
        envelope.WeaponCard.cardName,
        envelope.RoomCard.cardName
    );

    Debug.Log("DEBUG accusation correct? " + correct);

    OnAccusationMade(correct, playerName);
}

private void DebugShowEnvelope()
{
    if (envelope == null)
    {
        envelope = FindAnyObjectByType<Envelope>();
    }

    if (envelope == null)
    {
        Debug.LogWarning("No Envelope found in scene.");
        return;
    }

    envelope.ShowEnvelope();
}


}