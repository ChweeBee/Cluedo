using System;
using System.Collections.Generic;

public enum CharacterId
{
    MissScarlet,
    ColonelMustard,
    MissPeacock,
    MisterGreen,
    ProfessorPlum,
    MissWhite,
    MonsieurBrunette,
    SargentGray
}

[Serializable]
public class PlayerSetup
{
    public CharacterId character;
    public bool isCPU;

    public bool IsAI { get => isCPU; set => isCPU = value; }

    // Persisted board position. -1,-1 means "no saved tile yet — use the spawner's default for this character".
    public int tileX = -1;
    public int tileY = -1;

    // Card names dealt to this player (matches Card.cardName).
    public List<string> handCardNames = new List<string>();

    public PlayerSetup() { }

    public PlayerSetup(CharacterId character, bool isCPU)
    {
        this.character = character;
        this.isCPU = isCPU;
    }

    public bool HasSavedTile => tileX >= 0 && tileY >= 0;
}

[Serializable]
public class EnvelopeSolution
{
    public string suspectCardName;
    public string weaponCardName;
    public string roomCardName;

    public bool IsValid =>
        !string.IsNullOrEmpty(suspectCardName) &&
        !string.IsNullOrEmpty(weaponCardName) &&
        !string.IsNullOrEmpty(roomCardName);
}

[Serializable]
public class GameSaveData
{
    public int slotIndex = -1;
    public List<PlayerSetup> players = new List<PlayerSetup>();
    public int currentTurnIndex = 0;
    public string savedAtUtc;

    // Last successful dice roll for the current turn. 0 means "no roll yet".
    public int lastDiceTotal = 0;

    // Whether the current player has already rolled this turn. Locks out re-rolls across saves/reloads.
    public bool hasRolledThisTurn = false;

    // Whether cards have been dealt for this save (so we don't re-deal on reload).
    public bool cardsDealt = false;

    // The envelope solution chosen at game start, persisted so it stays the same across reloads.
    public EnvelopeSolution envelope = new EnvelopeSolution();

    // Names of players (CluedoPlayer transforms / CharacterId names) that have been eliminated by a wrong accusation.
    public List<string> eliminatedPlayerNames = new List<string>();

    // Cards revealed face-up on the public table (matches Card.cardName).
    public List<string> publicHandCardNames = new List<string>();

    public bool IsValid => players != null && players.Count >= 2;
}
