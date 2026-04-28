using UnityEngine;
<<<<<<< Updated upstream
=======
using UnityEngine.UI;
using System.Collections;
using System.Collections;
>>>>>>> Stashed changes
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
<<<<<<< Updated upstream
    public static GameManager Instance;  

    [Header("References")]
    public CardManager cardManager;
    public TurnManager turnManager;
    public SuggestionManager suggestionManager;
    public RoomManager roomManager;

    [Header("Envelope (Secret Solution)")]
    public Card envelopeSuspect;
    public Card envelopeWeapon;
    public Card envelopeRoom;

    // Each player's hand: playerName -> list of cards
    public Dictionary<string, List<Card>> playerHands = new Dictionary<string, List<Card>>();

    // Eliminated players
    public List<string> eliminatedPlayers = new List<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (cardManager == null)
            cardManager = FindAnyObjectByType<CardManager>();
        
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
        
        if (suggestionManager == null)
            suggestionManager = FindAnyObjectByType<SuggestionManager>();
        
        if (roomManager == null)
            roomManager = FindAnyObjectByType<RoomManager>();
    }

    void Start()
    {
        SetupGame();
    }

    void SetupGame()
    {
        if (turnManager == null)
        {
            Debug.LogError("[GameManager] TurnManager is null!");
            return;
        }
        
        if (turnManager.PlayerCount == 0)
        {
            Debug.LogWarning("[GameManager] No players assigned in TurnManager! Please assign players in the inspector.");
            return;
        }

        Debug.Log("[GameManager] Starting game setup");

        if (cardManager == null)
        {
            Debug.LogError("[GameManager] CardManager is null!");
            return;
        }
        
        cardManager.SortDeck();

        Debug.Log(
            "Suspects: " + cardManager.suspectDeck.Count +
            ", Weapons: " + cardManager.weaponDeck.Count +
            ", Rooms: " + cardManager.roomDeck.Count
        );

        if (
            cardManager.suspectDeck.Count == 0 ||
            cardManager.weaponDeck.Count == 0 ||
            cardManager.roomDeck.Count == 0
        )
        {
            Debug.LogError("[GameManager] A deck is empty - check that all cards have their CardType set correctly!");
            return;
        }

        // Copy decks
        List<Card> suspects = new List<Card>(cardManager.suspectDeck);
        List<Card> weapons = new List<Card>(cardManager.weaponDeck);
        List<Card> rooms = new List<Card>(cardManager.roomDeck);

        // Shuffle each deck
        Shuffle(suspects);
        Shuffle(weapons);
        Shuffle(rooms);

        // Select envelope cards (one from each deck)
        envelopeSuspect = suspects[0];
        suspects.RemoveAt(0);

        envelopeWeapon = weapons[0];
        weapons.RemoveAt(0);

        envelopeRoom = rooms[0];
        rooms.RemoveAt(0);

        Debug.Log(
            "[GameManager] Envelope set: " +
            envelopeSuspect.cardName + ", " +
            envelopeWeapon.cardName + ", " +
            envelopeRoom.cardName
        );

        // Combine remaining cards
        List<Card> remaining = new List<Card>();
        remaining.AddRange(suspects);
        remaining.AddRange(weapons);
        remaining.AddRange(rooms);

        Shuffle(remaining);
        
        Debug.Log($"[GameManager] Remaining cards to deal: {remaining.Count}");

        // Initialize player hands
        int playerCount = turnManager.PlayerCount;
        playerHands.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            if (turnManager.Players[i] == null)
            {
                Debug.LogError($"[GameManager] Player at index {i} is null!");
                continue;
            }
            string playerName = turnManager.Players[i].name;
            playerHands[playerName] = new List<Card>();
            Debug.Log($"[GameManager] Created hand for {playerName}");
        }

        // Deal cards to players
        int index = 0;
        foreach (Card card in remaining)
        {
            if (playerCount == 0) break;
            string playerName = turnManager.Players[index % playerCount].name;
            
            if (playerHands.ContainsKey(playerName))
            {
                playerHands[playerName].Add(card);
                Debug.Log($"[GameManager] Dealt {card.cardName} to {playerName}");
            }
            else
            {
                Debug.LogError($"[GameManager] Player {playerName} not found in playerHands dictionary!");
            }
            index++;
        }

        // Log each player's hand
        foreach (var kvp in playerHands)
        {
            string hand = "";
            foreach (Card card in kvp.Value)
                hand += card.cardName + ", ";
            Debug.Log($"[GameManager] {kvp.Key}'s hand ({kvp.Value.Count} cards): " + hand);
        }

        Debug.Log("[GameManager] Game setup complete");
        
        if (turnManager != null)
            turnManager.StartGame();
        else
            Debug.LogError("[GameManager] TurnManager is null after setup!");
    }

    public void EliminatePlayer(string playerName)
    {
        if (!eliminatedPlayers.Contains(playerName))
        {
            eliminatedPlayers.Add(playerName);
            Debug.Log($"[GameManager] {playerName} has been eliminated!");
            
            /*
            Transform playerTransform = turnManager?.GetPlayerByName(playerName);
            if (playerTransform != null)
            {
                playerTransform.gameObject.SetActive(false);
            }
            */
        }
    }

    public bool IsEliminated(string playerName)
    {
        return eliminatedPlayers.Contains(playerName);
    }

    public bool CheckAccusation(string suspectName, string weaponName, string roomName)
    {
        if (envelopeSuspect == null || envelopeWeapon == null || envelopeRoom == null)
        {
            Debug.LogError("[GameManager] Envelope cards are not set!");
            return false;
        }
        
        return envelopeSuspect.cardName == suspectName &&
               envelopeWeapon.cardName == weaponName &&
               envelopeRoom.cardName == roomName;
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
=======
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
    [SerializeField] private int cardsPerPlayer = 3;
    [SerializeField] private float postRollDelay = 1f;
    [SerializeField] private float postMoveDelay = 1f;
    [SerializeField] private float postSuggestionDelay = 2f;
    
    [Header("UI")]
    [SerializeField] private GameObject turnIndicator;
    [SerializeField] private Text turnText;
    [SerializeField] private Text rollResultText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text gameOverText;
    [SerializeField] private Button showEnvelopeButton;
    
    private bool waitingForRoll = true;
    private bool waitingForMove = false;
    private bool gamePaused = false;
    
    public enum GameState
    {
        Setup,
        WaitingForRoll,
        WaitingForMove,
        SuggestionPhase,
        AccusationPhase,
        GameOver
    }
    
    void Start()
    {
        // Find references if not assigned
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (diceManager == null) diceManager = FindObjectOfType<DiceManager>();
        if (cardDealer == null) cardDealer = FindObjectOfType<CardDealer>();
        if (cardManager == null) cardManager = FindObjectOfType<CardManager>();
        if (suggestionManager == null) suggestionManager = FindObjectOfType<SuggestionManager>();
        if (unitController == null) unitController = FindObjectOfType<UnitController>();
        if (envelope == null) envelope = FindObjectOfType<Envelope>();
        
        // Setup envelope button
        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.onClick.AddListener(ShowEnvelope);
            showEnvelopeButton.gameObject.SetActive(false);
        }
        
        // Initialize game
        StartCoroutine(InitializeGame());
    }
    
    private IEnumerator InitializeGame()
    {
        currentState = GameState.Setup;
        Debug.Log("Initializing game...");
        
        // Wait for envelope to initialize
        yield return null;
        
        // Deal cards to all players (solution cards are already removed from deck by envelope)
        yield return StartCoroutine(DealCardsToPlayers());
        
        // Start first turn
        currentState = GameState.WaitingForRoll;
        UpdateTurnUI();
        
        Debug.Log($"Game initialized. Solution is in the envelope. {turnManager.PlayerCount} players in game.");
    }
    
    private IEnumerator DealCardsToPlayers()
    {
        if (turnManager == null || cardDealer == null) yield break;
        
        // Make sure cardManager's allCards doesn't include solution cards
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
        
        foreach (Transform player in turnManager.Players)
        {
            // Ensure player has a CardHolder component
            CardHolder cardHolder = player.GetComponent<CardHolder>();
            if (cardHolder == null)
            {
                cardHolder = player.gameObject.AddComponent<CardHolder>();
            }
            
            // Set up the player's hand transform
            if (cardHolder.playerHand == null)
            {
                // Create a hand container for each player
                GameObject handContainer = new GameObject($"{player.name}_Hand");
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null) handContainer.transform.SetParent(canvas.transform);
                cardHolder.playerHand = handContainer.transform;
            }
            
            // Temporarily set the CardDealer's playerHand to this player's hand
            Transform originalHand = cardDealer.playerHand;
            cardDealer.playerHand = cardHolder.playerHand;
            cardDealer.DealHand(cardsPerPlayer);
            cardDealer.playerHand = originalHand;
            
            Debug.Log($"Dealt {cardsPerPlayer} cards to {player.name}");
            
            yield return null;
        }
        
        Debug.Log($"Finished dealing cards. Cards remaining in deck: {cardManager.allCards.Count}");
    }
    
    void Update()
    {
         if (Input.GetKeyDown(KeyCode.T)) turnManager.NextTurn();
        if (Input.GetKeyDown(KeyCode.E)) envelope.ShowEnvelope();

        if (!isGameActive || gamePaused) return;
        
        // Handle game flow based on current state
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
        // Check if dice have been rolled
        if (diceManager.totalResult > 0)
        {
            waitingForRoll = false;
            waitingForMove = true;
            currentState = GameState.WaitingForMove;
            UpdateRollUI(diceManager.totalResult);
            Debug.Log($"Roll result: {diceManager.totalResult}. Ready to move.");
        }
        else
        {
            // Optional: Display message to roll dice
            if (rollResultText != null && !rollResultText.gameObject.activeSelf)
            {
                rollResultText.text = "Press SPACE to roll dice";
                rollResultText.gameObject.SetActive(true);
            }
        }
    }
    
    private void HandleMovePhase()
    {
        // Movement is handled by UnitController
        // Check if movement is complete by seeing if dice result is reset
        if (waitingForMove && diceManager.totalResult == 0)
        {
            waitingForMove = false;
            waitingForRoll = true;
            
            // Small delay after movement before next turn
            StartCoroutine(DelayBeforeNextTurn());
        }
    }
    
    private IEnumerator DelayBeforeNextTurn()
    {
        gamePaused = true;
        yield return new WaitForSeconds(postMoveDelay);
        
        // Move to next turn
        currentState = GameState.WaitingForRoll;
        UpdateTurnUI();
        
        // Reset for next player's turn
        waitingForRoll = true;
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
        
        // Return to waiting for roll (suggestion ends turn)
        currentState = GameState.WaitingForRoll;
        waitingForRoll = true;
        waitingForMove = false;
        
        // Force next turn after suggestion
        if (turnManager != null)
        {
            turnManager.NextTurn();
            UpdateTurnUI();
        }
        
        gamePaused = false;
    }
    
    public void OnAccusationMade(bool isCorrect, Transform accuser)
    {
        if (isCorrect)
        {
            EndGame(true, accuser);
        }
        else
        {
            // Incorrect accusation - player is eliminated
            StartCoroutine(HandleIncorrectAccusation(accuser));
        }
    }
    
    private IEnumerator HandleIncorrectAccusation(Transform eliminatedPlayer)
    {
        currentState = GameState.AccusationPhase;
        gamePaused = true;
        
        Debug.Log($"{eliminatedPlayer.name} made an incorrect accusation and is eliminated!");
        
        // Remove player from turn rotation
        // You'd need to implement elimination logic in TurnManager
        
        yield return new WaitForSeconds(2f);
        
        // Check if only one player remains
        if (turnManager.PlayerCount <= 1)
        {
            EndGame(true, turnManager.Players[0]);
        }
        else
        {
            // Continue game with next player
            currentState = GameState.WaitingForRoll;
            waitingForRoll = true;
            waitingForMove = false;
            UpdateTurnUI();
        }
        
        gamePaused = false;
    }
    
    private void EndGame(bool playerWon, Transform winner)
    {
        currentState = GameState.GameOver;
        isGameActive = false;
        
        string message = playerWon ? $"{winner.name} wins the game!" : "Game Over!";
        Debug.Log(message);
        
        // Reveal the envelope solution
        if (envelope != null)
        {
            envelope.SetGameOver(true);
        }
        
        // Show envelope button so players can see the solution
        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.gameObject.SetActive(true);
        }
        
        if (gameOverPanel != null)
        {
            if (gameOverText != null)
            {
                string solutionText = "";
                if (envelope != null)
                {
                    solutionText = $"\n\nThe solution was:\n{envelope.SuspectCard?.cardName}\n{envelope.WeaponCard?.cardName}\n{envelope.RoomCard?.cardName}";
                }
                gameOverText.text = message + solutionText;
            }
            gameOverPanel.SetActive(true);
        }
    }
    
    private void ShowEnvelope()
    {
        if (envelope != null)
        {
            envelope.ShowEnvelope();
        }
    }
    
    private void UpdateTurnUI()
    {
        if (turnManager != null && turnManager.CurrentPlayer != null)
        {
            if (turnText != null)
            {
                turnText.text = $"{turnManager.CurrentPlayer.name}'s Turn";
            }
            
            if (turnIndicator != null)
            {
                turnIndicator.transform.position = turnManager.CurrentPlayer.position + Vector3.up * 2f;
            }
        }
        
        // Reset roll display
        if (rollResultText != null)
        {
            if (currentState == GameState.WaitingForRoll)
            {
                rollResultText.text = "Press SPACE to roll dice";
                rollResultText.gameObject.SetActive(true);
            }
            else if (currentState == GameState.WaitingForMove)
            {
                rollResultText.gameObject.SetActive(true);
            }
            else
            {
                rollResultText.gameObject.SetActive(false);
            }
        }
    }
    
    private void UpdateRollUI(int rollResult)
    {
        if (rollResultText != null)
        {
            rollResultText.text = $"You rolled: {rollResult}\nClick on tiles to move";
        }
    }
    
    // Public method to reset game
    public void ResetGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
>>>>>>> Stashed changes
    }
}