using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// In-game pause button plus its menu: resume the current run or go back to the main menu.
public class PauseMenuUI : MonoBehaviour
{
    public Button pauseButton;
    public GameObject panel;
    public Button resumeButton;
    public Button homeButton;

    bool paused;

    void Awake()
    {
        Time.timeScale = 1f;

        if (pauseButton != null) pauseButton.onClick.AddListener(Pause);
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (homeButton != null) homeButton.onClick.AddListener(GoHome);
        if (panel != null) panel.SetActive(false);
    }

    void Pause()
    {
        if (paused) return;
        paused = true;
        if (panel != null) panel.SetActive(true);
        Time.timeScale = 0f;
    }

    void Resume()
    {
        paused = false;
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void GoHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
