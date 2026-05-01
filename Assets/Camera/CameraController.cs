using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Default,
        Idle,
        Move,
        Tracking,
        Disabled,
    }

    [SerializeField] CameraMode startingMode = CameraMode.Default;
    [SerializeField] CameraDefault defaultBehaviour;
    [SerializeField] CameraIdle idleBehaviour;
    [SerializeField] CameraMove moveBehaviour;

    [Header("Idle Activation")]
    [Tooltip("Seconds of no input before Default automatically switches to Idle.")]
    [SerializeField] float idleAfterSeconds = 5f;

    public CameraMode CurrentMode { get; private set; } = CameraMode.Disabled;

    float lastInputTime;
    Vector3 lastMousePosition;
    Transform pendingMoveTarget;

    // resolves the per-mode behaviour components.
    void Awake()
    {
        if (defaultBehaviour == null) defaultBehaviour = GetComponent<CameraDefault>();
        if (idleBehaviour == null) idleBehaviour = GetComponent<CameraIdle>();
        if (moveBehaviour == null) moveBehaviour = GetComponent<CameraMove>();
    }

    // initialises input timers and enters the configured starting mode.
    void Start()
    {
        lastMousePosition = Input.mousePosition;
        lastInputTime = Time.time;
        SetMode(startingMode);
    }

    float EffectiveIdleAfterSeconds =>
        ClientSettings.Instance != null ? ClientSettings.Instance.Data.idleAfterSeconds : idleAfterSeconds;

    // tracks input activity and toggles between default and idle when needed.
    void Update()
    {
        if (PauseManager.IsGamePaused) return;

        bool inputThisFrame = DetectInput();
        if (inputThisFrame) lastInputTime = Time.time;

        // drop into idle after the configured inactivity window.
        if (CurrentMode == CameraMode.Default && Time.time - lastInputTime >= EffectiveIdleAfterSeconds)
        {
            SetMode(CameraMode.Idle);
        }
        else if (CurrentMode == CameraMode.Idle && inputThisFrame)
        {
            SetMode(CameraMode.Default);
        }
    }

    // returns true when any keyboard, mouse-button, scroll, or motion input occurs this frame.
    bool DetectInput()
    {
        if (Input.anyKeyDown) return true;
        if (Input.mouseScrollDelta.sqrMagnitude > 0f) return true;

        Vector3 mouse = Input.mousePosition;
        if ((mouse - lastMousePosition).sqrMagnitude > 1f)
        {
            lastMousePosition = mouse;
            return true;
        }
        lastMousePosition = mouse;
        return false;
    }

    // switches to a new camera mode by exiting the old one and entering the new.
    public void SetMode(CameraMode mode)
    {
        if (mode == CurrentMode) return;

        ExitMode(CurrentMode);
        CurrentMode = mode;
        EnterMode(mode);

        // reset the inactivity timer so default doesn't immediately retrigger idle.
        lastInputTime = Time.time;
    }

    // bumps the idle timer back to now, used after dice rolls and movement.
    public void ResetIdleTimer()
    {
        lastInputTime = Time.time;
        lastMousePosition = Input.mousePosition;
    }

    // requests the move-mode camera to focus on the given target.
    public void BeginMove(Transform target)
    {
        if (target == null) return;
        pendingMoveTarget = target;
        if (CurrentMode == CameraMode.Move)
        {
            // already in move, just retarget without re-entering.
            if (moveBehaviour != null) moveBehaviour.BeginMove(target);
        }
        else
        {
            SetMode(CameraMode.Move);
        }
    }

    // dispatches the start-of-mode hook to the matching behaviour component.
    void EnterMode(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Default:
                if (defaultBehaviour != null) defaultBehaviour.BeginDefault();
                break;
            case CameraMode.Idle:
                if (idleBehaviour != null) idleBehaviour.BeginIdle();
                break;
            case CameraMode.Move:
                if (moveBehaviour != null) moveBehaviour.BeginMove(pendingMoveTarget);
                break;
            case CameraMode.Tracking:
                // hook up unit/board tracking here later
                break;
            case CameraMode.Disabled:
                break;
        }
    }

    // dispatches the end-of-mode hook to the matching behaviour component.
    void ExitMode(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Default:
                if (defaultBehaviour != null) defaultBehaviour.EndDefault();
                break;
            case CameraMode.Idle:
                if (idleBehaviour != null) idleBehaviour.EndIdle();
                break;
            case CameraMode.Move:
                if (moveBehaviour != null) moveBehaviour.EndMove();
                break;
        }
    }
}
