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

    public PlayerSetup() { }

    public PlayerSetup(CharacterId character, bool isCPU)
    {
        this.character = character;
        this.isCPU = isCPU;
    }
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
