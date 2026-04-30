using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public class Envelope : MonoBehaviour
{
    [Header("The Secret Solution")]
    [SerializeField] private Card suspectCard;
    [SerializeField] private Card weaponCard;
    [SerializeField] private Card roomCard;
    
    [Header("UI References")]
    [SerializeField] private GameObject envelopePanel;
    [SerializeField] private TMP_Text suspectText;
    [SerializeField] private TMP_Text weaponText;
    [SerializeField] private TMP_Text roomText;
    [SerializeField] private Button revealButton;
    [SerializeField] private Button closeButton;
    
    [Header("References")]
    [SerializeField] private CardManager cardManager;
    
    private bool isRevealed = false;
    private bool gameOver = false;
    
    // Public properties to access solution
    public Card SuspectCard => suspectCard;
    public Card WeaponCard => weaponCard;
    public Card RoomCard => roomCard;
    public bool IsRevealed => isRevealed;
    
    void Awake()
    {
        if (envelopePanel != null) envelopePanel.SetActive(false);
    }

    void Start()
    {
        if (cardManager == null) cardManager = FindAnyObjectByType<CardManager>();

        if (envelopePanel != null) envelopePanel.SetActive(false);
        
        if (revealButton != null) revealButton.onClick.AddListener(RevealEnvelope);
        if (closeButton != null) closeButton.onClick.AddListener(CloseEnvelope);
        
        // Initialize the envelope with random solution cards
        InitializeEnvelope();
    }
    
    private void InitializeEnvelope()
    {
        if (cardManager == null)
        {
            Debug.LogError("CardManager not found!");
            return;
        }

        cardManager.SortDeck();

        GameSaveData save = GameBootstrap.Instance != null ? GameBootstrap.Instance.Active : null;
        EnvelopeSolution saved = save != null ? save.envelope : null;

        if (saved != null && saved.IsValid)
        {
            suspectCard = FindCardByName(cardManager.suspectDeck, saved.suspectCardName);
            weaponCard = FindCardByName(cardManager.weaponDeck, saved.weaponCardName);
            roomCard = FindCardByName(cardManager.roomDeck, saved.roomCardName);
            Debug.Log($"Envelope restored from save: {suspectCard?.cardName} with {weaponCard?.cardName} in the {roomCard?.cardName}");
        }
        else
        {
            if (cardManager.suspectDeck.Count > 0)
                suspectCard = cardManager.suspectDeck[Random.Range(0, cardManager.suspectDeck.Count)];
            if (cardManager.weaponDeck.Count > 0)
                weaponCard = cardManager.weaponDeck[Random.Range(0, cardManager.weaponDeck.Count)];
            if (cardManager.roomDeck.Count > 0)
                roomCard = cardManager.roomDeck[Random.Range(0, cardManager.roomDeck.Count)];

            if (save != null && save.slotIndex >= 0)
            {
                save.envelope = new EnvelopeSolution
                {
                    suspectCardName = suspectCard != null ? suspectCard.cardName : null,
                    weaponCardName = weaponCard != null ? weaponCard.cardName : null,
                    roomCardName = roomCard != null ? roomCard.cardName : null
                };
                SaveSystem.Save(save.slotIndex, save);
            }

            Debug.Log($"Envelope initialized: {suspectCard?.cardName} with {weaponCard?.cardName} in the {roomCard?.cardName}");
        }

        if (suspectCard != null) cardManager.allCards.Remove(suspectCard);
        if (weaponCard != null) cardManager.allCards.Remove(weaponCard);
        if (roomCard != null) cardManager.allCards.Remove(roomCard);

        cardManager.SortDeck();
    }

    private static Card FindCardByName(List<Card> deck, string name)
    {
        if (deck == null || string.IsNullOrEmpty(name)) return null;
        return deck.Find(c => c != null && c.cardName == name);
    }
    
    public void RevealEnvelope()
    {
        if (gameOver)
        {
            if (envelopePanel != null)
            {
                UpdateEnvelopeDisplay();
                envelopePanel.SetActive(true);
            }
        }
        else
        {
            Debug.Log("Envelope can only be revealed after the game is over!");
        }
    }
    
    private void UpdateEnvelopeDisplay()
    {
        if (suspectText != null) suspectText.text = $"Suspect: {suspectCard?.cardName ?? "Unknown"}";
        if (weaponText != null) weaponText.text = $"Weapon: {weaponCard?.cardName ?? "Unknown"}";
        if (roomText != null) roomText.text = $"Room: {roomCard?.cardName ?? "Unknown"}";
    }
    
    private void CloseEnvelope()
    {
        if (envelopePanel != null) envelopePanel.SetActive(false);
    }
    
    public void SetGameOver(bool isGameOver)
    {
        gameOver = isGameOver;
        if (gameOver) isRevealed = true;
    }
    
    public bool CheckAccusation(Card accusedSuspect, Card accusedWeapon, Card accusedRoom)
    {
        bool isCorrect = (accusedSuspect == suspectCard && 
                         accusedWeapon == weaponCard && 
                         accusedRoom == roomCard);
        
        if (isCorrect)
        {
            Debug.Log("Accusation is CORRECT!");
        }
        else
        {
            Debug.Log("Accusation is INCORRECT!");
        }
        
        return isCorrect;
    }
    
    // Method to visually show the envelope (optional animation)
    public void ShowEnvelope()
    {
        if (envelopePanel != null && !envelopePanel.activeSelf)
        {
            UpdateEnvelopeDisplay();
            envelopePanel.SetActive(true);
        }
    }
    
    public void HideEnvelope()
    {
        if (envelopePanel != null) envelopePanel.SetActive(false);
    }
}