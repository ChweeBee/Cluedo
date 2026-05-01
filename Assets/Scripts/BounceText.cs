using UnityEngine;

public class BounceText : MonoBehaviour
{
    public float bounceHeight = 0.1f;
    public float bounceSpeed = 2f;
    private float originalY;

    // remembers the starting y so the bounce stays centred there.
    void Start()
    {
        originalY = transform.position.y;
    }

    // applies a sine-wave vertical offset each frame.
    void Update()
    {
        Vector3 pos = transform.position;
        pos.y = originalY + Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
        transform.position = pos;
    }
}