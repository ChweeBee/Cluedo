using System.Collections;
using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [SerializeField] DiceRoller dice1;
    [SerializeField] DiceRoller dice2;
    [SerializeField] DiceCamera diceCamera;
    [Tooltip("Seconds the dice cameras stay visible after both dice settle.")]
    [SerializeField] float diceCameraHoldSeconds = 3f;

    public int totalResult;

    private bool rolling = false;

    void Start()
    {
        Debug.Log("DiceManager started");
    }

    void Update()
    {
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space detected");
        }

        if (Input.GetKeyDown(KeyCode.Space) && !rolling)
        {
            Debug.Log("Rolling dice...");
            RollDice();
        }

        GetResults();
    }

    public void GetResults()
    {
        // Created just tp be compatible with AIController
        if (dice1.HasFinishedRolling() && dice2.HasFinishedRolling())
        {
            totalResult = dice1.finalResult + dice2.finalResult;
            Debug.Log("Total roll: " + totalResult);
            rolling = false;
            if (diceCamera != null) StartCoroutine(HideDiceCameraAfterDelay());
        }
    }

    IEnumerator HideDiceCameraAfterDelay()
    {
        yield return new WaitForSeconds(diceCameraHoldSeconds);
        if (diceCamera != null) diceCamera.Hide();
    }

    public void RollDice()
    {
        rolling = true;
        totalResult = 0;

        Debug.Log("Calling StartRoll");
        dice1.StartRoll();
        dice2.StartRoll();
        if (diceCamera != null) diceCamera.Show(dice1.transform, dice2.transform);
    }
}
