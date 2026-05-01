using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private TMP_Dropdown suspectDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private Button suggestButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text suggestionResultText;
    [SerializeField] private GameObject cardRevealPanel;
    [SerializeField] private Transform cardButtonContainer;
    [SerializeField] private GameObject cardButtonPrefab;

    [Header("Accusation UI")]
    [SerializeField] private GameObject accusationPanel;
    [SerializeField] private TMP_Dropdown accuseSuspectDropdown;
    [SerializeField] private TMP_Dropdown accuseWeaponDropdown;
    [SerializeField] private TMP_Dropdown accuseRoomDropdown;
    [SerializeField] private Button accuseButton;
    [SerializeField] private Button cancelAccuseButton;
    [SerializeField] private TMP_Text accusationResultText;

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

    void Awake()
    {
        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        if (accusationPanel != null) accusationPanel.SetActive(false);
        if (cardRevealPanel != null) cardRevealPanel.SetActive(false);
        ClearResultTexts();
    }

    public void ClearResultTexts()
    {
        if (suggestionResultText != null) suggestionResultText.text = string.Empty;
        if (accusationResultText != null) accusationResultText.text = string.Empty;
    }

    void Start()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();
        if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();
        if (envelope == null) envelope = FindAnyObjectByType<Envelope>();

        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        if (accusationPanel != null) accusationPanel.SetActive(false);
        if (cardRevealPanel != null) cardRevealPanel.SetActive(false);

        SetupDropdowns();

        if (suggestButton != null) suggestButton.onClick.AddListener(MakeSuggestion);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelSuggestion);
        if (accuseButton != null) accuseButton.onClick.AddListener(MakeAccusation);
        if (cancelAccuseButton != null) cancelAccuseButton.onClick.AddListener(CancelAccusation);
        if (showEnvelopeButton != null)
        {
            showEnvelopeButton.onClick.AddListener(ShowEnvelope);
            showEnvelopeButton.gameObject.SetActive(false);
        }
    }

    public void RefreshDropdowns() { SetupDropdowns(); }

    void Update()
    {
        if (turnManager == null || gameOver || isWaitingForSuggestionResponse) return;
        if (turnManager.CurrentPlayer == null) return;

        if (Input.GetKeyDown(KeyCode.S)) ShowSuggestionPanel();
        if (Input.GetKeyDown(KeyCode.A)) ShowAccusationPanel();
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
            if (envelope?.SuspectCard != null && !names.Contains(envelope.SuspectCard.cardName))
                names.Add(envelope.SuspectCard.cardName);
            accuseSuspectDropdown.AddOptions(names);
        }

        if (accuseWeaponDropdown != null)
        {
            accuseWeaponDropdown.ClearOptions();
            List<string> names = cardManager.weaponDeck.Select(c => c.cardName).ToList();
            if (envelope?.WeaponCard != null && !names.Contains(envelope.WeaponCard.cardName))
                names.Add(envelope.WeaponCard.cardName);
            accuseWeaponDropdown.AddOptions(names);
        }

        if (accuseRoomDropdown != null)
        {
            accuseRoomDropdown.ClearOptions();
            List<string> names = cardManager.roomDeck.Select(c => c.cardName).ToList();
            if (envelope?.RoomCard != null && !names.Contains(envelope.RoomCard.cardName))
                names.Add(envelope.RoomCard.cardName);
            accuseRoomDropdown.AddOptions(names);
        }
    }

    public void StartSuggestion(string playerName, Room room)
    {
        Debug.Log("Starting suggestion for " + playerName + " in " + room.roomName);
        ShowSuggestionPanel();
    }

    public void ShowSuggestionPanel()
    {
        if (suggestionPanel == null || gameOver || isWaitingForSuggestionResponse) return;

        Transform currentPlayer = turnManager.CurrentPlayer;
        Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);

        if (currentRoom == null)
        {
            Debug.Log("You must be in a room to make a suggestion");
            return;
        }

        suggestionPanel.SetActive(true);
        if (suggestionResultText != null) suggestionResultText.text = "";
        Debug.Log("Making suggestion in " + currentRoom.roomName);
    }

    public void ShowAccusationPanel()
    {
        if (accusationPanel == null || gameOver) return;
        accusationPanel.SetActive(true);
        if (accusationResultText != null) accusationResultText.text = "";
    }

    private void ShowCardChoicePanel(List<Card> matchingCards, System.Action<Card> onCardChosen)
    {
        if (cardRevealPanel == null || cardButtonContainer == null || cardButtonPrefab == null)
        {
            Debug.LogWarning("[SuggestionManager] cardRevealPanel/container/prefab not wired — auto-picking first matching card.");
            if (matchingCards != null && matchingCards.Count > 0) onCardChosen?.Invoke(matchingCards[0]);
            return;
        }

        cardRevealPanel.SetActive(true);

        foreach (Transform child in cardButtonContainer)
            DestroyImmediate(child.gameObject);

        foreach (Card card in matchingCards)
        {
            GameObject btn = Instantiate(cardButtonPrefab, cardButtonContainer);
            btn.GetComponentInChildren<TMP_Text>().text = card.cardName;
            Card captured = card;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                cardRevealPanel.SetActive(false);
                onCardChosen(captured);
            });
        }
    }

    public void MakeSuggestion()
    {
        if (suspectDropdown == null || weaponDropdown == null) return;

        suggestedSuspect = cardManager.suspectDeck[suspectDropdown.value];
        suggestedWeapon = cardManager.weaponDeck[weaponDropdown.value];

        Transform currentPlayer = turnManager.CurrentPlayer;
        Room currentRoom = roomManager.GetPlayerRoom(currentPlayer.name);

        if (currentRoom == null)
        {
            Debug.Log("Cannot make suggestion - not in a room");
            suggestionPanel.SetActive(false);
            return;
        }

        suggestedRoom = cardManager.roomDeck.Find(r => r.cardName == currentRoom.roomName);
        Debug.Log("Suggestion made: " + suggestedSuspect.cardName + ", " + suggestedWeapon.cardName + ", " + currentRoom.roomName);
        suggestionPanel.SetActive(false);
        StartCoroutine(ResolveSuggestion());
    }

    public void MakeAccusation()
    {
        if (accuseSuspectDropdown == null || accuseWeaponDropdown == null || accuseRoomDropdown == null) return;

        Card accusedSuspect = GetCardFromDropdownValue(accuseSuspectDropdown, cardManager.suspectDeck, envelope?.SuspectCard);
        Card accusedWeapon = GetCardFromDropdownValue(accuseWeaponDropdown, cardManager.weaponDeck, envelope?.WeaponCard);
        Card accusedRoom = GetCardFromDropdownValue(accuseRoomDropdown, cardManager.roomDeck, envelope?.RoomCard);

        bool isCorrect = envelope != null && envelope.CheckAccusation(accusedSuspect, accusedWeapon, accusedRoom);
        string accuserName = turnManager.CurrentPlayer.name;

        if (accusationPanel != null) accusationPanel.SetActive(false);

        if (isCorrect)
        {
            Debug.Log("[SuggestionManager] " + accuserName + " made a CORRECT accusation");
            if (accusationResultText != null) accusationResultText.text = "CORRECT! " + accuserName + " wins!";
            GameManager.Instance.OnAccusationMade(true, accuserName);
        }
        else
        {
            Debug.Log("[SuggestionManager] " + accuserName + " made an INCORRECT accusation");
            if (accusationResultText != null) accusationResultText.text = "INCORRECT! " + accuserName + " is out!";
            GameManager.Instance.OnAccusationMade(false, accuserName);
        }
    }

    private void CancelSuggestion()
    {
        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        ReturnToPostMoveActions();
    }

    private void CancelAccusation()
    {
        if (accusationPanel != null) accusationPanel.SetActive(false);
        ReturnToPostMoveActions();
    }

    private void ReturnToPostMoveActions()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.SuggestionPhase ||
            GameManager.Instance.CurrentState == GameManager.GameState.AccusationPhase)
            GameManager.Instance.ReturnToPostMove();
    }

    private Card GetCardFromDropdownValue(TMP_Dropdown dropdown, List<Card> deckCards, Card envelopeCard)
    {
        string selectedName = dropdown.options[dropdown.value].text;
        Card found = deckCards.Find(c => c.cardName == selectedName);
        if (found != null) return found;
        if (envelopeCard != null && envelopeCard.cardName == selectedName) return envelopeCard;
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
            List<Card> matchingCards = GetAllMatchingCards(playerToAsk);

            if (matchingCards.Count > 0)
            {
                showingPlayer = playerToAsk;
                Card chosen = null;
                ShowCardChoicePanel(matchingCards, c => chosen = c);
                yield return new WaitUntil(() => chosen != null);
                shownCard = chosen;
                Debug.Log(showingPlayer.name + " showed " + shownCard.cardName + " to " + currentSuggester.name);

                if (suggestionResultText != null)
                    suggestionResultText.text = showingPlayer.name + " showed " + shownCard.cardName + " to " + currentSuggester.name;
                break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        if (shownCard == null)
        {
            Debug.Log("No one could disprove " + currentSuggester.name + "'s suggestion");
            if (suggestionResultText != null)
                suggestionResultText.text = "No one could disprove " + currentSuggester.name + "'s suggestion.";
        }

        isWaitingForSuggestionResponse = false;
        yield return new WaitForSeconds(1f);

        if (GameManager.Instance != null)
            GameManager.Instance.OnSuggestionFinished();
    }

    private List<Card> GetAllMatchingCards(Transform player)
    {
        List<Card> matches = new List<Card>();
        CluedoPlayer cluedoPlayer = player.GetComponent<CluedoPlayer>();
        if (cluedoPlayer == null || cluedoPlayer.hand == null) return matches;

        foreach (Card card in cluedoPlayer.hand)
        {
            if (card.cardName == suggestedSuspect?.cardName ||
                card.cardName == suggestedWeapon?.cardName ||
                card.cardName == suggestedRoom?.cardName)
                matches.Add(card);
        }

        Debug.Log("Found " + matches.Count + " matching cards for " + player.name);
        return matches;
    }

    private void ShowCardToPlayer(Card card, Transform player)
    {
        Debug.Log("Showing " + card.cardName + " to " + player.name);
    }

    private void ShowEnvelope()
    {
        if (envelope != null) envelope.ShowEnvelope();
    }

    private IEnumerator CloseAccusationPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (accusationPanel != null) accusationPanel.SetActive(false);
    }
}