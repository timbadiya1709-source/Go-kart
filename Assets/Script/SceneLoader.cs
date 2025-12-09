using UnityEngine;
using UnityEngine.SceneManagement;   // Required for SceneManager

public class SceneLoader : MonoBehaviour
{
    // Call this from your UI Button
    public void LoadRacingScene()
    {
        SceneManager.LoadScene("Racing Scene");   // Use your Racing scene name here
    }

    // Optional: quit game (for Exit button on Home page)
    public void QuitGame()
    {
        Application.Quit();

        // This line helps you test in the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
