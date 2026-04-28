using UnityEngine;
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
}