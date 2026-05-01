using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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
        PostMoveActions,
        SuggestionPhase,
        AccusationPhase,
        GameOver
    }

    [Header("Game Settings")]
    [SerializeField] private float postMoveDelay = 1f;
    [SerializeField] private float returnToMenuDelay = 10f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isGameActive = true;
    private bool gamePaused = false;
    private bool waitingForMove = false;

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Setup;


    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private SuggestionManager suggestionManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private CardDealer cardDealer;
    [SerializeField] private Envelope envelope;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private CameraController cameraController;

    [Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private TMP_Text rollResultText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private Button showEnvelopeButton;
    [SerializeField] private Button secretPassageButton;
    [SerializeField] private GameObject hudRoot;

    private readonly HashSet<string> eliminatedPlayers = new HashSet<string>();

    public GameState CurrentState => currentState;

    // pushes a string into the roll-result label, used by external panels.
    public void SetRollResultText(string text)
    {
        if (rollResultText != null) rollResultText.text = text;
    }

    public bool HasRolledThisTurn
    {
        get
        {
            GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
            return save != null && save.hasRolledThisTurn;
        }
    }

    public bool HasSuggestedOrAccusedThisTurn
    {
        get
        {
            GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
            return save != null && save.hasSuggestedOrAccusedThisTurn;
        }
    }

    // sets the once-per-turn suggest-or-accuse flag and persists it.
    public void MarkSuggestedOrAccused()
    {
        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save == null) return;
        if (save.hasSuggestedOrAccusedThisTurn) return;
        save.hasSuggestedOrAccusedThisTurn = true;
        if (save.slotIndex >= 0) SaveSystem.Save(save.slotIndex, save);
    }

    // assigns the singleton instance.
    void Awake()
    {
        Instance = this;
    }

    // resolves references, restores eliminations, and starts the game loop.
    void Start()
    {
        FindReferences();
        RestoreEliminations();

        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.onClick.AddListener(ShowEnvelope);
            showEnvelopeButton.gameObject.SetActive(false);
        }

        if (secretPassageButton != null)
        {
            secretPassageButton.onClick.AddListener(UseSecretPassage);
            secretPassageButton.gameObject.SetActive(false);
        }

        StartGame();
    }


   // routes per-frame input to the active phase handler.
   void Update()
{
#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.E))
        envelope.ShowEnvelope();
#endif

    if (!isGameActive || gamePaused)
        return;

    if (currentState == GameState.PostMoveActions)
        HandlePostMoveInput();

    RefreshTurnText();
    RefreshSecretPassageButton();
    RefreshHudVisibility();

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

    // resolves any unset manager references via findobject lookups.
    private void FindReferences()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (diceManager == null) diceManager = FindAnyObjectByType<DiceManager>();
        if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();
        if (suggestionManager == null) suggestionManager = FindAnyObjectByType<SuggestionManager>();
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
        if (cardDealer == null) cardDealer = FindAnyObjectByType<CardDealer>();
        if (envelope == null) envelope = FindAnyObjectByType<Envelope>();
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();
    }

    // bootstraps the very first turn after dependencies are ready.
    public void StartGame()
    {
        SetState(GameState.Setup);

        if (cardManager != null)
            cardManager.SortDeck();

        BeginTurn();
    }

    // sets up the next player's turn, handling saved roll restoration and ai dispatch.
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

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        int savedRoll = save != null ? save.lastDiceTotal : 0;
        bool alreadyRolled = save != null && save.hasRolledThisTurn;

        // restore a partial dice state if the save remembers one.
        if (diceManager != null)
        {
            if (savedRoll > 0)
            {
                diceManager.ApplySavedRoll(savedRoll);
                if (rollResultText != null)
                    rollResultText.text = BuildPostRollText(savedRoll);
            }
            else
            {
                diceManager.totalResult = 0;
                if (alreadyRolled)
                {
                    SetState(GameState.PostMoveActions);
                    if (rollResultText != null)
                        rollResultText.text = BuildAlreadyMovedText();
                }
            }
        }

        // hand control to the ai routine when this turn belongs to a cpu.
        if (IsCurrentPlayerAI())
        {
            var ai = turnManager.CurrentPlayer.GetComponent<AIPlayer>();
            if (ai != null) ai.PerformAITurn();
            else Debug.LogWarning("[TurnManager] AI player missing AIPlayer component.");
        }
    }

    // watches for the dice to land and transitions to the move phase.
    private void HandleRollPhase()
    {
        if (diceManager == null) return;

        if (diceManager.totalResult <= 0) return;

        PersistDiceTotal(diceManager.totalResult);
        SetState(GameState.WaitingForMove);

        if (rollResultText != null)
            rollResultText.text = BuildPostRollText(diceManager.totalResult);

        ResetCameraToDefault();

        Debug.Log("[GameManager] Waiting for player movement.");
    }

    // called by unitcontroller when movement finishes, transitions to the post-move phase.
    public void OnPlayerMoved()
    {
        if (currentState != GameState.WaitingForMove) return;

        PersistTurnState(0, true);

        if (turnManager.CurrentPlayer == null)
        {
            EndCurrentTurn();
            return;
        }

        SetState(GameState.PostMoveActions);

        if (rollResultText != null)
            rollResultText.text = BuildAlreadyMovedText();

        ResetCameraToDefault();
    }

    // wraps the current turn, advances to the next player, and persists state.
    public void EndCurrentTurn()
    {
        if (currentState == GameState.GameOver) return;

        if (suggestionManager != null) suggestionManager.ClearResultTexts();

        // reset and persist the once-per-turn flag so reloads start clean.
        var save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save != null)
        {
            save.hasSuggestedOrAccusedThisTurn = false;
            if (save.slotIndex >= 0) SaveSystem.Save(save.slotIndex, save);
        }

        PersistTurnState(0, false);
        turnManager.NextTurn();
        BeginTurn();
    }

    // hides the hud while the camera is in idle mode.
    private void RefreshHudVisibility()
    {
        if (hudRoot == null) return;

        bool showHud = cameraController == null || cameraController.CurrentMode != CameraController.CameraMode.Idle;
        if (hudRoot.activeSelf != showHud)
            hudRoot.SetActive(showHud);
    }

    // shows the secret-passage button only when the current room has one.
    private void RefreshSecretPassageButton()
    {
        if (secretPassageButton == null) return;

        bool show = false;
        if (currentState == GameState.WaitingForRoll &&
            roomManager != null &&
            turnManager != null &&
            turnManager.CurrentPlayer != null)
        {
            Room currentRoom = roomManager.GetPlayerRoom(turnManager.CurrentPlayer.name);
            show = currentRoom != null && roomManager.HasSecretPassage(currentRoom);
        }

        if (secretPassageButton.gameObject.activeSelf != show)
            secretPassageButton.gameObject.SetActive(show);
    }

    // teleports the current player through a secret passage if one is available.
    public void UseSecretPassage()
    {
        if (currentState != GameState.WaitingForRoll) return;
        if (roomManager == null || turnManager == null || turnManager.CurrentPlayer == null) return;
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null) return;

        Transform unit = turnManager.CurrentPlayer;
        Room source = roomManager.GetPlayerRoom(unit.name);
        if (source == null) return;

        Room target = roomManager.GetSecretPassageTarget(source);
        if (target == null) return;

        source.PlayerLeft(unit.name);
        Vector2Int? slot = target.PlayerEntered(unit.name);
        if (!slot.HasValue) return;

        Vector3 worldPos = gridManager.GetPositionFromCoordinates(slot.Value);
        unit.position = new Vector3(worldPos.x, unit.position.y, worldPos.z);
        unit.LookAt(new Vector3(worldPos.x, unit.position.y, worldPos.z + 1f));

        turnManager.SetLogicalTile(unit, slot.Value);
        if (System.Enum.TryParse<CharacterId>(unit.name, out var charId))
            turnManager.RecordPlayerTile(charId, slot.Value);

        PersistTurnState(0, true);
        SetState(GameState.PostMoveActions);

        if (rollResultText != null)
            rollResultText.text = BuildAlreadyMovedText();

        Debug.Log($"[GameManager] {unit.name} used the secret passage from {source.roomName} to {target.roomName}.");
    }

    // listens for end-turn, suggest, or accuse hotkeys after a move resolves.
    private void HandlePostMoveInput()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            EndCurrentTurn();
            return;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (HasSuggestedOrAccusedThisTurn)
            {
                Debug.Log("Already suggested or accused this turn.");
                return;
            }
            TryStartSuggestion();
            return;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (HasSuggestedOrAccusedThisTurn)
            {
                Debug.Log("Already suggested or accused this turn.");
                return;
            }
            if (suggestionManager == null || roomManager == null || turnManager.CurrentPlayer == null)
                return;

            Room accusationRoom = roomManager.GetPlayerRoom(turnManager.CurrentPlayer.name);
            if (accusationRoom == null)
            {
                Debug.Log("Cannot accuse: player must be in a room.");
                return;
            }

            SetState(GameState.AccusationPhase);
            suggestionManager.ShowAccusationPanel();
        }
    }

    // opens the suggestion panel if the current player is in a room.
    private void TryStartSuggestion()
    {
        if (suggestionManager == null || turnManager.CurrentPlayer == null || roomManager == null)
            return;

        Room room = roomManager.GetPlayerRoom(turnManager.CurrentPlayer.name);
        if (room == null)
        {
            Debug.Log("Cannot suggest: player is not in a room.");
            return;
        }

        SetState(GameState.SuggestionPhase);
        suggestionManager.StartSuggestion(turnManager.CurrentPlayer.name, room);
    }

    // builds the multi-line hud string shown immediately after a roll.
    private string BuildPostRollText(int total)
    {
        if (IsCurrentPlayerAI())
            return "Rolled: " + total + "\nWaiting for AI...";

        string text = "Rolled: " + total +
                      "\nPress Space to confirm move" +
                      "\nPress N to end turn";

        if (IsCurrentPlayerInRoom())
        {
            if (HasSuggestedOrAccusedThisTurn)
                text += "\nAlready suggested or accused this turn.";
            else
            {
                text += "\nPress S to make a suggestion";
                text += "\nPress A to make an accusation";
            }
        }
        return text;
    }

    // builds the hud string shown once movement is finished.
    private string BuildAlreadyMovedText()
    {
        if (IsCurrentPlayerAI())
            return "Waiting for AI...";

        string text = "Already moved this turn." +
                      "\nPress N to end turn";

        if (IsCurrentPlayerInRoom())
        {
            if (HasSuggestedOrAccusedThisTurn)
                text += "\nAlready suggested or accused this turn.";
            else
            {
                text += "\nPress S to make a suggestion";
                text += "\nPress A to make an accusation";
            }
        }
        return text;
    }

    // returns true if the current player is controlled by the ai.
    private bool IsCurrentPlayerAI()
    {
        return turnManager != null && turnManager.IsCurrentPlayerAI;
    }

    // snaps the camera back to default and resets the idle timer.
    private void ResetCameraToDefault()
    {
        if (cameraController == null) return;
        cameraController.SetMode(CameraController.CameraMode.Default);
        cameraController.ResetIdleTimer();
    }

    // returns true if the current player is standing inside any room.
    private bool IsCurrentPlayerInRoom()
    {
        if (roomManager == null || turnManager == null || turnManager.CurrentPlayer == null) return false;
        return roomManager.GetPlayerRoom(turnManager.CurrentPlayer.name) != null;
    }

    // persists the dice total and rolled flag derived from it.
    private void PersistDiceTotal(int total)
    {
        PersistTurnState(total, total > 0);
    }

    // writes per-turn state back to the save file when it actually changes.
    private void PersistTurnState(int roll, bool hasRolled)
    {
        if (GameBootstrap.Instance == null) return;
        GameSaveData save = GameBootstrap.Instance.Active;
        if (save == null || save.slotIndex < 0) return;
        if (save.lastDiceTotal == roll && save.hasRolledThisTurn == hasRolled) return;

        save.lastDiceTotal = roll;
        save.hasRolledThisTurn = hasRolled;
        SaveSystem.Save(save.slotIndex, save);
    }

    // suggestionmanager calls this once a suggestion round wraps up.
    public void OnSuggestionFinished()
    {
        ReturnToPostMove();
    }

    // restores the post-move state when a phase finishes or is cancelled.
    public void ReturnToPostMove()
    {
        if (currentState == GameState.GameOver) return;

        SetState(GameState.PostMoveActions);
        if (rollResultText != null)
            rollResultText.text = BuildAlreadyMovedText();
    }

    // routes the result of an accusation to either game-end or elimination.
    public void OnAccusationMade(bool correct, string playerName)
    {
        Debug.Log("OnAccusationMade called - correct: " + correct + " player: " + playerName);

        if (correct)
        {
            EndGame(playerName);
            return;
        }

        EliminatePlayer(playerName);
        if (!ShouldEndGame())
            EndCurrentTurn();
    }

    // returns true if the named player has already been eliminated.
    public bool IsEliminated(string playerName)
    {
        return eliminatedPlayers.Contains(playerName);
    }

    // marks a player as eliminated, persists it, and hides their unit visuals.
    public void EliminatePlayer(string playerName)
    {
        if (eliminatedPlayers.Contains(playerName)) return;

        eliminatedPlayers.Add(playerName);

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save != null)
        {
            if (save.eliminatedPlayerNames == null)
                save.eliminatedPlayerNames = new System.Collections.Generic.List<string>();
            if (!save.eliminatedPlayerNames.Contains(playerName))
                save.eliminatedPlayerNames.Add(playerName);
            if (save.slotIndex >= 0)
                SaveSystem.Save(save.slotIndex, save);
        }

        HideUnit(FindUnitByName(playerName));

        Debug.Log("[GameManager] " + playerName + " has been eliminated.");
    }

    // rebuilds the eliminated set from the save and hides those units in the scene.
    private void RestoreEliminations()
    {
        eliminatedPlayers.Clear();

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save != null && save.eliminatedPlayerNames != null)
        {
            foreach (string name in save.eliminatedPlayerNames)
            {
                if (!string.IsNullOrEmpty(name))
                    eliminatedPlayers.Add(name);
            }
        }

        if (turnManager != null)
        {
            foreach (Transform p in turnManager.Players)
            {
                if (p != null && eliminatedPlayers.Contains(p.name))
                    HideUnit(p);
            }
        }
    }

    // looks up a player transform by name across the spawned players.
    private Transform FindUnitByName(string playerName)
    {
        if (turnManager == null) return null;
        foreach (Transform p in turnManager.Players)
        {
            if (p != null && p.name == playerName) return p;
        }
        return null;
    }

    // disables every renderer and collider on a unit so it disappears from play.
    private void HideUnit(Transform unit)
    {
        if (unit == null) return;
        foreach (Renderer r in unit.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
        foreach (Collider c in unit.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
    }

    // compares an accusation tuple against the envelope solution.
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

    // returns true and ends the game if zero or one players remain active.
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

    // legacy fallback that ends a turn after the dice clears down.
    private void HandleMovePhase()
{
    if (waitingForMove && diceManager.totalResult == 0)
    {
        waitingForMove = false;

        StartCoroutine(DelayBeforeNextTurn());
    }
}

// pauses input briefly between turns so the camera can settle.
private IEnumerator DelayBeforeNextTurn()
{
    gamePaused = true;

    yield return new WaitForSeconds(postMoveDelay);

    currentState = GameState.WaitingForRoll;
    UpdateTurnUI();

    waitingForMove = false;
    gamePaused = false;
}


    // shows the game-over panel and unlocks the envelope reveal button.
    public void EndGame(string winner)
    {
            Debug.Log("EndGame called - winner: " + winner);
            
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

        StartCoroutine(ReturnToMainMenuAfterDelay());
    }

    // wipes the finished save and returns to the main menu after the configured delay.
    private IEnumerator ReturnToMainMenuAfterDelay()
    {
        yield return new WaitForSeconds(returnToMenuDelay);

        var save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        if (save != null && save.slotIndex >= 0) SaveSystem.Delete(save.slotIndex);
        if (GameBootstrap.Instance != null) GameBootstrap.Instance.Clear();

        SceneManager.LoadScene(mainMenuSceneName);
    }
    // delegates to the envelope component when the button is pressed.
    private void ShowEnvelope()
    {
        Debug.Log("Envelope button/debug key pressed");

        if (envelope != null)
            envelope.ShowEnvelope();
        else
            Debug.LogWarning("Envelope reference missing");
    }

    // refreshes the turn label and prompt text based on whose turn it is.
    private void UpdateTurnUI()
    {
        RefreshTurnText();

        if (rollResultText != null)
            rollResultText.text = IsCurrentPlayerAI() ? "Waiting for AI..." : "Press Space to roll";
    }

    // builds the turn-label string with a hand/notebook hint for humans.
    private void RefreshTurnText()
    {
        if (turnText == null || turnManager == null || turnManager.CurrentPlayer == null) return;

        string playerName = turnManager.CurrentPlayer.name;
        bool handVisible = cardDealer != null && cardDealer.IsHandVisible;
        int playerNumber = turnManager.CurrentIndex + 1;

        string hint = handVisible
            ? "Press H to hide"
            : "Press " + playerNumber + " to view your cards and notebook";

        string desired = playerName + "'s Turn\n" + hint;
        if (turnText.text != desired)
            turnText.text = desired;
    }

    // updates the active game state and logs the transition.
    private void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log("[GameState] " + newState);
    }


    // editor helper that fires a suggestion for the current player.
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

// editor helper that triggers the correct accusation against the envelope.
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

// editor helper that opens the envelope panel directly.
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