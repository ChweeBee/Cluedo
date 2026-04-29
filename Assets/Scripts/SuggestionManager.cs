using UnityEngine;
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

    private bool isWaitingForSuggestionResponse = false;
    private bool gameOver = false;

    private List<Transform> playersToAsk;
    private int currentPlayerIndex = 0;
    private Transform currentSuggester;

    private Card suggestedSuspect;
    private Card suggestedWeapon;
    private Card suggestedRoom;

    void Start()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
        if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();
        if (envelope == null) envelope = FindAnyObjectByType<Envelope>();

        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        if (accusationPanel != null) accusationPanel.SetActive(false);

        SetupDropdowns();

        if (suggestButton != null)
            suggestButton.onClick.AddListener(MakeSuggestion);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => suggestionPanel.SetActive(false));

        if (accuseButton != null)
            accuseButton.onClick.AddListener(MakeAccusation);

        if (cancelAccuseButton != null)
            cancelAccuseButton.onClick.AddListener(() => accusationPanel.SetActive(false));

        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.onClick.AddListener(ShowEnvelope);
            showEnvelopeButton.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (turnManager == null || gameOver || isWaitingForSuggestionResponse)
            return;

        if (turnManager.CurrentPlayer == null)
            return;

        if (Input.GetKeyDown(KeyCode.S))
            ShowSuggestionPanel();

        if (Input.GetKeyDown(KeyCode.A))
            ShowAccusationPanel();
    }

    private void SetupDropdowns()
    {
        if (cardManager == null) return;

        if (suspectDropdown != null)
        {
            suspectDropdown.ClearOptions();
            suspectDropdown.AddOptions(cardManager.suspectDeck.Select(c => c.cardName).ToList());
        }

        if (weaponDropdown != null)
        {
            weaponDropdown.ClearOptions();
            weaponDropdown.AddOptions(cardManager.weaponDeck.Select(c => c.cardName).ToList());
        }

        if (accuseSuspectDropdown != null)
        {
            accuseSuspectDropdown.ClearOptions();
            List<string> names = cardManager.suspectDeck.Select(c => c.cardName).ToList();

            if (envelope != null && envelope.SuspectCard != null && !names.Contains(envelope.SuspectCard.cardName))
                names.Add(envelope.SuspectCard.cardName);

            accuseSuspectDropdown.AddOptions(names);
        }

        if (accuseWeaponDropdown != null)
        {
            accuseWeaponDropdown.ClearOptions();
            List<string> names = cardManager.weaponDeck.Select(c => c.cardName).ToList();

            if (envelope != null && envelope.WeaponCard != null && !names.Contains(envelope.WeaponCard.cardName))
                names.Add(envelope.WeaponCard.cardName);

            accuseWeaponDropdown.AddOptions(names);
        }

        if (accuseRoomDropdown != null)
        {
            accuseRoomDropdown.ClearOptions();
            List<string> names = cardManager.roomDeck.Select(c => c.cardName).ToList();

            if (envelope != null && envelope.RoomCard != null && !names.Contains(envelope.RoomCard.cardName))
                names.Add(envelope.RoomCard.cardName);

            accuseRoomDropdown.AddOptions(names);
        }
    }

