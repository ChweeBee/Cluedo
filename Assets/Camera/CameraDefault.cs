using UnityEngine;

public class CameraDefault : MonoBehaviour
{
    Vector3 startPosition;
    Quaternion startRotation;

    // captures the rest pose used as the default camera position.
    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    // snaps the camera back to the rest pose.
    public void BeginDefault()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    // placeholder for cleanup work when leaving default mode.
    public void EndDefault()
    {
    }
}
