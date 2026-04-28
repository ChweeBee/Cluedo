using UnityEngine;
<<<<<<< Updated upstream
using System.Collections.Generic;

public class SuggestionManager : MonoBehaviour
{
    TurnManager turnManager;
    GameManager gameManager;
    CardManager cardManager;

    bool waitingForSuggestion = false;
    bool waitingForAccusation = false;

    string currentPlayer;
    Room currentRoom;

    List<Card> availableSuspects;
    List<Card> availableWeapons;

    int selectedSuspectIndex = -1;
    int selectedWeaponIndex = -1;

    string pendingAccusationPlayer;
    string pendingAccusationSuspect;
    string pendingAccusationWeapon;
    string pendingAccusationRoom;

    void Start()
    {
        turnManager = FindAnyObjectByType<TurnManager>();
        gameManager = FindAnyObjectByType<GameManager>();
        cardManager = FindAnyObjectByType<CardManager>();

        Debug.Log("SuggestionManager Initialized");
    }

    void Update()
    {
        if (waitingForSuggestion)
            HandleSuggestionInput();

        if (waitingForAccusation)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                waitingForAccusation = false;

                MakeAccusation(
                    pendingAccusationPlayer,
                    pendingAccusationSuspect,
                    pendingAccusationWeapon,
                    pendingAccusationRoom
                );
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                waitingForAccusation = false;
                Debug.Log(pendingAccusationPlayer + " passes on accusation");
                turnManager.NextTurn();
            }
        }
    }

    public void StartSuggestion(string player, Room room)
    {
        Debug.Log("Starting suggestion for " + player);

        currentPlayer = player;
        currentRoom = room;

        List<Card> playerHand = gameManager.playerHands[currentPlayer];

        availableSuspects = new List<Card>();
        availableWeapons = new List<Card>();

        foreach (Card suspect in cardManager.suspectDeck)
        {
            if (!playerHand.Contains(suspect))
                availableSuspects.Add(suspect);
        }

        foreach (Card weapon in cardManager.weaponDeck)
        {
            if (!playerHand.Contains(weapon))
                availableWeapons.Add(weapon);
        }

        Debug.Log("Suggestion Phase");
        Debug.Log(currentPlayer + " is in " + currentRoom.roomName);
        Debug.Log("Choose suspect:");

        for (int i = 0; i < availableSuspects.Count; i++)
        {
            Debug.Log((i + 1) + ". " + availableSuspects[i].cardName);
        }

        waitingForSuggestion = true;
        selectedSuspectIndex = -1;
        selectedWeaponIndex = -1;
    }

    void HandleSuggestionInput()
    {
        if (selectedSuspectIndex == -1)
        {
            for (int i = 0; i < availableSuspects.Count; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedSuspectIndex = i;

                    Debug.Log(
                        "Selected: " +
                        availableSuspects[selectedSuspectIndex].cardName
                    );

                    Debug.Log("Choose weapon:");

                    for (int w = 0; w < availableWeapons.Count; w++)
                    {
                        Debug.Log((w + 1) + ". " + availableWeapons[w].cardName);
                    }

                    return;
                }
            }
        }
        else if (selectedWeaponIndex == -1)
        {
            for (int i = 0; i < availableWeapons.Count; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedWeaponIndex = i;

                    Debug.Log(
                        "Selected: " +
                        availableWeapons[selectedWeaponIndex].cardName
                    );

                    string suspectName =
                        availableSuspects[selectedSuspectIndex].cardName;

                    string weaponName =
                        availableWeapons[selectedWeaponIndex].cardName;

                    waitingForSuggestion = false;

                    ProcessSuggestion(
                        currentPlayer,
                        currentRoom,
                        suspectName,
                        weaponName
                    );

                    return;
                }
            }
        }
    }

    void ProcessSuggestion(string player, Room room, string suspect, string weapon)
    {
        Debug.Log(
            player + " suggests " +
            suspect + ", " +
            weapon + ", " +
            room.roomName
        );

        bool disproved = false;

        IReadOnlyList<Transform> players = turnManager.Players;
        int startIndex = turnManager.CurrentIndex;

        for (int offset = 1; offset < players.Count; offset++)
        {
            int idx = (startIndex + offset) % players.Count;
            string otherPlayer = players[idx].name;

            if (gameManager.IsEliminated(otherPlayer))
                continue;

            List<Card> hand = gameManager.playerHands[otherPlayer];

            Card match = hand.Find(c =>
                c.cardName == suspect ||
                c.cardName == weapon ||
                c.cardName == room.roomName
            );

            if (match != null)
            {
                Debug.Log(otherPlayer + " disproves with " + match.cardName);
                disproved = true;
                break;
            }
            else
            {
                Debug.Log(otherPlayer + " cannot disprove");
            }
        }

        if (!disproved)
        {
            Debug.Log("Nobody could disprove");
            Debug.Log("Press A to accuse or P to pass");

            pendingAccusationPlayer = player;
            pendingAccusationSuspect = suspect;
            pendingAccusationWeapon = weapon;
            pendingAccusationRoom = room.roomName;

            waitingForAccusation = true;
        }
        else
        {
            turnManager.NextTurn();
        }
    }

    public void MakeAccusation(
        string player,
        string suspect,
        string weapon,
        string room
    )
    {
        Debug.Log(player + " accuses: " + suspect + ", " + weapon + ", " + room);

        if (gameManager.CheckAccusation(suspect, weapon, room))
        {
            Debug.Log(player + " wins");
            turnManager.EndGame(player);
        }
        else
        {
            Debug.Log(player + " eliminated");

            gameManager.EliminatePlayer(player);

            int activePlayers = 0;
            string lastPlayer = "";

            foreach (Transform p in turnManager.Players)
            {
                if (!gameManager.IsEliminated(p.name))
                {
                    activePlayers++;
                    lastPlayer = p.name;
                }
            }

            if (activePlayers == 0)
            {
                turnManager.EndGame("Nobody");
            }
            else if (activePlayers == 1)
            {
                turnManager.EndGame(lastPlayer);
            }
            else
            {
                turnManager.NextTurn();
            }
        }
    }
