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
        if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();
    }

    void Update()
    {
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.Space) && CanRoll())
            RollDice();

        if (rolling)
        {
            if (dice1.HasFinishedRolling() && dice2.HasFinishedRolling())
            {
                totalResult = dice1.finalResult + dice2.finalResult;
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

    public bool CanRoll()
    {
        if (rolling) return false;
        if (totalResult > 0) return false;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
                return false;
            if (GameManager.Instance.HasRolledThisTurn)
                return false;
        }
        return true;
    }

    public void RollDice()
    {
        if (!CanRoll()) return;

        rolling = true;
        totalResult = 0;

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