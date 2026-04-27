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
        if (Input.GetKeyDown(KeyCode.K))
        {
            DealHand(3);
        }
    }
}
