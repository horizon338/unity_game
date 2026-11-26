using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Level select panel: shows the stage name and its art preview, and switches stages with a
// horizontal swipe or the arrow buttons before launching the chosen scene.
public class LevelSelectUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject panel;
    public Image previewImage;
    public Text levelName;
    public Text pageLabel;
    public Button prevButton;
    public Button nextButton;
    public Button startButton;
    public Button backButton;

    public string[] sceneNames;
    public string[] levelTitles;
    public Sprite[] previewSprites;

    int index;
    Vector2 dragStart;
    bool dragging;

    void Awake()
    {
        if (prevButton != null) prevButton.onClick.AddListener(() => Step(-1));
        if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        if (startButton != null) startButton.onClick.AddListener(StartSelected);
        if (backButton != null) backButton.onClick.AddListener(Close);
        Refresh();
    }

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        Refresh();
    }

    void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    void StartSelected()
    {
        if (sceneNames == null || sceneNames.Length == 0) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneNames[Mathf.Clamp(index, 0, sceneNames.Length - 1)]);
    }

    void Step(int direction)
    {
        if (sceneNames == null || sceneNames.Length == 0) return;
        index = (index + direction + sceneNames.Length) % sceneNames.Length;
        Refresh();
    }

    void Refresh()
    {
        if (sceneNames == null || sceneNames.Length == 0) return;
        index = Mathf.Clamp(index, 0, sceneNames.Length - 1);

        if (levelName != null)
        {
            levelName.text = levelTitles != null && index < levelTitles.Length
                ? levelTitles[index]
                : sceneNames[index];
        }
        if (previewImage != null && previewSprites != null && index < previewSprites.Length && previewSprites[index] != null)
        {
            previewImage.sprite = previewSprites[index];
            previewImage.enabled = true;
        }
        if (pageLabel != null)
        {
            string dots = "";
            for (int i = 0; i < sceneNames.Length; i++) dots += i == index ? "● " : "○ ";
            pageLabel.text = dots.TrimEnd();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStart = eventData.position;
        dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;
        dragging = false;

        float dx = eventData.position.x - dragStart.x;
        if (dx > 80f) Step(-1);       // swipe right: previous stage
        else if (dx < -80f) Step(1);  // swipe left: next stage
    }
}
