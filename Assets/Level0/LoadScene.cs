using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadScene : MonoBehaviour
{
    private enum Scene3State { NotVisited = 0, VisitedNeedsShop = 1, ShopVisited = 2 }
    private static Scene3State scene3State = Scene3State.NotVisited;
    private static bool scene4Bounce = false; // when true, shop<->level4 should bounce using the same button
    private static bool ensureLevel4ButtonVisible = false;

    public void LadScenebyName(string name)
    {
        // Handle special flow for Scene3 -> Scene4 progression.
        // Some UI buttons in Scene3 may call LadScenebyName("Scene2-Shop") or similar.
        // Treat calls originating from Scene3 as requests to advance past Scene3 when appropriate.
        string active = SceneManager.GetActiveScene().name;


        // If caller explicitly requests Scene3, set state and load Scene3 (entry to crop sim)
        if (name == "Scene3-Crop Sim")
        {
            if (scene3State == Scene3State.ShopVisited)
            {
                // Player has already been to shop after Scene3; jump to Scene4
                SceneManager.LoadScene("Scene4-FarmingLevel3");
            }
            else
            {
                // Mark that Scene3 has been visited and the flow should go to shop first
                scene3State = Scene3State.VisitedNeedsShop;
                SceneManager.LoadScene("Scene3-Crop Sim");
            }
            return;
        }

        // If the call originates from Scene3 and requests the shop, move to shop and mark progress
        if (active == "Scene3-Crop Sim" && name == "Scene2-Shop")
        {
            // First time leaving Scene3 -> go to shop and mark that shop has been visited in this flow
            if (scene3State == Scene3State.VisitedNeedsShop)
            {
                scene3State = Scene3State.ShopVisited;
                SceneManager.LoadScene("Scene2-Shop");
                return;
            }
            // If already ShopVisited, fall through to default behavior
        }

        // If in the shop and the flow indicates we just came from Scene3, clicking the same button
        // (whether it's wired to load the shop or Scene3) should now advance to Scene4 instead of reloading the shop.
        if (active == "Scene2-Shop" && (name == "Scene2-Shop" || name == "Scene3-Crop Sim") && scene3State == Scene3State.ShopVisited)
        {
            // reset state so subsequent runs behave normally
            scene3State = Scene3State.NotVisited;
            SceneManager.LoadScene("Scene4-FarmingLevel3");
            return;
        }

        // default behavior
        // --- Level 4 bounce flow ---
        // If caller explicitly requests Level 4, mark bounce mode and load Level 4
        if (name == "Scene4-FarmingLevel3")
        {
            scene4Bounce = true;
            // Ensure UI fix runs after the scene is loaded
            ensureLevel4ButtonVisible = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("Scene4-FarmingLevel3");
            return;
        }

        // From Level4, a "Go Back" button wired to the shop should send the player to the shop
        if (active == "Scene4-FarmingLevel3" && name == "Scene2-Shop")
        {
            scene4Bounce = true;
            // leaving level4 to shop; keep bounce mode and ensure next time we can return
            SceneManager.LoadScene("Scene2-Shop");
            return;
        }

        // When in the shop and bounce mode is active, clicking the same shop button should return to Level4
        if (active == "Scene2-Shop" && (name == "Scene2-Shop" || name == "Scene4-FarmingLevel3") && scene4Bounce)
        {
            scene4Bounce = false;
            // when returning to level4, ensure the button remains visible (in case it's hidden by UI state)
            ensureLevel4ButtonVisible = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("Scene4-FarmingLevel3");
            return;
        }

        SceneManager.LoadScene(name);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!ensureLevel4ButtonVisible) return;
        ensureLevel4ButtonVisible = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (scene.name != "Scene4-FarmingLevel3") return;

        // Try to find the button named "Next Level" in the loaded scene and make it visible and wired.
        GameObject btnObj = GameObject.Find("Next Level");
        if (btnObj == null)
        {
            Debug.LogWarning("LoadScene: Level4 button 'Next Level' not found in Scene4.");
            return;
        }

        // Activate parents
        Transform t = btnObj.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }

        // Ensure Canvas renders on top
        var foundCanvas = btnObj.GetComponentInParent<Canvas>();
        if (foundCanvas != null)
        {
            foundCanvas.overrideSorting = true;
            foundCanvas.sortingOrder = 1000;
            if (foundCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                foundCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Clear any CanvasGroup that might hide the button
        foreach (var cg in btnObj.GetComponentsInParent<CanvasGroup>())
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Ensure visuals are visible
        foreach (var imgChild in btnObj.GetComponentsInChildren<Image>(true))
        {
            if (imgChild != null)
            {
                var c = imgChild.color; c.a = 1f; imgChild.color = c;
                imgChild.enabled = true;
            }
        }
        foreach (var tmpChild in btnObj.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tmpChild != null)
            {
                tmpChild.color = new Color(0f, 0f, 0f, 1f);
                tmpChild.enabled = true;
            }
        }
        foreach (var txtChild in btnObj.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            if (txtChild != null)
            {
                txtChild.color = new Color(0f, 0f, 0f, 1f);
                txtChild.enabled = true;
            }
        }

        // Ensure button interactable and wire to shop via existing LoadScene if present
        var btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var loader = Object.FindObjectOfType<LoadScene>();
                if (loader != null)
                    loader.LadScenebyName("Scene2-Shop");
                else
                    SceneManager.LoadScene("Scene2-Shop");
            });
        }
        Debug.Log("LoadScene: ensured Level4 'Next Level' button is visible and wired to shop.");

        // Also ensure any Button in the scene that is wired to LoadScene.LadScenebyName is visible
        var allButtons = Object.FindObjectsOfType<UnityEngine.UI.Button>(true);
        foreach (var b in allButtons)
        {
            try
            {
                var ev = b.onClick;
                int cnt = ev.GetPersistentEventCount();
                bool wiredToLoader = false;
                for (int i = 0; i < cnt; i++)
                {
                    var methodName = ev.GetPersistentMethodName(i);
                    var target = ev.GetPersistentTarget(i);
                    if (methodName == "LadScenebyName" || (target != null && target.GetType().Name == "LoadScene"))
                    {
                        wiredToLoader = true;
                        break;
                    }
                }

                if (!wiredToLoader) continue;

                GameObject btnGameObject = b.gameObject;
                // Activate parents
                Transform pt = btnGameObject.transform;
                while (pt != null)
                {
                    if (!pt.gameObject.activeSelf) pt.gameObject.SetActive(true);
                    pt = pt.parent;
                }

                // Ensure Canvas rendering
                var c = btnGameObject.GetComponentInParent<Canvas>();
                if (c != null)
                {
                    c.overrideSorting = true;
                    c.sortingOrder = 1000;
                }

                foreach (var cg in btnGameObject.GetComponentsInParent<CanvasGroup>())
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                foreach (var imgChild in btnGameObject.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (imgChild != null)
                    {
                        var col = imgChild.color; col.a = 1f; imgChild.color = col;
                        imgChild.enabled = true;
                    }
                }

                foreach (var tmpChild in btnGameObject.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmpChild != null)
                    {
                        tmpChild.color = new Color(0f, 0f, 0f, 1f);
                        tmpChild.enabled = true;
                    }
                }

                foreach (var txtChild in btnGameObject.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    if (txtChild != null)
                    {
                        txtChild.color = new Color(0f, 0f, 0f, 1f);
                        txtChild.enabled = true;
                    }
                }

                b.interactable = true;
                Debug.Log("LoadScene: made loader-wired button visible: " + btnGameObject.name);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("LoadScene: error while inspecting button: " + ex.Message);
            }
        }
    }
}
