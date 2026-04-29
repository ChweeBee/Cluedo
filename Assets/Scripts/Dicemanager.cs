using System.Collections;
using UnityEngine;

public class DiceManager : MonoBehaviour
{
    [SerializeField] DiceRoller dice1;
    [SerializeField] DiceRoller dice2;
    [SerializeField] DiceCamera diceCamera;
    [SerializeField] CameraController cameraController;
    [Tooltip("Seconds the dice cameras stay visible after both dice settle.")]
    [SerializeField] float diceCameraHoldSeconds = 3f;

    public int totalResult;

    private bool rolling = false;

    void Start()
    {
        Debug.Log("DiceManager started");
        if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();
    }

    void Update()
    {
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space detected");
        }

        if (Input.GetKeyDown(KeyCode.Space) && !rolling && totalResult <= 0)
        {
            Debug.Log("Rolling dice...");
            RollDice();
        }

        if (rolling)
        {
            if (dice1.HasFinishedRolling() && dice2.HasFinishedRolling())
            {
                totalResult = dice1.finalResult + dice2.finalResult;
                Debug.Log("Total roll: " + totalResult);
                rolling = false;
                if (diceCamera != null) StartCoroutine(HideDiceCameraAfterDelay());
            }
        }
    }

    IEnumerator HideDiceCameraAfterDelay()
    {
        yield return new WaitForSeconds(diceCameraHoldSeconds);
        if (diceCamera != null) diceCamera.Hide();
        if (cameraController != null)
        {
            cameraController.SetMode(CameraController.CameraMode.Default);
            cameraController.ResetIdleTimer();
        }
    }

    public void RollDice()
    {
        if (rolling || totalResult > 0)
        {
            Debug.Log("Dice already rolled this turn; ignoring roll request.");
            return;
        }

        rolling = true;
        totalResult = 0;

        Debug.Log("Calling StartRoll");
        dice1.StartRoll();
        dice2.StartRoll();
        if (diceCamera != null) diceCamera.Show(dice1.transform, dice2.transform);
    }

    public void ApplySavedRoll(int total)
    {
        rolling = false;
        totalResult = total;
    }
}