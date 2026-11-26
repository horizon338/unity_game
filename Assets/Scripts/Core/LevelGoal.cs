using UnityEngine;
using UnityEngine.SceneManagement;

// End-of-stage marker: touching the flag loads the next scene, or shows the stage clear
// message when this is the final stage.
public class LevelGoal : MonoBehaviour
{
    public string nextScene;
    public GameObject clearMessage;

    bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.GetComponent<PlayerController>() == null) return;
        triggered = true;

        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
            return;
        }

        if (clearMessage != null) clearMessage.SetActive(true);
    }
}
