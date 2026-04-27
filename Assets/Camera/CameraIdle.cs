using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraIdle : MonoBehaviour
{
    enum IdleMove
    {
        PanAround,
        Orbit,
        SlowZoomIn,
        PullBack,
        Hold,
    }

    [Header("Targeting")]
    [SerializeField] string pointOfInterestTag = "CameraPOI";

    [Header("Timing")]
    [SerializeField] float minMoveDuration = 4f;
    [SerializeField] float maxMoveDuration = 7f;

    [Header("Framing")]
    [Tooltip("Distance from target during orbit / pan framing.")]
    [SerializeField] float framingDistance = 6f;
    [Tooltip("Vertical offset above the target while framed.")]
    [SerializeField] float framingHeight = 2.5f;
    [Tooltip("How far the slow zoom advances toward the target (0..1).")]
    [Range(0f, 0.9f)]
    [SerializeField] float zoomCloseness = 0.4f;
    [Tooltip("How far the pull-back retreats from the target.")]
    [SerializeField] float pullBackDistance = 4f;

    [Header("Pan / Orbit")]
    [SerializeField] float panMinArc = 30f;
    [SerializeField] float panMaxArc = 90f;
    [SerializeField] float orbitMinSpeed = 8f;
    [SerializeField] float orbitMaxSpeed = 18f;

    Transform[] cachedTargets;
    Transform lastTarget;
    IdleMove lastMove;
    bool hasLastMove;
    Coroutine loop;

    public void BeginIdle()
    {
        EndIdle();
        RefreshTargets();
        loop = StartCoroutine(IdleLoop());
    }

    public void EndIdle()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    IEnumerator IdleLoop()
    {
        while (true)
        {
            Transform target = PickTarget();
            if (target == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            IdleMove move = PickMove();
            float duration = Random.Range(minMoveDuration, maxMoveDuration);

            // Spectator-style hard cut into the new shot.
            SnapToFraming(target, Random.Range(0f, 360f));

            switch (move)
            {
                case IdleMove.PanAround:  yield return PanAround(target, duration); break;
                case IdleMove.Orbit:      yield return Orbit(target, duration); break;
                case IdleMove.SlowZoomIn: yield return SlowZoomIn(target, duration); break;
                case IdleMove.PullBack:   yield return PullBack(target, duration); break;
                case IdleMove.Hold:       yield return new WaitForSeconds(duration); break;
            }

            lastTarget = target;
            lastMove = move;
            hasLastMove = true;
        }
    }

    void RefreshTargets()
    {
        if (string.IsNullOrEmpty(pointOfInterestTag))
        {
            cachedTargets = new Transform[0];
            return;
        }

        GameObject[] found = GameObject.FindGameObjectsWithTag(pointOfInterestTag);
        cachedTargets = new Transform[found.Length];
        for (int i = 0; i < found.Length; i++) cachedTargets[i] = found[i].transform;
    }

    IdleMove PickMove()
    {
        int count = System.Enum.GetValues(typeof(IdleMove)).Length;
        if (count <= 1 || !hasLastMove) return (IdleMove)Random.Range(0, count);

        int offset = Random.Range(1, count);
        return (IdleMove)(((int)lastMove + offset) % count);
    }

    Transform PickTarget()
    {
        if (cachedTargets == null || cachedTargets.Length == 0) return null;
        if (cachedTargets.Length == 1) return cachedTargets[0];

        for (int attempt = 0; attempt < 4; attempt++)
        {
            Transform candidate = cachedTargets[Random.Range(0, cachedTargets.Length)];
            if (candidate != lastTarget) return candidate;
        }
        return cachedTargets[Random.Range(0, cachedTargets.Length)];
    }

    // -- Behaviours --------------------------------------------------------

    IEnumerator PanAround(Transform target, float duration)
    {
        // Hard cut to a starting angle, then arc around the target by a random sweep.
        float startAngle = AngleAround(target, transform.position);
        float sweep = Random.Range(panMinArc, panMaxArc) * (Random.value < 0.5f ? -1f : 1f);

        float t = 0f;
        while (t < 1f && target != null)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float angle = startAngle + sweep * k;
            transform.position = target.position + FramingOffset(angle);
            FaceTargetInstant(target);
            yield return null;
        }
    }

    IEnumerator Orbit(Transform target, float duration)
    {
        float angularSpeed = Random.Range(orbitMinSpeed, orbitMaxSpeed) * (Random.value < 0.5f ? -1f : 1f);
        float angle = AngleAround(target, transform.position);
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            angle += angularSpeed * Time.deltaTime;
            transform.position = target.position + FramingOffset(angle);
            FaceTargetInstant(target);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator SlowZoomIn(Transform target, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 toTarget = target.position - startPos;
        Vector3 endPos = startPos + toTarget * zoomCloseness;

        float t = 0f;
        while (t < 1f && target != null)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(startPos, endPos, k);
            FaceTargetInstant(target);
            yield return null;
        }
    }

    IEnumerator PullBack(Transform target, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 awayDir = (startPos - target.position).normalized;
        if (awayDir.sqrMagnitude < 0.0001f) awayDir = -transform.forward;
        Vector3 endPos = startPos + awayDir * pullBackDistance;

        float t = 0f;
        while (t < 1f && target != null)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(startPos, endPos, k);
            FaceTargetInstant(target);
            yield return null;
        }
    }

    // -- Helpers -----------------------------------------------------------

    void SnapToFraming(Transform target, float angleDegrees)
    {
        transform.position = target.position + FramingOffset(angleDegrees);
        FaceTargetInstant(target);
    }

    Vector3 FramingOffset(float angleDegrees)
    {
        float r = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(r) * framingDistance, framingHeight, Mathf.Sin(r) * framingDistance);
    }

    float AngleAround(Transform target, Vector3 worldPos)
    {
        Vector3 d = worldPos - target.position;
        return Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
    }

    void FaceTargetInstant(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }
}
