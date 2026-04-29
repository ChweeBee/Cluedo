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

    // Persisted board position. -1,-1 means "no saved tile yet — use the spawner's default for this character".
    public int tileX = -1;
    public int tileY = -1;

    public PlayerSetup() { }

    public PlayerSetup(CharacterId character, bool isCPU)
    {
        this.character = character;
        this.isCPU = isCPU;
    }

    public bool HasSavedTile => tileX >= 0 && tileY >= 0;
}

[Serializable]
public class GameSaveData
{
    public int slotIndex = -1;
    public List<PlayerSetup> players = new List<PlayerSetup>();
    public int currentTurnIndex = 0;
    public string savedAtUtc;

    public bool IsValid => players != null && players.Count >= 2;
}
