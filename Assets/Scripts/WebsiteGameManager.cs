using System.Diagnostics.Contracts;

using DG.Tweening.Core.Easing;

using UnityEngine;

public class WebsiteGameManager : MonoBehaviour
{
    public static WebsiteGameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called from website JavaScript
    public void ResetGame()
    {
        Debug.Log("Game reset requested");
        // Implement your reset logic here
        // Example: Reload the first scene
        Application.Quit();
    }

    public void PauseGame()
    {
        Debug.Log("Game paused");
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        Debug.Log("Game resumed");
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}