using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    public int finalResult;

    private Rigidbody rb;
    private bool isRolling = false;
    public float rollDelay = 0.5f;
    private float timer = 0f;
    public float launchForce = 7f;


    void DetermineFinishedSide()
    {
        float closestDot = -1f;
        string resultLabel = "Unknown";

        // Checking all directions
        CheckDirection(transform.up, "2", ref closestDot, ref resultLabel);
        CheckDirection(-transform.up, "5", ref closestDot, ref resultLabel);
        CheckDirection(transform.right, "4", ref closestDot, ref resultLabel);
        CheckDirection(-transform.right, "3", ref closestDot, ref resultLabel);
        CheckDirection(transform.forward, "1", ref closestDot, ref resultLabel);
        CheckDirection(-transform.forward, "6", ref closestDot, ref resultLabel);

        // Convert the string result to an actual number
        finalResult = int.Parse(resultLabel);
        Debug.Log("You rolled a: " + finalResult);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

        //only allows roll if Space is pressed and dice isnt rolling
        if (Input.GetKeyDown(KeyCode.Space) && !isRolling)
        {
            StartRoll();
        }

        //if rolling count down the timer
        if (isRolling)
        {
            timer += Time.deltaTime;
            //only check if stopped after delay
            if(timer > rollDelay)
            {
                if(rb.linearVelocity.magnitude < 0.05f && rb.angularVelocity.magnitude < 0.05f)
                {
                    isRolling = false;
                    Debug.Log("Dice landed");

                    DetermineFinishedSide();
                }
            }

        }
    }


    //main method that deals with rolling
    void StartRoll()
    {
        isRolling = true;
        timer = 0f;

        //reset pos
        transform.position = new Vector3(0, 2, 0);
        transform.rotation = Random.rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //Applies force
        rb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);

    }

    void CheckDirection(Vector3 sideDirection, string label, ref float closestDot, ref string resultLabel)
    {
        float dot = Vector3.Dot(sideDirection, Vector3.up);

        if (dot > closestDot)
        {
            closestDot = dot;
            resultLabel = label;
        }
    }
}