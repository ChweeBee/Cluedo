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

    void Start() { }

    void Update()
    {
        if (PauseManager.IsGamePaused) return;

        //check for roll input
        if (Input.GetKeyDown(KeyCode.Space) && CanRoll())
            RollDice();

        GetResults();
    }

    //logic to detect when dice stop moving and hide camera
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

    IEnumerator HideDiceCameraAfterDelay()
    {
        yield return new WaitForSeconds(diceCameraHoldSeconds);
        if (diceCamera != null) diceCamera.Hide();
    }

    //checks for a legal roll
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

    //triggers roll and camera focus
    public void RollDice()
    {
        if (!CanRoll()) return;

        rolling = true;
        totalResult = 0;

        dice1.StartRoll();
        dice2.StartRoll();
        if (diceCamera != null) diceCamera.Show(dice1.transform, dice2.transform);
    }

    //restores roll from save data
    public void ApplySavedRoll(int total)
    {
        rolling = false;
        totalResult = total;
    }
}
