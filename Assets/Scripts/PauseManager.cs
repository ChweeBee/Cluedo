using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; }

    [SerializeField] KeyCode toggleKey = KeyCode.P;
    [SerializeField] GameObject pauseCanvas;

    public bool IsPaused => IsGamePaused;

    // syncs visual state with the current pause flag on scene start.
    void Start()
    {
        ApplyPauseState();
    }

    // listens for the configured pause toggle key.
    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    // flips the pause flag on or off.
    public void Toggle()
    {
        IsGamePaused = !IsGamePaused;
        ApplyPauseState();
    }

    // forces the game into a paused state.
    public void Pause()
    {
        if (IsGamePaused) return;
        IsGamePaused = true;
        ApplyPauseState();
    }

    // forces the game out of a paused state.
    public void Resume()
    {
        if (!IsGamePaused) return;
        IsGamePaused = false;
        ApplyPauseState();
    }

    // applies the pause flag to time scale and the pause canvas visibility.
    void ApplyPauseState()
    {
        Time.timeScale = IsGamePaused ? 0f : 1f;
        if (pauseCanvas != null) pauseCanvas.SetActive(IsGamePaused);
    }

    // releases time scale if disabled while paused so the engine doesn't freeze.
    void OnDisable()
    {
        if (IsGamePaused)
        {
            IsGamePaused = false;
            Time.timeScale = 1f;
        }
    }
}
