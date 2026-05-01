using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    public int finalResult;

    [SerializeField] private Vector3 startPosition;
    private Rigidbody rb;
    private bool isRolling = false;
    public float rollDelay = 0.5f;
    private float timer = 0f;
    public float launchForce = 7f;
    
    private bool hasInitialised = false;

    // initialises the rigidbody on first run.
    void Start()
    {
        Initialise();
    }

    // grabs or adds the rigidbody and stashes the starting position.
    void Initialise()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        startPosition = transform.position;

        if (rb != null)
            rb.isKinematic = true;

        hasInitialised = true;
    }

    // figures out which face is pointing up after the dice settles.
    void DetermineFinishedSide()
    {
        float closestDot = -1f;
        string resultLabel = "Unknown";

        try
        {
            // check every face direction against world up.
            CheckDirection(transform.up, "2", ref closestDot, ref resultLabel);
            CheckDirection(-transform.up, "5", ref closestDot, ref resultLabel);
            CheckDirection(transform.right, "4", ref closestDot, ref resultLabel);
            CheckDirection(-transform.right, "3", ref closestDot, ref resultLabel);
            CheckDirection(transform.forward, "1", ref closestDot, ref resultLabel);
            CheckDirection(-transform.forward, "6", ref closestDot, ref resultLabel);

            // Convert the string result to an actual number
            finalResult = int.Parse(resultLabel);
        }
        catch
        {
            finalResult = Random.Range(1, 7);
        }
    }

    // ticks the roll timer and finalises the result once the dice has stopped moving.
    void Update()
    {
        if (!hasInitialised) return;

        if (isRolling)
        {
            timer += Time.deltaTime;
            // wait at least rolldelay before checking velocities.
            if (timer > rollDelay)
            {
                if (rb != null)
                {
                    if (rb.linearVelocity.magnitude < 0.2f && rb.angularVelocity.magnitude < 0.2f)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        isRolling = false;
                        DetermineFinishedSide();
                    }
                }
                else
                {
                    isRolling = false;
                    finalResult = Random.Range(1, 7);
                }
            }
        }
    }

    // resets the dice and launches it with a random impulse and torque.
    public void StartRoll()
    {
        // Make sure we're initialised
        if (!hasInitialised)
        {
            Initialise();
        }
        
        if (rb == null)
        {
            finalResult = Random.Range(1, 7);
            isRolling = false;
            return;
        }
        
        // Reset state
        rb.isKinematic = false;
        isRolling = true;
        timer = 0f;
        finalResult = 0;

        // Reset position
        transform.position = startPosition;
        transform.rotation = Random.rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
    }

    // tracks the face whose normal is closest to world up.
    void CheckDirection(Vector3 sideDirection, string label, ref float closestDot, ref string resultLabel)
    {
        float dot = Vector3.Dot(sideDirection, Vector3.up);

        if (dot > closestDot)
        {
            closestDot = dot;
            resultLabel = label;
        }
    }

    // returns true once the dice has settled, with a fallback random face if it stalls.
    public bool HasFinishedRolling()
    {
        if (isRolling && timer > rollDelay + 3f)
        {
            isRolling = false;
            if (finalResult == 0)
                finalResult = Random.Range(1, 7);
        }

        return !isRolling && finalResult > 0;
    }
}