public void StartSuggestion(string playerName, Room room)
{
    Debug.Log("Starting suggestion for " + playerName);
    Debug.Log(playerName + " suggests in " + room.roomName);

    Debug.Log("Suggestion complete. Press N to end turn.");

    ShowSuggestionPanel();
}


    public void ShowSuggestionPanel()
    {
        if (suggestionPanel == null || gameOver || isWaitingForSuggestionResponse)
            return;

        Transform currentPlayer = turnManager.CurrentPlayer;
        Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);

        if (currentRoom == null)
        {
            Debug.Log("You must be in a room to make a suggestion");
            return;
        }

        suggestionPanel.SetActive(true);

        if (suggestionResultText != null)
            suggestionResultText.text = "";

        Debug.Log("Making suggestion in " + currentRoom.roomName);
    }

    public void ShowAccusationPanel()
    {
        if (accusationPanel == null || gameOver)
            return;

        accusationPanel.SetActive(true);

        if (accusationResultText != null)
            accusationResultText.text = "";
    }

    public void MakeSuggestion()
    {
        if (suspectDropdown == null || weaponDropdown == null)
            return;

        suggestedSuspect = cardManager.suspectDeck[suspectDropdown.value];
        suggestedWeapon = cardManager.weaponDeck[weaponDropdown.value];

        Transform currentPlayer = turnManager.CurrentPlayer;
        Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);

        if (currentRoom == null)
        {
            Debug.Log("Cannot make suggestion because player is not in a room");
            suggestionPanel.SetActive(false);
            return;
        }

        suggestedRoom = cardManager.roomDeck.Find(r => r.cardName == currentRoom.roomName);

        Debug.Log(
            "Suggestion made: " +
            suggestedSuspect.cardName + ", " +
            suggestedWeapon.cardName + ", " +
            currentRoom.roomName
        );

        suggestionPanel.SetActive(false);
        StartCoroutine(ResolveSuggestion());
    }

    public void MakeAccusation()
    {
        if (accuseSuspectDropdown == null || accuseWeaponDropdown == null || accuseRoomDropdown == null)
            return;

        Card accusedSuspect = GetCardFromDropdownValue(accuseSuspectDropdown.value, cardManager.suspectDeck, envelope?.SuspectCard);
        Card accusedWeapon = GetCardFromDropdownValue(accuseWeaponDropdown.value, cardManager.weaponDeck, envelope?.WeaponCard);
        Card accusedRoom = GetCardFromDropdownValue(accuseRoomDropdown.value, cardManager.roomDeck, envelope?.RoomCard);

        bool isCorrect = envelope != null && envelope.CheckAccusation(accusedSuspect, accusedWeapon, accusedRoom);

        if (isCorrect)
        {
            Debug.Log(turnManager.CurrentPlayer.name + " made a correct accusation");

            if (accusationResultText != null)
                accusationResultText.text = "CORRECT! " + turnManager.CurrentPlayer.name + " wins!";

            GameManager.Instance.OnAccusationMade(true, turnManager.CurrentPlayer.name);
        }
        else
        {
            Debug.Log(turnManager.CurrentPlayer.name + " made an incorrect accusation");

            if (accusationResultText != null)
                accusationResultText.text = "INCORRECT! " + turnManager.CurrentPlayer.name + " is out!";

            StartCoroutine(HandleIncorrectAccusation());
        }

        StartCoroutine(CloseAccusationPanelAfterDelay(3f));
    }

    private Card GetCardFromDropdownValue(int dropdownValue, List<Card> deckCards, Card envelopeCard)
    {
        if (dropdownValue < deckCards.Count)
            return deckCards[dropdownValue];

        if (envelopeCard != null && dropdownValue == deckCards.Count)
            return envelopeCard;

        return null;
    }

    private IEnumerator ResolveSuggestion()
    {
        isWaitingForSuggestionResponse = true;
        currentSuggester = turnManager.CurrentPlayer;

        playersToAsk = new List<Transform>();

        int startIndex = turnManager.CurrentIndex;

        for (int i = 1; i < turnManager.PlayerCount; i++)
        {
            int nextIndex = (startIndex + i) % turnManager.PlayerCount;
            playersToAsk.Add(turnManager.Players[nextIndex]);
        }

        Card shownCard = null;
        Transform showingPlayer = null;

        for (currentPlayerIndex = 0; currentPlayerIndex < playersToAsk.Count; currentPlayerIndex++)
        {
            Transform playerToAsk = playersToAsk[currentPlayerIndex];

            Card cardToShow = GetPlayerCardMatchingSuggestion(playerToAsk);

            if (cardToShow != null)
            {
                shownCard = cardToShow;
                showingPlayer = playerToAsk;

                Debug.Log(showingPlayer.name + " showed " + shownCard.cardName + " to " + currentSuggester.name);
                ShowCardToPlayer(shownCard, currentSuggester);
                break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        if (shownCard == null)
            Debug.Log("No one could disprove " + currentSuggester.name + "'s suggestion");

        isWaitingForSuggestionResponse = false;

        yield return new WaitForSeconds(1f);
        //turnManager.NextTurn();
    }

    private Card GetPlayerCardMatchingSuggestion(Transform player)
    {
        CardHolder cardHolder = player.GetComponent<CardHolder>();

        if (cardHolder == null || cardHolder.playerHand == null)
            return null;

        foreach (Card card in cardHolder.playerHand.GetComponentsInChildren<Card>())
        {
            if (card == suggestedSuspect || card == suggestedWeapon || card == suggestedRoom)
                return card;
        }

        return null;
    }

    private void ShowCardToPlayer(Card card, Transform player)
    {
        Debug.Log("Showing " + card.cardName + " to " + player.name);
    }

    private IEnumerator HandleIncorrectAccusation()
    {
        Transform incorrectPlayer = turnManager.CurrentPlayer;

        Debug.Log(incorrectPlayer.name + " is eliminated from the game");

        //turnManager.NextTurn();



        GameManager.Instance.OnAccusationMade(false, incorrectPlayer.name);


        yield return new WaitForSeconds(2f);

        if (accusationPanel != null)
            accusationPanel.SetActive(false);

    }

    /*

    Obselete
    
    private void GameOver(bool playerWon, Transform winner)
    {
        gameOver = true;

        Debug.Log("Game Over! " + winner.name + " wins");

        if (showEnvelopeButton != null)
            showEnvelopeButton.gameObject.SetActive(true);

        if (envelope != null)
            envelope.SetGameOver(true);
    }
    */

    private void ShowEnvelope()
    {
        if (envelope != null)
            envelope.ShowEnvelope();
    }

    private IEnumerator CloseAccusationPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (accusationPanel != null)
            accusationPanel.SetActive(false);
    }
}