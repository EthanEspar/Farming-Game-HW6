using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    private static bool visitedScene3 = false;

    public void LadScenebyName(string name)
    {
        if (name == "Scene3-Crop Sim")
        {
            if (visitedScene3)
            {
                SceneManager.LoadScene("Scene4-FarmingLevel3");
            }
            else
            {
                visitedScene3 = true;
                SceneManager.LoadScene("Scene3-Crop Sim");
            }
        }
        else
        {
            SceneManager.LoadScene(name);
        }
    }
}