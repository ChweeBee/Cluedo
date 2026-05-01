using UnityEngine;

public class DiceCamera : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Canvas holding the two RawImages displaying the dice cameras.")]
    [SerializeField] Canvas canvas;
    [Tooltip("Camera assigned to the first die. Should render to a RenderTexture displayed by a RawImage on the canvas.")]
    [SerializeField] Camera dieCameraA;
    [Tooltip("Camera assigned to the second die. Same setup as the first.")]
    [SerializeField] Camera dieCameraB;

    [Header("Targets")]
    [SerializeField] Transform dieA;
    [SerializeField] Transform dieB;

    [Header("Orbit")]
    [Tooltip("Horizontal distance from the die.")]
    [SerializeField] float radius = 2.5f;
    [Tooltip("Vertical offset above the die.")]
    [SerializeField] float height = 1.6f;
    [Tooltip("Orbit speed in degrees per second.")]
    [SerializeField] float orbitSpeed = 30f;
    [Tooltip("Starting angle offset for the second camera so the two views differ.")]
    [SerializeField] float secondCameraOffset = 180f;

    float angleA;
    float angleB;
    bool active;

    // hides the dice canvas and cameras at scene start.
    void Awake()
    {
        SetVisible(false);
    }

    // overload that uses the inspector-assigned dice transforms.
    public void Show()
    {
        Show(dieA, dieB);
    }

    // points the orbit cameras at two dice and reveals the canvas.
    public void Show(Transform a, Transform b)
    {
        dieA = a;
        dieB = b;
        angleA = 0f;
        angleB = secondCameraOffset;
        active = true;
        SetVisible(true);
    }

    // hides the dice canvas and stops the orbit update.
    public void Hide()
    {
        active = false;
        SetVisible(false);
    }

    // toggles the canvas and both render cameras together.
    void SetVisible(bool on)
    {
        if (canvas != null) canvas.enabled = on;
        if (dieCameraA != null) dieCameraA.enabled = on;
        if (dieCameraB != null) dieCameraB.enabled = on;
    }

    // orbits both cameras around their dice each frame while active.
    void LateUpdate()
    {
        if (!active) return;
        UpdateOrbit(dieCameraA, dieA, ref angleA);
        UpdateOrbit(dieCameraB, dieB, ref angleB);
    }

    // advances one camera's orbit angle and repositions it.
    void UpdateOrbit(Camera cam, Transform die, ref float angle)
    {
        if (cam == null || die == null) return;
        angle += orbitSpeed * Time.deltaTime;
        float r = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(r) * radius, height, Mathf.Sin(r) * radius);
        cam.transform.position = die.position + offset;
        cam.transform.LookAt(die.position);
    }
}
