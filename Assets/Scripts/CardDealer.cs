using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDealer : MonoBehaviour
{
    public CardManager cardManager;
    public Transform playerHand;

    public void DealHand(int amount)
    {
        while (playerHand.childCount > 0) 
        {
            DestroyImmediate(playerHand.GetChild(0).gameObject);
        }

        List<Card> pickedThisTurn = new List<Card>();

        for (int i = 0; i < amount; i++)
        {
            if (cardManager.allCards.Count > 0)
            {
                int randomIndex = Random.Range(0, cardManager.allCards.Count);
                Card cardPrefab = cardManager.allCards[randomIndex];
                if (!pickedThisTurn.Contains(cardPrefab))
                {
                    Instantiate(cardPrefab, playerHand);
                    pickedThisTurn.Add(cardPrefab);
                }
                else
                {
                    i--;
                }
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(playerHand.GetComponent<RectTransform>());
    }
    private void Update()
    {
        if (PauseManager.IsGamePaused) return;

<<<<<<< Updated upstream
        if (Input.GetKeyDown(KeyCode.K))
        {
            DealHand(3);
        }
=======
        if (Input.GetKeyDown(KeyCode.K)) DealToAllPlayers();

        // H now strictly toggles visibility
        if (Input.GetKeyDown(KeyCode.H)) ToggleAllHands();

        // 1 and 2 now trigger the "Refill UI" logic
        if (Input.GetKeyDown(KeyCode.Alpha1)) ShowHandByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ShowHandByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ShowHandByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ShowHandByIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ShowHandByIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ShowHandByIndex(5);
>>>>>>> Stashed changes
    }
}
