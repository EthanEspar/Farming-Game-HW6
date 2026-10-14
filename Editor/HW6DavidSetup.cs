using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Editor-only helper that builds the HW6 GUI for you (menu bar: "HW6 Setup (David)").
// Every action supports Ctrl/Cmd+Z. Nothing here runs in the game itself.
public static class HW6DavidSetup
{
    const string HW6Folder = "Assets/HW6";
    const string PrefabFolder = "Assets/HW6/Prefabs";
    const string TimePrefabPath = "Assets/HW6/Prefabs/Time Display Canvas.prefab";

    static readonly Color ButtonTextColor = new Color(0.196f, 0.196f, 0.196f, 1f);

    // =====================================================================
    // 1) Time Display Canvas  (Scene1-FarmingSim and Scene3-Crop Sim)
    // =====================================================================
    [MenuItem("HW6 Setup (David)/1 - Add Time Display Canvas to this scene", false, 1)]
    static void AddTimeDisplay()
    {
        DayNightControl dnc = Object.FindAnyObjectByType<DayNightControl>();
        if (dnc == null)
        {
            Popup("This scene has no DayNightController.\n\nOpen Scene1-FarmingSim or Scene3-Crop Sim first.");
            return;
        }
        if (Object.FindAnyObjectByType<TimeDisplayUI>() != null)
        {
            Popup("This scene already has a Time Display Canvas.");
            return;
        }

        EnsureFolders();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TimePrefabPath);
        GameObject instance;
        if (prefab == null)
        {
            // first time: build it, then save it as a prefab so every level can reuse it
            instance = BuildTimeDisplayCanvas();
            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, TimePrefabPath, InteractionMode.UserAction);
        }
        else
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        Undo.RegisterCreatedObjectUndo(instance, "Add Time Display Canvas");

        // replace the old "Days: 1" label
        GameObject oldLabel = GameObject.Find("DayLable");
        Undo.RecordObject(dnc, "Clear old day label");
        dnc.dayLable = null;
        EditorUtility.SetDirty(dnc);
        if (oldLabel != null)
            Undo.DestroyObjectImmediate(oldLabel);

        Selection.activeGameObject = instance;
        MarkSceneDirty();
        Debug.Log("HW6: Time Display Canvas added" + (oldLabel != null ? " and old 'DayLable' removed." : "."));
    }

    static GameObject BuildTimeDisplayCanvas()
    {
        GameObject root = new GameObject("Time Display Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        TimeDisplayUI ui = root.AddComponent<TimeDisplayUI>();

        // ---- top bar ----
        GameObject panel = NewUI("Time Panel", root.transform);
        SetRect(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -15), new Vector2(920, 110));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.07f, 0.82f);
        panelImage.raycastTarget = false;
        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        ui.dayValueText = MakeCell(panel.transform, "Day Cell", "DAY", "Day 1", 0.8f);
        ui.weekdayValueText = MakeCell(panel.transform, "Weekday Cell", "DAY OF THE WEEK", "Monday", 1.3f);
        ui.seasonValueText = MakeCell(panel.transform, "Season Cell", "SEASON", "Spring", 1.3f);
        ui.timeValueText = MakeCell(panel.transform, "Time Cell", "TIME", "6:00 AM", 1f);
        ui.timeCell = ui.timeValueText.transform.parent.gameObject;

        // ---- announcement banner (hidden until a new season / message) ----
        GameObject banner = NewUI("Announcement Banner", root.transform);
        SetRect(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(1300, 70));
        Image bannerImage = banner.AddComponent<Image>();
        bannerImage.color = new Color(0f, 0f, 0f, 0.72f);
        bannerImage.raycastTarget = false;
        TextMeshProUGUI bannerText = NewText("Announcement Text", banner.transform, "Winter is here!", 36, new Color(1f, 0.92f, 0.4f), FontStyles.Bold);
        Stretch(bannerText.gameObject, 10);
        bannerText.enableAutoSizing = true;
        bannerText.fontSizeMin = 18;
        bannerText.fontSizeMax = 36;
        ui.announcementBanner = banner;
        ui.announcementText = bannerText;
        banner.SetActive(false);

        return root;
    }

    static TextMeshProUGUI MakeCell(Transform parent, string name, string caption, string value, float flexibleWidth)
    {
        GameObject cell = NewUI(name, parent);
        Image cellImage = cell.AddComponent<Image>();
        cellImage.color = new Color(1f, 1f, 1f, 0.06f);
        cellImage.raycastTarget = false;
        LayoutElement element = cell.AddComponent<LayoutElement>();
        element.flexibleWidth = flexibleWidth;

        TextMeshProUGUI captionText = NewText("Caption", cell.transform, caption, 16, new Color(0.75f, 0.78f, 0.72f), FontStyles.Bold);
        SetRect(captionText.gameObject, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(0, 24));

        TextMeshProUGUI valueText = NewText("Value", cell.transform, value, 34, Color.white, FontStyles.Bold);
        RectTransform rt = valueText.rectTransform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(4, 4);
        rt.offsetMax = new Vector2(-4, -28);
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 16;
        valueText.fontSizeMax = 34;
        return valueText;
    }

    // =====================================================================
    // 2) Farm Fund  (Scene1-FarmingSim)
    // =====================================================================
    [MenuItem("HW6 Setup (David)/2 - Add Farm Fund object to this scene (Scene 1)", false, 2)]
    static void AddFarmFund()
    {
        if (Object.FindAnyObjectByType<FarmFund>() != null)
        {
            Popup("This scene already has a Farm Fund.");
            return;
        }
        GameObject fundObject = new GameObject("Farm Fund");
        fundObject.AddComponent<FarmFund>();
        Undo.RegisterCreatedObjectUndo(fundObject, "Add Farm Fund");
        Selection.activeGameObject = fundObject;
        MarkSceneDirty();
        Debug.Log("HW6: 'Farm Fund' added at the root of the Hierarchy.");
    }

    // =====================================================================
    // 3) Market panel  (Scene2-Shop)
    // =====================================================================
    [MenuItem("HW6 Setup (David)/3 - Build Market (sell + upgrade) in this scene (Scene 2)", false, 3)]
    static void BuildMarket()
    {
        SeedsMoneyScene2 seedShop = Object.FindAnyObjectByType<SeedsMoneyScene2>();
        if (seedShop == null)
        {
            Popup("This scene has no SeedsMoneyScene2 script.\n\nOpen Scene2-Shop first.");
            return;
        }
        if (Object.FindAnyObjectByType<ShopUpgradeManager>() != null)
        {
            Popup("This scene already has a Shop Upgrade Manager.");
            return;
        }
        Canvas canvas = seedShop.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Popup("Couldn't find the shop's Canvas.");
            return;
        }
        Transform canvasT = canvas.rootCanvas.transform;

        // ---- panel along the bottom of the shop screen ----
        GameObject panel = NewUI("Market Panel", canvasT);
        SetRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(1700, 380));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.13f, 0.09f, 0.05f, 0.9f);
        panelImage.raycastTarget = false; // the background never swallows clicks meant for other buttons

        TextMeshProUGUI title = NewText("Market Title", panel.transform, "MARKET - sell your harvest & buy upgrades", 46, new Color(1f, 0.85f, 0.4f), FontStyles.Bold);
        SetRect(title.gameObject, Center, Center, Center, new Vector2(0, 150), new Vector2(1600, 60));

        // left side: selling
        TextMeshProUGUI harvested = NewText("Harvested Text", panel.transform, "Harvested plants: 0", 42, Color.white, FontStyles.Bold);
        SetRect(harvested.gameObject, Center, Center, Center, new Vector2(-425, 85), new Vector2(800, 60));
        TextMeshProUGUI price = NewText("Sell Price Text", panel.transform, "Sell price: $5 each", 32, new Color(0.85f, 0.85f, 0.85f), FontStyles.Normal);
        SetRect(price.gameObject, Center, Center, Center, new Vector2(-425, 30), new Vector2(800, 50));
        TextMeshProUGUI unused;
        Button sellOne = NewButton("Sell One Button", panel.transform, "Sell 1", new Vector2(-620, -60), new Vector2(360, 110), 40, out unused);
        Button sellAll = NewButton("Sell All Button", panel.transform, "Sell All", new Vector2(-230, -60), new Vector2(360, 110), 40, out unused);

        // right side: upgrade
        TextMeshProUGUI upgradeInfo = NewText("Upgrade Info Text", panel.transform, "Speed Boots - walk 50% faster in Level 3", 34, Color.white, FontStyles.Normal);
        SetRect(upgradeInfo.gameObject, Center, Center, Center, new Vector2(425, 75), new Vector2(800, 100));
        TextMeshProUGUI bootsLabel;
        Button bootsButton = NewButton("Buy Speed Boots Button", panel.transform, "Buy Speed Boots ($15)", new Vector2(425, -60), new Vector2(640, 110), 40, out bootsLabel);

        // bottom: feedback message
        TextMeshProUGUI message = NewText("Market Message", panel.transform, "", 34, new Color(1f, 0.92f, 0.4f), FontStyles.Normal);
        SetRect(message.gameObject, Center, Center, Center, new Vector2(0, -152), new Vector2(1600, 50));

        Undo.RegisterCreatedObjectUndo(panel, "Build Market Panel");

        // ---- manager object ----
        GameObject managerObject = new GameObject("Shop Upgrade Manager");
        ShopUpgradeManager manager = managerObject.AddComponent<ShopUpgradeManager>();
        manager.marketPanel = panel;
        manager.harvestedText = harvested;
        manager.sellPriceText = price;
        manager.sellOneButton = sellOne;
        manager.sellAllButton = sellAll;
        manager.upgradeInfoText = upgradeInfo;
        manager.buySpeedBootsButton = bootsButton;
        manager.buySpeedBootsLabel = bootsLabel;
        manager.marketMessageText = message;

        GameObject instructions = GameObject.Find("Instructions");
        if (instructions != null) manager.instructionsText = instructions.GetComponent<TMP_Text>();

        Button next = null;
        if (seedShop.firstPurchaseButton != null) next = seedShop.firstPurchaseButton.GetComponent<Button>();
        if (next == null)
        {
            GameObject nextObject = GameObject.Find("NextLvl");
            if (nextObject != null) next = nextObject.GetComponent<Button>();
        }
        manager.nextLevelButton = next;
        if (next != null) manager.nextLevelButtonLabel = next.GetComponentInChildren<TMP_Text>(true);

        Undo.RegisterCreatedObjectUndo(managerObject, "Add Shop Upgrade Manager");
        EditorUtility.SetDirty(manager);
        Selection.activeGameObject = managerObject;
        MarkSceneDirty();
        Debug.Log("HW6: Market Panel + Shop Upgrade Manager built. NextLvl found: " + (next != null) + ", Instructions found: " + (instructions != null));
    }

    // =====================================================================
    // 4) Speed Boots effect on the Player  (Scene4, or Scene3 for testing)
    // =====================================================================
    [MenuItem("HW6 Setup (David)/4 - Add Speed Boots effect to the Player in this scene", false, 4)]
    static void AddSpeedBoots()
    {
        PlayerMove player = Object.FindAnyObjectByType<PlayerMove>();
        if (player == null)
        {
            Popup("No Player (PlayerMove script) in this scene.");
            return;
        }
        if (player.GetComponent<SpeedBootsUpgrade>() != null)
        {
            Popup("The Player already has SpeedBootsUpgrade.");
            return;
        }
        Undo.AddComponent<SpeedBootsUpgrade>(player.gameObject);
        Selection.activeGameObject = player.gameObject;
        MarkSceneDirty();
        Debug.Log("HW6: SpeedBootsUpgrade added to " + player.name);
    }

    // =====================================================================
    // 5) Turn the scene "Plant" template into a real prefab  (Scene3-Crop Sim)
    // =====================================================================
    [MenuItem("HW6 Setup (David)/5 - Turn the scene Plant into a prefab (Scene 3)", false, 20)]
    static void PlantToPrefab()
    {
        plantManager manager = Object.FindAnyObjectByType<plantManager>();
        if (manager == null)
        {
            Popup("No PlantManager in this scene. Open Scene3-Crop Sim first.");
            return;
        }
        GameObject template = manager.plantPrefab;
        if (template == null)
        {
            Popup("PlantManager's Plant Prefab field is empty.");
            return;
        }
        if (EditorUtility.IsPersistent(template))
        {
            Popup("PlantManager already uses a prefab from the Project window - nothing to do.");
            return;
        }

        EnsureFolders();
        string path = AssetDatabase.GenerateUniqueAssetPath(PrefabFolder + "/Plant.prefab");
        bool success;
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(template, path, out success);
        if (!success || asset == null)
        {
            Popup("Saving the prefab failed - see the Console.");
            return;
        }

        Undo.RecordObject(manager, "Point PlantManager at Plant prefab");
        manager.plantPrefab = asset;
        EditorUtility.SetDirty(manager);
        Undo.DestroyObjectImmediate(template);
        MarkSceneDirty();
        Debug.Log("HW6: Plant saved to " + path + ", PlantManager now uses it, scene copy removed.");
    }

    // =====================================================================
    // helpers
    // =====================================================================
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(HW6Folder)) AssetDatabase.CreateFolder("Assets", "HW6");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(HW6Folder, "Prefabs");
    }

    static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(GameObject go, float padding)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, 0);
        rt.offsetMax = new Vector2(-padding, 0);
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        GameObject go = NewUI(name, parent);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false; // never block button clicks
        return t;
    }

    static Button NewButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, float fontSize, out TextMeshProUGUI labelText)
    {
        GameObject go = NewUI(name, parent);
        SetRect(go, Center, Center, Center, pos, size);
        Image image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        labelText = NewText("Text (TMP)", go.transform, label, fontSize, ButtonTextColor, FontStyles.Bold);
        Stretch(labelText.gameObject, 8);
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 18;
        labelText.fontSizeMax = fontSize;
        return button;
    }

    static void MarkSceneDirty()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    static void Popup(string message)
    {
        EditorUtility.DisplayDialog("HW6 Setup", message, "OK");
    }
}
