using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CluedoPlayer : MonoBehaviour
{
    public bool isHuman;
    public List<Card> hand = new List<Card>();
    public Transform handUI; //only assign UI for human player
}