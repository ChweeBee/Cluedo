using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    [Header("Framing")]
    [Tooltip("Horizontal distance from the target.")]
    [SerializeField] float followDistance = 5f;
    [Tooltip("Vertical offset above the target.")]
    [SerializeField] float followHeight = 4;
    [Tooltip("Vertical offset on the target where the camera looks.")]
    [SerializeField] float lookAtHeight = 1f;

    [Header("Edge Pan")]
    [Tooltip("Horizontal screen fraction (0..1) treated as dead zone in the center. 0.5 = middle 50% does nothing.")]
    [Range(0f, 1f)]
    [SerializeField] float deadZone = 0.5f;
    [Tooltip("Maximum orbit speed at the screen edge, in degrees per second.")]
    [SerializeField] float panSpeed = 90f;
    [Tooltip("Flip if the pan direction feels backwards.")]
    [SerializeField] bool invertPan = false;

    [Header("Transition")]
    [Tooltip("Time to glide from the current pose into the follow pose on enter.")]
    [SerializeField] float enterDuration = 0.5f;

    Transform target;
    float orbitAngle;
    Coroutine enterRoutine;
    bool active;

    public Transform Target => target;

    public void BeginMove(Transform newTarget)
    {
        EndMove();
        if (newTarget == null) return;
        target = newTarget;

        // Start orbit angle from the character's current facing so the camera enters from behind them.
        orbitAngle = AngleBehindTarget();

        CenterCursor();

        active = true;
        enterRoutine = StartCoroutine(EnterTransition());
    }

    void CenterCursor()
    {
        if (Mouse.current == null) return;
        Mouse.current.WarpCursorPosition(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
    }

    public void EndMove()
    {
        active = false;
        target = null;
        if (enterRoutine != null)
        {
            StopCoroutine(enterRoutine);
            enterRoutine = null;
        }
    }

    void LateUpdate()
    {
        if (!active || target == null) return;
        if (enterRoutine != null) return; // entry coroutine owns the pose during transition

        ApplyEdgePan();
        ApplyFollowPose();
    }

    void ApplyEdgePan()
    {
        // -1 at left edge, +1 at right edge, 0 at center.
        float screenX = Screen.width <= 0 ? 0f : (Input.mousePosition.x / Screen.width) * 2f - 1f;

        float halfDead = deadZone * 0.5f;
        float magnitude = 0f;
        if (screenX > halfDead)       magnitude = (screenX - halfDead) / (1f - halfDead);
        else if (screenX < -halfDead) magnitude = (screenX + halfDead) / (1f - halfDead);
        magnitude = Mathf.Clamp(magnitude, -1f, 1f);

        float sign = invertPan ? 1f : -1f;
        orbitAngle += magnitude * panSpeed * sign * Time.deltaTime;
    }

    void ApplyFollowPose()
    {
        transform.position = target.position + OffsetFromAngle(orbitAngle);
        transform.LookAt(target.position + Vector3.up * lookAtHeight);
    }

    Vector3 OffsetFromAngle(float angleDeg)
    {
        float r = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(r) * followDistance, followHeight, Mathf.Sin(r) * followDistance);
    }

    IEnumerator EnterTransition()
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float t = 0f;
        float dur = Mathf.Max(0.01f, enterDuration);
        while (t < 1f && target != null)
        {
            // Recompute target each frame so any rotation the character does during the fade-in is tracked.
            orbitAngle = AngleBehindTarget();
            Vector3 endPos = target.position + OffsetFromAngle(orbitAngle);
            Quaternion endRot = Quaternion.LookRotation((target.position + Vector3.up * lookAtHeight) - endPos);

            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(startPos, endPos, k);
            transform.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }
        enterRoutine = null;
    }

    float AngleBehindTarget()
    {
        Vector3 back = -target.forward;
        return Mathf.Atan2(back.z, back.x) * Mathf.Rad2Deg;
    }
}
