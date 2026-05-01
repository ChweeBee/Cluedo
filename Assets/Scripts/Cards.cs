using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public enum CardType {Suspect, Weapon, Room }

    public enum SuspectName { Scarlett, Mustard, White, Green, Peacock, Plum}
    public enum WeaponName { Candlestick, Knife, LeadPipe, Revolver, Rope, Wrench }
    public enum RoomName { Kitchen, Ballroom, Conservatory, BilliardRoom, Library, Study, Hall, Lounge, DiningRoom }

    [Header("Card Settings")]
    public CardType cardType;
    public SuspectName suspect;
    public WeaponName weapon;
    public RoomName room;
    public string cardName;

    [Header("UI References")]
    public TextMeshProUGUI nameLabel;
    public Image artworkDisplay;

    // editor-time hook that keeps the card label and artwork tint in sync with its type.
    private void OnValidate()
    {
        if (nameLabel != null)
        {
            nameLabel.text = cardName;
        }
        if (artworkDisplay != null)
        {
            if (cardType == CardType.Suspect) artworkDisplay.color = Color.red;
            else if (cardType == CardType.Weapon) artworkDisplay.color = Color.gray;
            else if (cardType == CardType.Room) artworkDisplay.color = Color.blue;
        }
    }
 }