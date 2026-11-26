using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu: start the campaign, open level select, or quit the app.
public class MainMenuUI : MonoBehaviour
{
    public Button startButton;
    public Button levelSelectButton;
    public Button quitButton;
    public LevelSelectUI levelSelect;

    void Awake()
    {
        Time.timeScale = 1f;

        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OpenLevelSelect);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void StartGame()
    {
        SceneManager.LoadScene("Stage01");
    }

    void OpenLevelSelect()
    {
        if (levelSelect != null) levelSelect.Open();
    }

    void QuitGame()
    {
        Application.Quit();
    }
}