=======
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class SuggestionManager : MonoBehaviour
{
[Header("References")]
[SerializeField] private TurnManager turnManager;
[SerializeField] private CardManager cardManager;
[SerializeField] private RoomManager roomManager;
[SerializeField] private Envelope envelope;

[Header("Suggestion UI")]
[SerializeField] private GameObject suggestionPanel;
[SerializeField] private Dropdown suspectDropdown;
[SerializeField] private Dropdown weaponDropdown;
[SerializeField] private Button suggestButton;
[SerializeField] private Button cancelButton;
[SerializeField] private Text suggestionResultText;

[Header("Accusation UI")]
[SerializeField] private GameObject accusationPanel;
[SerializeField] private Dropdown accuseSuspectDropdown;
[SerializeField] private Dropdown accuseWeaponDropdown;
[SerializeField] private Dropdown accuseRoomDropdown;
[SerializeField] private Button accuseButton;
[SerializeField] private Button cancelAccuseButton;
[SerializeField] private Text accusationResultText;

[Header("Envelope UI")]
[SerializeField] private Button showEnvelopeButton;

// Current suggestion/accusation state
private bool isWaitingForSuggestionResponse = false;
private bool gameOver = false;

// Track which players have been asked for each suggestion
private List<Transform> playersToAsk;
private int currentPlayerIndex = 0;
private Transform currentSuggester;
private Card suggestedSuspect;
private Card suggestedWeapon;
private Card suggestedRoom;

void Start()
{
    // Find references if not assigned
    if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
    if (cardManager == null) cardManager = FindObjectOfType<CardManager>();
    if (roomManager == null) roomManager = FindObjectOfType<RoomManager>();
    if (envelope == null) envelope = FindObjectOfType<Envelope>();
    
    // Setup UI
    if (suggestionPanel != null) suggestionPanel.SetActive(false);
    if (accusationPanel != null) accusationPanel.SetActive(false);
    
    // Setup dropdowns
    SetupDropdowns();
    
    // Setup envelope button
    if (showEnvelopeButton != null)
    {
        showEnvelopeButton.onClick.AddListener(ShowEnvelope);
        showEnvelopeButton.gameObject.SetActive(false); // Hidden until game over
    }
}

void Update()
{
    // Handle suggestion/accusation input (only on current player's turn)
    if (turnManager != null && !gameOver && !isWaitingForSuggestionResponse)
    {
        Transform currentPlayer = turnManager.CurrentPlayer;
        if (currentPlayer != null)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                ShowSuggestionPanel();
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                ShowAccusationPanel();
            }
        }
    }
}

