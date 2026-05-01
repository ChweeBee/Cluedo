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

    // currently a placeholder for future setup work.
    void Start() { }

    // listens for the space hotkey and polls dice settling each frame.
    void Update()
    {
        if (PauseManager.IsGamePaused) return;

        if (Input.GetKeyDown(KeyCode.Space) && CanRoll())
            RollDice();

        GetResults();
    }

    // detects when both dice have settled and stores the total.
    public void GetResults()
    {
        if (!rolling) return;

        //wait until both dice stop rolling
        if (dice1.HasFinishedRolling() && dice2.HasFinishedRolling())
        {
            totalResult = dice1.finalResult + dice2.finalResult;
            rolling = false;
            if (diceCamera != null) StartCoroutine(HideDiceCameraAfterDelay());
        }
    }

    // hides the dice camera a few seconds after the dice settle.
    IEnumerator HideDiceCameraAfterDelay()
    {
        yield return new WaitForSeconds(diceCameraHoldSeconds);
        if (diceCamera != null) diceCamera.Hide();
    }

    // returns true only when a fresh roll is allowed by the current game state.
    public bool CanRoll()
    {
        if (rolling) return false;
        if (totalResult > 0) return false;
        if (GameManager.Instance != null)
        {
            //check if game is in correct state
            if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
                return false;
            //makes sure player hasnt already rolled
            if (GameManager.Instance.HasRolledThisTurn)
                return false;
        }
        return true;
    }

    // launches both dice and switches to the dice camera.
    public void RollDice()
    {
        if (!CanRoll()) return;

        rolling = true;
        totalResult = 0;

        dice1.StartRoll();
        dice2.StartRoll();
        if (diceCamera != null) diceCamera.Show(dice1.transform, dice2.transform);
    }

    // restores a previously rolled total from the save file.
    public void ApplySavedRoll(int total)
    {
        rolling = false;
        totalResult = total;
    }
}
