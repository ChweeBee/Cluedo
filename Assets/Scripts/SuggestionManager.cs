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
    private System.Action onSuggestionResolvedExternal;

    // hides every panel before any other script runs.
    void Awake()
    {
        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        if (accusationPanel != null) accusationPanel.SetActive(false);
        if (cardRevealPanel != null) cardRevealPanel.SetActive(false);
        ClearResultTexts();
    }

    // wipes the suggestion and accusation result strings.
    public void ClearResultTexts()
    {
        if (suggestionResultText != null) suggestionResultText.text = string.Empty;
        if (accusationResultText != null) accusationResultText.text = string.Empty;
    }

    // wires up dropdowns, buttons, and references on first run.
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

    // re-populates the dropdowns when external state changes.
    public void RefreshDropdowns() { SetupDropdowns(); }

    // listens for the suggestion and accusation hotkeys.
    void Update()
    {
        if (turnManager == null || gameOver || isWaitingForSuggestionResponse) return;
        if (turnManager.CurrentPlayer == null) return;
        if (GameManager.Instance != null && GameManager.Instance.HasSuggestedOrAccusedThisTurn) return;

        if (Input.GetKeyDown(KeyCode.S)) ShowSuggestionPanel();
        if (Input.GetKeyDown(KeyCode.A)) ShowAccusationPanel();
    }

    // fills every dropdown with the current decks plus any envelope-only entries.
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

    // legacy entry kept for room-driven suggestion triggers.
    public void StartSuggestion(string playerName, Room room)
    {
        Debug.Log("Starting suggestion for " + playerName + " in " + room.roomName);
        ShowSuggestionPanel();
    }

    // opens the suggestion panel if the current player is in a room.
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

    // opens the accusation panel.
    public void ShowAccusationPanel()
    {
        if (accusationPanel == null || gameOver) return;
        accusationPanel.SetActive(true);
        if (accusationResultText != null) accusationResultText.text = "";
    }

    // shows generic card-1, card-2 buttons so the disprover can pick which card to reveal.
    private void ShowCardChoicePanel(Transform disprover, List<Card> matchingCards, System.Action<Card> onCardChosen)
    {
        if (cardRevealPanel == null || cardButtonContainer == null || cardButtonPrefab == null)
        {
            Debug.LogWarning("[SuggestionManager] cardRevealPanel/container/prefab not wired — auto-picking first matching card.");
            if (matchingCards != null && matchingCards.Count > 0) onCardChosen?.Invoke(matchingCards[0]);
            return;
        }

        cardRevealPanel.SetActive(true);

        // clear any old buttons before rebuilding.
        foreach (Transform child in cardButtonContainer)
            DestroyImmediate(child.gameObject);

        // map each card to its slot in the disprover's hand for a stable label.
        List<Card> hand = disprover != null ? disprover.GetComponent<CluedoPlayer>()?.hand : null;

        foreach (Card card in matchingCards)
        {
            int handIndex = hand != null ? hand.IndexOf(card) : -1;
            int label = handIndex >= 0 ? handIndex + 1 : 0;

            GameObject btn = Instantiate(cardButtonPrefab, cardButtonContainer);
            btn.GetComponentInChildren<TMP_Text>().text = "Card " + label;
            Card captured = card;
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                cardRevealPanel.SetActive(false);
                onCardChosen(captured);
            });
        }
    }

    // confirms a human suggestion using the current dropdown values and current room.
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
        if (GameManager.Instance != null) GameManager.Instance.MarkSuggestedOrAccused();
        StartCoroutine(ResolveSuggestion());
    }

    // ai entry point for suggestions, fires onresolved when the round wraps up.
    public void MakeAISuggestion(Card suspect, Card weapon, Card room, System.Action onResolved)
    {
        suggestedSuspect = suspect;
        suggestedWeapon = weapon;
        suggestedRoom = room;
        onSuggestionResolvedExternal = onResolved;
        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.MarkSuggestedOrAccused();
        StartCoroutine(ResolveSuggestion());
    }

    // ai entry point for accusations, mirrors makeaccusation but skips the panel.
    public void MakeAIAccusation(Card suspect, Card weapon, Card room)
    {
        bool isCorrect = envelope != null && envelope.CheckAccusation(suspect, weapon, room);
        string accuserName = turnManager.CurrentPlayer.name;

        if (accusationPanel != null) accusationPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.MarkSuggestedOrAccused();

        if (isCorrect)
        {
            if (accusationResultText != null) accusationResultText.text = "CORRECT! " + accuserName + " wins!";
            GameManager.Instance.OnAccusationMade(true, accuserName);
        }
        else
        {
            if (accusationResultText != null) accusationResultText.text = "INCORRECT! " + accuserName + " is out!";
            StartCoroutine(EliminationAfterDelay(accuserName, 3f));
        }
    }

    // confirms a human accusation against the envelope solution.
    public void MakeAccusation()
    {
        if (accuseSuspectDropdown == null || accuseWeaponDropdown == null || accuseRoomDropdown == null) return;

        Card accusedSuspect = GetCardFromDropdownValue(accuseSuspectDropdown, cardManager.suspectDeck, envelope?.SuspectCard);
        Card accusedWeapon = GetCardFromDropdownValue(accuseWeaponDropdown, cardManager.weaponDeck, envelope?.WeaponCard);
        Card accusedRoom = GetCardFromDropdownValue(accuseRoomDropdown, cardManager.roomDeck, envelope?.RoomCard);

        bool isCorrect = envelope != null && envelope.CheckAccusation(accusedSuspect, accusedWeapon, accusedRoom);
        string accuserName = turnManager.CurrentPlayer.name;

        if (accusationPanel != null) accusationPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.MarkSuggestedOrAccused();

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
            StartCoroutine(EliminationAfterDelay(accuserName, 3f));
        }
    }

    // shows the elimination text for a few seconds before advancing the turn.
    private IEnumerator EliminationAfterDelay(string accuserName, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (accusationResultText != null) accusationResultText.text = string.Empty;
        if (GameManager.Instance != null) GameManager.Instance.OnAccusationMade(false, accuserName);
    }

    // closes the suggestion panel without finalizing.
    private void CancelSuggestion()
    {
        if (suggestionPanel != null) suggestionPanel.SetActive(false);
        ReturnToPostMoveActions();
    }

    // closes the accusation panel without finalizing.
    private void CancelAccusation()
    {
        if (accusationPanel != null) accusationPanel.SetActive(false);
        ReturnToPostMoveActions();
    }

    // hands control back to the post-move state if we were in suggestion or accusation phase.
    private void ReturnToPostMoveActions()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.SuggestionPhase ||
            GameManager.Instance.CurrentState == GameManager.GameState.AccusationPhase)
            GameManager.Instance.ReturnToPostMove();
    }

    // resolves a dropdown selection back to a card object, including envelope-only entries.
    private Card GetCardFromDropdownValue(TMP_Dropdown dropdown, List<Card> deckCards, Card envelopeCard)
    {
        string selectedName = dropdown.options[dropdown.value].text;
        Card found = deckCards.Find(c => c.cardName == selectedName);
        if (found != null) return found;
        if (envelopeCard != null && envelopeCard.cardName == selectedName) return envelopeCard;
        return null;
    }

    // walks around the table asking each player to disprove until one shows a card.
    private IEnumerator ResolveSuggestion()
    {
        isWaitingForSuggestionResponse = true;
        currentSuggester = turnManager.CurrentPlayer;
        playersToAsk = new List<Transform>();

        // build the ask order starting from the player after the suggester.
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
                AIPlayer aiDisprover = playerToAsk.GetComponent<AIPlayer>();
                bool disproverIsAI = aiDisprover != null && aiDisprover.enabled;

                // ai disprover skips the panel and uses its strategy directly.
                if (disproverIsAI)
                {
                    aiDisprover.Strategy?.BootstrapKnowledge();
                    chosen = aiDisprover.Strategy.PickDisproveCard(currentSuggester, suggestedSuspect, suggestedWeapon, suggestedRoom, matchingCards);
                }
                else
                {
                    int turnNumber = GetTurnNumberFor(playerToAsk);
                    string prompt = playerToAsk.name + ", select which card to show.\nPress " + turnNumber + " to see your cards";
                    if (GameManager.Instance != null) GameManager.Instance.SetRollResultText(prompt);

                    ShowCardChoicePanel(playerToAsk, matchingCards, c => chosen = c);
                    yield return new WaitUntil(() => chosen != null);
                }

                shownCard = chosen;
                Debug.Log(showingPlayer.name + " showed " + shownCard.cardName + " to " + currentSuggester.name);

                // teach the ai suggester that this card is now known.
                AIPlayer aiSuggester = currentSuggester.GetComponent<AIPlayer>();
                if (aiSuggester != null && aiSuggester.enabled)
                    aiSuggester.Strategy?.OnCardShownToMe(shownCard);

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

        var aiCallback = onSuggestionResolvedExternal;
        onSuggestionResolvedExternal = null;
        if (aiCallback != null) aiCallback();

        if (GameManager.Instance != null)
            GameManager.Instance.OnSuggestionFinished();
    }

    // returns the 1-based hotkey index of a given player.
    private int GetTurnNumberFor(Transform player)
    {
        if (turnManager == null || player == null) return 1;
        var players = turnManager.Players;
        for (int i = 0; i < players.Count; i++)
            if (players[i] == player) return i + 1;
        return 1;
    }

    // returns every card in a hand that matches one of the suggested cards.
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

    // debug helper for logging a card reveal.
    private void ShowCardToPlayer(Card card, Transform player)
    {
        Debug.Log("Showing " + card.cardName + " to " + player.name);
    }

    // delegates to the envelope component when the show button is clicked.
    private void ShowEnvelope()
    {
        if (envelope != null) envelope.ShowEnvelope();
    }

    // closes the accusation panel after a short delay.
    private IEnumerator CloseAccusationPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (accusationPanel != null) accusationPanel.SetActive(false);
    }
}