private void SetupDropdowns()
{
    // Setup suspect dropdowns
    if (suspectDropdown != null)
    {
        suspectDropdown.ClearOptions();
        List<string> suspectNames = cardManager.suspectDeck.Select(c => c.cardName).ToList();
        suspectDropdown.AddOptions(suspectNames);
    }
    
    if (accuseSuspectDropdown != null)
    {
        accuseSuspectDropdown.ClearOptions();
        List<string> suspectNames = cardManager.suspectDeck.Select(c => c.cardName).ToList();
        // Add envelope's suspect card if it's not in the deck (it was removed)
        if (envelope != null && envelope.SuspectCard != null && !suspectNames.Contains(envelope.SuspectCard.cardName))
        {
            suspectNames.Add(envelope.SuspectCard.cardName);
        }
        accuseSuspectDropdown.AddOptions(suspectNames);
    }
    
    // Setup weapon dropdowns
    if (weaponDropdown != null)
    {
        weaponDropdown.ClearOptions();
        List<string> weaponNames = cardManager.weaponDeck.Select(c => c.cardName).ToList();
        weaponDropdown.AddOptions(weaponNames);
    }
    
    if (accuseWeaponDropdown != null)
    {
        accuseWeaponDropdown.ClearOptions();
        List<string> weaponNames = cardManager.weaponDeck.Select(c => c.cardName).ToList();
        if (envelope != null && envelope.WeaponCard != null && !weaponNames.Contains(envelope.WeaponCard.cardName))
        {
            weaponNames.Add(envelope.WeaponCard.cardName);
        }
        accuseWeaponDropdown.AddOptions(weaponNames);
    }
    
    // Setup room dropdowns for accusation
    if (accuseRoomDropdown != null)
    {
        accuseRoomDropdown.ClearOptions();
        List<string> roomNames = cardManager.roomDeck.Select(c => c.cardName).ToList();
        if (envelope != null && envelope.RoomCard != null && !roomNames.Contains(envelope.RoomCard.cardName))
        {
            roomNames.Add(envelope.RoomCard.cardName);
        }
        accuseRoomDropdown.AddOptions(roomNames);
    }
}



/*
public void ShowSuggestionPanel()
{
Transform currentPlayer = turnManager.CurrentPlayer;
Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);

if (currentRoom != null)
{
    Debug.Log($"Suggestion triggered in {currentRoom.roomName}");
    StartCoroutine(ResolveSuggestion());
}
else
{
    Debug.Log("Must be in a room to suggest!");
}
}

public void ShowAccusationPanel()
{
    if (accusationPanel != null && !gameOver && !isWaitingForSuggestionResponse)
    {
        accusationPanel.SetActive(true);
        accusationResultText.text = "";
    }
}
*/



public void ShowSuggestionPanel()
{
    if (suggestionPanel != null && !gameOver && !isWaitingForSuggestionResponse)
    {
        // Get current player's room
        Transform currentPlayer = turnManager.CurrentPlayer;
        Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);
        
        if (currentRoom != null)
        {
            suggestionPanel.SetActive(true);
            suggestionResultText.text = "";
            
            // Auto-select current room in suggestion (optional UI feedback)
            Debug.Log($"Making suggestion in {currentRoom.roomName}");
        }
        else
        {
            Debug.Log("You must be in a room to make a suggestion!");
            StartCoroutine(ShowTempMessage("Must be in a room to suggest!", 2f));
        }
    }
}

public void ShowAccusationPanel()
{
Debug.Log("Accusation triggered");
MakeAccusation();
}

