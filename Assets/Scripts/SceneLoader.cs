
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneLoader: Scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"SceneLoader: '{sceneName}' is not available to load. " +
                "Check that the scene is included in the Build Profile."
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
