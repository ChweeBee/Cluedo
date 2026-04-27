using UnityEngine;

public class CameraDefault : MonoBehaviour
{
    Vector3 startPosition;
    Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void BeginDefault()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    public void EndDefault()
    {
    }
}