public void MakeSuggestion()
{
    if (suspectDropdown == null || weaponDropdown == null) return;
    
    // Get selected cards
    suggestedSuspect = cardManager.suspectDeck[suspectDropdown.value];
    suggestedWeapon = cardManager.weaponDeck[weaponDropdown.value];
    
    // Get current room
    Transform currentPlayer = turnManager.CurrentPlayer;
    Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);
    
    if (currentRoom == null)
    {
        Debug.Log("Cannot make suggestion - not in a room!");
        suggestionPanel.SetActive(false);
        return;
    }
    
    suggestedRoom = cardManager.roomDeck.Find(r => r.cardName == currentRoom.roomType.ToString());
    
    Debug.Log($"Suggestion made: {suggestedSuspect.cardName} with {suggestedWeapon.cardName} in {currentRoom.roomName}");
    
    // Close suggestion panel
    suggestionPanel.SetActive(false);
    
    // Start the suggestion resolution process
    StartCoroutine(ResolveSuggestion());
}


/*
public void MakeAccusation()
{
// TEMP: testing with first card in each deck
Card accusedSuspect = cardManager.suspectDeck[0];
Card accusedWeapon = cardManager.weaponDeck[0];
Card accusedRoom = cardManager.roomDeck[0];

bool isCorrect = envelope.CheckAccusation(accusedSuspect, accusedWeapon, accusedRoom);
Debug.Log(isCorrect ? "Correct accusation!" : "Wrong accusation!");
}
*/


public void MakeAccusation()
{
    if (accuseSuspectDropdown == null || accuseWeaponDropdown == null || accuseRoomDropdown == null) return;
    
    // Get the selected cards
    Card accusedSuspect = GetCardFromDropdownValue(accuseSuspectDropdown.value, cardManager.suspectDeck, envelope?.SuspectCard);
    Card accusedWeapon = GetCardFromDropdownValue(accuseWeaponDropdown.value, cardManager.weaponDeck, envelope?.WeaponCard);
    Card accusedRoom = GetCardFromDropdownValue(accuseRoomDropdown.value, cardManager.roomDeck, envelope?.RoomCard);
    
    // Check accusation against the envelope
    bool isCorrect = envelope != null && envelope.CheckAccusation(accusedSuspect, accusedWeapon, accusedRoom);
    
    if (isCorrect)
    {
        Debug.Log($"{turnManager.CurrentPlayer.name} made a CORRECT accusation! Game Over!");
        accusationResultText.text = $"CORRECT! {turnManager.CurrentPlayer.name} wins!\n\nThe envelope contains:\n{accusedSuspect.cardName}\n{accusedWeapon.cardName}\n{accusedRoom.cardName}";
        gameOver = true;
        
        // Show envelope button
        if (showEnvelopeButton != null) showEnvelopeButton.gameObject.SetActive(true);
        
        // Trigger game over sequence
        GameOver(true, turnManager.CurrentPlayer);
    }
    else
    {
        Debug.Log($"{turnManager.CurrentPlayer.name} made an INCORRECT accusation!");
        accusationResultText.text = $"INCORRECT! {turnManager.CurrentPlayer.name} is out!\n\nThe correct solution is still in the envelope.";
        
        // Remove player from game or skip their turns
        StartCoroutine(HandleIncorrectAccusation());
    }
    
    // Close accusation panel after delay
    StartCoroutine(CloseAccusationPanelAfterDelay(3f));
}


private Card GetCardFromDropdownValue(int dropdownValue, List<Card> deckCards, Card envelopeCard)
{
    if (dropdownValue < deckCards.Count)
    {
        return deckCards[dropdownValue];
    }
    else if (envelopeCard != null && dropdownValue == deckCards.Count)
    {
        return envelopeCard;
    }
    return null;
}

