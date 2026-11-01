
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "CentralDistrict";

    public void StartGame()
    {
        if (sceneLoader == null)
        {
            Debug.LogError(
                "MainMenuManager: SceneLoader reference is missing."
            );
            return;
        }

        sceneLoader.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        // Settings functionality will be implemented later.
        Debug.Log("Settings menu is not implemented yet.");
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Shadow of Justice...");

#if UNITY_EDITOR
        // Stop Play mode when testing inside Unity.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Close the standalone game.
        Application.Quit();
#endif
    }
}
