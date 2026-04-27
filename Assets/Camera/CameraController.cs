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

    void Awake()
    {
        if (defaultBehaviour == null) defaultBehaviour = GetComponent<CameraDefault>();
        if (idleBehaviour == null) idleBehaviour = GetComponent<CameraIdle>();
        if (moveBehaviour == null) moveBehaviour = GetComponent<CameraMove>();
    }

    void Start()
    {
        lastMousePosition = Input.mousePosition;
        lastInputTime = Time.time;
        SetMode(startingMode);
    }

    void Update()
    {
        bool inputThisFrame = DetectInput();
        if (inputThisFrame) lastInputTime = Time.time;

        if (CurrentMode == CameraMode.Default && Time.time - lastInputTime >= idleAfterSeconds)
        {
            SetMode(CameraMode.Idle);
        }
        else if (CurrentMode == CameraMode.Idle && inputThisFrame)
        {
            SetMode(CameraMode.Default);
        }
    }

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

    public void SetMode(CameraMode mode)
    {
        if (mode == CurrentMode) return;

        ExitMode(CurrentMode);
        CurrentMode = mode;
        EnterMode(mode);

        // Reset the inactivity timer on any mode change so we don't immediately re-trigger.
        lastInputTime = Time.time;
    }

    public void BeginMove(Transform target)
    {
        if (target == null) return;
        pendingMoveTarget = target;
        if (CurrentMode == CameraMode.Move)
        {
            // Already in Move; just retarget without re-entering.
            if (moveBehaviour != null) moveBehaviour.BeginMove(target);
        }
        else
        {
            SetMode(CameraMode.Move);
        }
    }

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
