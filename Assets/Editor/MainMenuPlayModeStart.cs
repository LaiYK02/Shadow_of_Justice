using UnityEditor;
using UnityEditor.SceneManagement;

internal static class MainMenuPlayModeStart
{
    [InitializeOnLoadMethod]
    private static void RegisterStartSceneSetup()
    {
        EditorApplication.playModeStateChanged += SetStartScene;
    }

    private static void SetStartScene(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/MainMenu.unity");
        if (mainMenu != null)
            EditorSceneManager.playModeStartScene = mainMenu;
    }
}
