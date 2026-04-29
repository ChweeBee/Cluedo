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

    void Start()
    {
        Initialise();
    }
    
    void Initialise()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError($"[DiceRoller] No Rigidbody found on {gameObject.name}! Adding one...");
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        startPosition = transform.position;
        
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        hasInitialised = true;
        Debug.Log($"[DiceRoller] {gameObject.name} initialised");
    }

    void DetermineFinishedSide()
    {
        float closestDot = -1f;
        string resultLabel = "Unknown";

        // Safety check: if we can't determine, set a random result
        try
        {
            // Checking all directions
            CheckDirection(transform.up, "2", ref closestDot, ref resultLabel);
            CheckDirection(-transform.up, "5", ref closestDot, ref resultLabel);
            CheckDirection(transform.right, "4", ref closestDot, ref resultLabel);
            CheckDirection(-transform.right, "3", ref closestDot, ref resultLabel);
            CheckDirection(transform.forward, "1", ref closestDot, ref resultLabel);
            CheckDirection(-transform.forward, "6", ref closestDot, ref resultLabel);

            // Convert the string result to an actual number
            finalResult = int.Parse(resultLabel);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DiceRoller] Error determining dice face: {e.Message}. Using random result.");
            finalResult = Random.Range(1, 7);
        }
        
        Debug.Log($"[DiceRoller] {gameObject.name} rolled: {finalResult}");
    }

    void Update()
    {
        if (!hasInitialised) return;
        
        // If rolling count down the timer
        if (isRolling)
        {
            timer += Time.deltaTime;
            // Only check if stopped after delay
            if (timer > rollDelay)
            {
                // Check if rigidbody exists and has stopped moving
                if (rb != null)
                {
                    if (rb.linearVelocity.magnitude < 0.2f && rb.angularVelocity.magnitude < 0.2f)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        isRolling = false;
                        Debug.Log($"[DiceRoller] {gameObject.name} landed");

                        DetermineFinishedSide();
                    }
                }
                else
                {
                    // Fallback if rigidbody is missing
                    isRolling = false;
                    finalResult = Random.Range(1, 7);
                    Debug.LogWarning($"[DiceRoller] {gameObject.name} missing Rigidbody, using random result: {finalResult}");
                }
            }
        }
    }

    // Main method that deals with rolling
    public void StartRoll()
    {
        // Make sure we're initialised
        if (!hasInitialised)
        {
            Initialise();
        }
        
        // Check if rigidbody exists
        if (rb == null)
        {
            Debug.LogError($"[DiceRoller] {gameObject.name} has no Rigidbody! Cannot roll.");
            // Fallback: just set a random result
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

        // Apply force
        rb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
        
        Debug.Log($"[DiceRoller] {gameObject.name} started rolling");
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

    public bool HasFinishedRolling()
    {
        // Safety check: if rolling but took too long, force finish
        if (isRolling && timer > rollDelay + 3f)
        {
            Debug.LogWarning($"[DiceRoller] {gameObject.name} took too long to finish. Forcing completion.");
            isRolling = false;
            if (finalResult == 0)
            {
                finalResult = Random.Range(1, 7);
            }
        }
        
        return !isRolling && finalResult > 0;
    }
}