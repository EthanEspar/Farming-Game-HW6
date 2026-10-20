using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Generic scene-loading adapter for buttons configured in the Unity Inspector.
/// Shop-specific progression belongs to ShopUpgradeManager and ShopSceneConfig.
/// </summary>
public class LoadScene : MonoBehaviour
{
    // Kept for existing UnityEvent bindings in the scenes.
    public void LadScenebyName(string sceneName)
    {
        LoadSceneByName(sceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("LoadScene: no scene name was provided.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "LoadScene: scene '" + sceneName +
                "' is not enabled in Build Profiles."
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
