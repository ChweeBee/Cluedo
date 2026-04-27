public class Card
{
    public enum CardType 
        { 
        Suspect, 
        Weapon, 
        Room 
        }
    public enum SuspectName 
        { 
        Scarlett, 
        Mustard, 
        White, 
        Green, 
        Peacock, 
        Plum 
        }
    public enum WeaponName
        {
         Candlestick, 
         Knife, 
         LeadPipe, 
         Revolver, 
         Rope, 
         Wrench
        }

    public CardType cardType;
    public SuspectName suspect;
    public WeaponName weapon;
    public Room.RoomType room;

    public string cardName;
}