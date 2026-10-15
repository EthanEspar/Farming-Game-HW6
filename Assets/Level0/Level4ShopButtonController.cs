using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ensures the "Next Level" / Go Back button in Level 4 is visible and wired to the shop.
public class Level4ShopButtonController : MonoBehaviour
{
    public string buttonObjectName = "Next Level";
    public string shopSceneName = "Scene2-Shop";

    void Start()
    {
        GameObject btnObj = GameObject.Find(buttonObjectName);
        if (btnObj == null)
        {
            Debug.LogWarning("Level4ShopButtonController: button object '" + buttonObjectName + "' not found in scene.");
            return;
        }
        // Ensure the button and all its parents are active
        Transform t = btnObj.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }

        // Bring the button to front in its canvas and enable its RectTransform
        btnObj.transform.SetAsLastSibling();
        var rt = btnObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            // keep existing anchored position but ensure size is reasonable
            if (rt.sizeDelta.x == 0 || rt.sizeDelta.y == 0) rt.sizeDelta = new Vector2(200, 50);
        }

        // Ensure any Canvas above the button renders on top
        Canvas foundCanvas = btnObj.GetComponentInParent<Canvas>();
        if (foundCanvas != null)
        {
            foundCanvas.overrideSorting = true;
            foundCanvas.sortingOrder = 1000;
            // ensure there's a GraphicRaycaster so button is interactable
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
        // Ensure all child RectTransforms are visible and properly scaled/sized
        foreach (var childRt in btnObj.GetComponentsInChildren<RectTransform>(true))
        {
            childRt.localScale = Vector3.one;
            // if child has zero size, give it a reasonable default
            if (childRt.sizeDelta.x == 0 || childRt.sizeDelta.y == 0)
            {
                // don't change the root button's size here (already handled)
                if (childRt != rt) childRt.sizeDelta = new Vector2(160, 30);
            }
        }

        // Ensure child graphics are visible (images, text mesh pro, legacy text)
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
        // Ensure visible image alpha
        var img = btnObj.GetComponent<Image>();
        if (img != null)
        {
            var c = img.color; c.a = 1f; img.color = c;
        }

        // Fix or set the label (TextMeshPro or legacy Text)
        var tmp = btnObj.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.color = new Color(0f, 0f, 0f, 1f);
            if (string.IsNullOrEmpty(tmp.text)) tmp.text = "Go Back";
        }
        else
        {
            var txt = btnObj.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (txt != null)
            {
                txt.color = new Color(0f, 0f, 0f, 1f);
                if (string.IsNullOrEmpty(txt.text)) txt.text = "Go Back";
            }
        }

        // Wire the button click to go to shop via LoadScene if available
        var btn = btnObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                var loader = Object.FindObjectOfType<LoadScene>();
                if (loader != null)
                    loader.LadScenebyName(shopSceneName);
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene(shopSceneName);
            });
        }

        Debug.Log("Level4ShopButtonController: ensured button '" + buttonObjectName + "' is visible and wired.");
    }
}