private IEnumerator ResolveSuggestion()
{
    isWaitingForSuggestionResponse = true;
    currentSuggester = turnManager.CurrentPlayer;
    
    // Get all other players in order
    playersToAsk = new List<Transform>();
    int startIndex = turnManager.CurrentIndex;
    
    for (int i = 1; i < turnManager.PlayerCount; i++)
    {
        int nextIndex = (startIndex + i) % turnManager.PlayerCount;
        playersToAsk.Add(turnManager.Players[nextIndex]);
    }
    
    currentPlayerIndex = 0;
    
    // Ask each player if they have any of the suggested cards
    Card shownCard = null;
    Transform showingPlayer = null;
    
    while (currentPlayerIndex < playersToAsk.Count && shownCard == null)
    {
        Transform playerToAsk = playersToAsk[currentPlayerIndex];
        
        // Check if this player has any of the suggested cards in their hand
        Card cardToShow = GetPlayerCardMatchingSuggestion(playerToAsk);
        
        if (cardToShow != null)
        {
            shownCard = cardToShow;
            showingPlayer = playerToAsk;
            Debug.Log($"{showingPlayer.name} showed {shownCard.cardName} to {currentSuggester.name}");
            
            // Show card to the suggester
            ShowCardToPlayer(shownCard, currentSuggester);
            
            // Update UI with result
            if (suggestionResultText != null && suggestionPanel.activeSelf)
            {
                suggestionResultText.text = $"{showingPlayer.name} showed you the {shownCard.cardName}!";
            }
        }
        
        currentPlayerIndex++;
        yield return new WaitForSeconds(0.5f);
    }
    
    if (shownCard == null)
    {
        Debug.Log($"No one could disprove {currentSuggester.name}'s suggestion!");
        if (suggestionResultText != null && suggestionPanel.activeSelf)
        {
            suggestionResultText.text = "No one could disprove your suggestion!";
        }
    }
    
    isWaitingForSuggestionResponse = false;
    
    // End the suggester's turn after suggestion
    if (turnManager != null && currentSuggester == turnManager.CurrentPlayer)
    {
        yield return new WaitForSeconds(1f);
        // Force next turn
        turnManager.NextTurn();
    }
}

private Card GetPlayerCardMatchingSuggestion(Transform player)
{
    CardHolder cardHolder = player.GetComponent<CardHolder>();
    if (cardHolder != null && cardHolder.playerHand != null)
    {
        foreach (Card card in cardHolder.playerHand.GetComponentsInChildren<Card>())
        {
            if (card == suggestedSuspect || card == suggestedWeapon || card == suggestedRoom)
            {
                return card;
            }
        }
    }
    
    return null;
}

private void ShowCardToPlayer(Card card, Transform player)
{
    // Implementation to show card UI to specific player
    Debug.Log($"Showing {card.cardName} to {player.name}");
    
    // You can implement a UI popup that only appears for the suggester
    // For now, we'll just log it
}

private void ShowEnvelope()
{
    if (envelope != null)
    {
        envelope.ShowEnvelope();
    }
}

private IEnumerator HandleIncorrectAccusation()
{
    // Disable the player who made incorrect accusation
    Transform incorrectPlayer = turnManager.CurrentPlayer;
    
    // You could mark them as inactive here
    Debug.Log($"{incorrectPlayer.name} is eliminated from the game!");
    
    // Remove them from turn rotation (simplified)
    turnManager.NextTurn();
    
    yield return new WaitForSeconds(2f);
    accusationPanel.SetActive(false);
    
    // Check if only one player remains
    if (turnManager.PlayerCount <= 1)
    {
        GameOver(true, turnManager.CurrentPlayer);
    }
}

private void GameOver(bool playerWon, Transform winner)
{
    gameOver = true;
    Debug.Log($"Game Over! {winner.name} wins!");
    
    // Show envelope button so players can see the solution
    if (showEnvelopeButton != null) showEnvelopeButton.gameObject.SetActive(true);
    
    // Reveal the envelope
    if (envelope != null) envelope.SetGameOver(true);
}

private IEnumerator ShowTempMessage(string message, float duration)
{
    Debug.Log(message);
    yield return new WaitForSeconds(duration);
}

private IEnumerator CloseAccusationPanelAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    if (accusationPanel != null) accusationPanel.SetActive(false);
}
}

public static class ListExtensions
{
public static T Find<T>(this List<T> list, System.Predicate<T> match)
{
    foreach (T item in list)
    {
        if (match(item)) return item;
    }
    return default(T);
}
>>>>>>> Stashed changes
}