using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static bool IsGamePaused { get; private set; }

    [SerializeField] KeyCode toggleKey = KeyCode.P;
    [SerializeField] GameObject pauseCanvas;

    public bool IsPaused => IsGamePaused;

    void Start()
    {
        ApplyPauseState();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    public void Toggle()
    {
        IsGamePaused = !IsGamePaused;
        ApplyPauseState();
    }

    public void Pause()
    {
        if (IsGamePaused) return;
        IsGamePaused = true;
        ApplyPauseState();
    }

    public void Resume()
    {
        if (!IsGamePaused) return;
        IsGamePaused = false;
        ApplyPauseState();
    }

    void ApplyPauseState()
    {
        Time.timeScale = IsGamePaused ? 0f : 1f;
        if (pauseCanvas != null) pauseCanvas.SetActive(IsGamePaused);
    }

    void OnDisable()
    {
        if (IsGamePaused)
        {
            IsGamePaused = false;
            Time.timeScale = 1f;
        }
    }
}
