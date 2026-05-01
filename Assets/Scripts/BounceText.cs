using UnityEngine;

public class BounceText : MonoBehaviour
{
    public float bounceHeight = 0.1f;
    public float bounceSpeed = 2f;
    private float originalY;

    void Start()
    {
        originalY = transform.position.y;
    }

    void Update()
    {
        Vector3 pos = transform.position;
        pos.y = originalY + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.position = pos;
    }
}