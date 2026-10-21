using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Scene5InventoryHotbar : MonoBehaviour
{
    private const string TargetSceneName = "Scene5-InventoryMerge";
    private const string SharedToolSpriteResourcePath = "InventorySystem/Art/farming-tileset";

    [Header("Optional explicit tool sprites (recommended)")]
    [SerializeField] private Sprite toolOneSprite;
    [SerializeField] private Sprite toolTwoSprite;
    [SerializeField] private Sprite toolThreeSprite;

    private sealed class HotbarSlot
    {
        public Image background;
        public Image icon;
        public TMP_Text label;
        public TMP_Text count;
        public TMP_Text number;
    }

    private readonly List<HotbarSlot> slots = new List<HotbarSlot>();
    private readonly Dictionary<FarmInventoryItemId, InventoryItemData> definitions =
        new Dictionary<FarmInventoryItemId, InventoryItemData>();

    private FarmFund fund;
    private int selectedSlotIndex;
    private Sprite sharedToolSprite;

    public static int SelectedSlotOneBased { get; private set; } = 1;
    public static bool ToolRulesActiveInScene5
    {
        get
        {
            Scene active = SceneManager.GetActiveScene();
            return active.name == TargetSceneName && FindObjectOfType<Scene5InventoryHotbar>() != null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != TargetSceneName) return;

        if (FindObjectOfType<Scene5InventoryHotbar>() != null)
            return;

        GameObject host = new GameObject("Scene5 Inventory Hotbar");
        host.AddComponent<Scene5InventoryHotbar>();
    }

    private void Start()
    {
        fund = FarmFund.Instance;
        if (fund == null)
        {
            Debug.LogWarning("Scene5InventoryHotbar: FarmFund not found.");
            return;
        }

        InventoryItemData[] loaded = Resources.LoadAll<InventoryItemData>("InventoryItems");
        foreach (InventoryItemData item in loaded)
        {
            if (item != null)
                definitions[item.itemId] = item;
        }

        // Try to load the shared tool sprite from Resources/InventorySystem/Art/farming-tileset
        // (any sprite slice from that texture is acceptable; first is used).
        Sprite[] toolSprites = Resources.LoadAll<Sprite>(SharedToolSpriteResourcePath);
        if (toolSprites != null && toolSprites.Length > 0)
            sharedToolSprite = toolSprites[0];

        BuildUI();
        fund.InventoryChanged += RefreshCounts;
        RefreshCounts();
        SelectSlot(0);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
        else if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(5);
        else if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectSlot(6);
        else if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectSlot(7);
        else if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectSlot(8);
        else if (Keyboard.current.digit0Key.wasPressedThisFrame) SelectSlot(9);
    }

    private void BuildUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Inventory Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        GameObject panelObject = new GameObject("Hotbar Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = canvas.gameObject.layer;
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0f);
        panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 12f);
        panel.sizeDelta = new Vector2(1240f, 170f);
        panel.localScale = new Vector3(0.5f, 0.5f, 1f);

        Image panelBg = panelObject.GetComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.04f, 0.85f);

        for (int i = 0; i < 10; i++)
        {
            float x = 20f + i * 120f;
            HotbarSlot slot = CreateSlot(panel, i, x);
            slots.Add(slot);
        }

        // First 3 slots: tools
        Sprite tool1 = toolOneSprite != null
            ? toolOneSprite
            : (sharedToolSprite != null ? sharedToolSprite : GetToolSprite(1));
        Sprite tool2 = toolTwoSprite != null
            ? toolTwoSprite
            : (sharedToolSprite != null ? sharedToolSprite : GetToolSprite(2));
        Sprite tool3 = toolThreeSprite != null
            ? toolThreeSprite
            : (sharedToolSprite != null ? sharedToolSprite : GetToolSprite(3));

        SetSlotVisual(0, "Till", tool1, showCount: false);
        SetSlotVisual(1, "Water", tool2, showCount: false);
        SetSlotVisual(2, "Scythe", tool3, showCount: false);

        // Seed + wheat inventory slots
        SetSlotVisual(3, "Seeds", GetItemSprite(FarmInventoryItemId.FirstPlantSeed), showCount: true);
        SetSlotVisual(4, "Wheat", GetItemSprite(FarmInventoryItemId.FirstPlantHarvest), showCount: true);

        // Remaining slots are reserved/empty
        for (int i = 5; i < 10; i++)
            SetSlotVisual(i, "Empty", null, showCount: false);
    }

    private HotbarSlot CreateSlot(RectTransform parent, int index, float x)
    {
        GameObject slotObject = new GameObject("Slot " + (index + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        slotObject.layer = parent.gameObject.layer;
        slotObject.transform.SetParent(parent, false);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(x, 16f);
        rect.sizeDelta = new Vector2(110f, 138f);

        Image bg = slotObject.GetComponent<Image>();
        bg.color = new Color(0.78f, 0.67f, 0.45f, 0.95f);

        Image icon = CreateImage("Icon", slotObject.transform, new Vector2(55f, 86f), new Vector2(74f, 74f));
        icon.preserveAspect = true;

        TMP_Text label = CreateText("Label", slotObject.transform, new Vector2(55f, 30f), new Vector2(100f, 24f), 16f);
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.1f, 0.08f, 0.04f, 1f);

        TMP_Text count = CreateText("Count", slotObject.transform, new Vector2(84f, 112f), new Vector2(44f, 24f), 16f);
        count.alignment = TextAlignmentOptions.Center;
        count.color = Color.white;

        GameObject badge = new GameObject("CountBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badge.layer = slotObject.layer;
        badge.transform.SetParent(slotObject.transform, false);
        RectTransform badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0f, 0f);
        badgeRt.anchorMax = new Vector2(0f, 0f);
        badgeRt.pivot = new Vector2(0f, 0f);
        badgeRt.anchoredPosition = new Vector2(64f, 100f);
        badgeRt.sizeDelta = new Vector2(42f, 30f);
        Image badgeBg = badge.GetComponent<Image>();
        badgeBg.color = new Color(0.1f, 0.08f, 0.03f, 0.9f);
        count.transform.SetParent(badge.transform, false);
        RectTransform countRt = count.GetComponent<RectTransform>();
        countRt.anchorMin = new Vector2(0.5f, 0.5f);
        countRt.anchorMax = new Vector2(0.5f, 0.5f);
        countRt.pivot = new Vector2(0.5f, 0.5f);
        countRt.anchoredPosition = Vector2.zero;
        countRt.sizeDelta = new Vector2(40f, 26f);

        TMP_Text number = CreateText("Number", slotObject.transform, new Vector2(14f, 116f), new Vector2(20f, 20f), 14f);
        number.alignment = TextAlignmentOptions.Center;
        number.color = new Color(0.15f, 0.09f, 0.02f, 1f);
        number.text = ((index + 1) % 10).ToString();

        return new HotbarSlot
        {
            background = bg,
            icon = icon,
            label = label,
            count = count,
            number = number
        };
    }

    private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return obj.GetComponent<Image>();
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        TMP_Text text = obj.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private void SetSlotVisual(int index, string label, Sprite iconSprite, bool showCount)
    {
        if (index < 0 || index >= slots.Count) return;

        HotbarSlot slot = slots[index];
        slot.label.text = label;
        slot.icon.sprite = iconSprite;
        slot.icon.enabled = iconSprite != null;
        slot.count.transform.parent.gameObject.SetActive(showCount);
    }

    private Sprite GetToolSprite(int toolNumber)
    {
        ToolInteractions tools = FindAnyObjectByType<ToolInteractions>();
        if (tools == null) return null;

        GameObject tool = null;
        if (toolNumber == 1) tool = tools.toolOne;
        else if (toolNumber == 2) tool = tools.toolTwo;
        else if (toolNumber == 3) tool = tools.toolThree;

        if (tool == null) return null;

        Image img = tool.GetComponentInChildren<Image>(true);
        if (img != null && img.sprite != null) return img.sprite;

        SpriteRenderer sr = tool.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) return sr.sprite;

        return null;
    }

    private Sprite GetItemSprite(FarmInventoryItemId itemId)
    {
        if (definitions.TryGetValue(itemId, out InventoryItemData data) && data != null)
            return data.itemSprite;
        return null;
    }

    private void RefreshCounts()
    {
        if (fund == null) return;
        if (slots.Count < 5) return;

        int totalSeeds = Mathf.Max(0, fund.Seeds + fund.SecondPlantSeeds);
        int wheat = Mathf.Max(0, fund.HarvestedPlants);

        // Slot 4 (index 3): show Seeds only when player has any seeds.
        if (totalSeeds > 0)
        {
            slots[3].label.text = "Seeds";
            slots[3].icon.sprite = GetItemSprite(FarmInventoryItemId.FirstPlantSeed);
            slots[3].icon.enabled = slots[3].icon.sprite != null;
            slots[3].count.text = "x" + totalSeeds;
            slots[3].count.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            slots[3].label.text = "Empty";
            slots[3].icon.enabled = false;
            slots[3].count.transform.parent.gameObject.SetActive(false);
        }

        // Slot 5 (index 4): show Wheat only when player has any harvested wheat.
        if (wheat > 0)
        {
            slots[4].label.text = "Wheat";
            slots[4].icon.sprite = GetItemSprite(FarmInventoryItemId.FirstPlantHarvest);
            slots[4].icon.enabled = slots[4].icon.sprite != null;
            slots[4].count.text = "x" + wheat;
            slots[4].count.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            slots[4].label.text = "Empty";
            slots[4].icon.enabled = false;
            slots[4].count.transform.parent.gameObject.SetActive(false);
        }
    }

    private void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return;
        selectedSlotIndex = index;
        SelectedSlotOneBased = selectedSlotIndex + 1;

        for (int i = 0; i < slots.Count; i++)
        {
            bool selected = i == selectedSlotIndex;
            slots[i].background.color = selected
                ? new Color(1f, 0.9f, 0.45f, 1f)
                : new Color(0.78f, 0.67f, 0.45f, 0.95f);
            slots[i].number.color = selected
                ? new Color(0f, 0f, 0f, 1f)
                : new Color(0.15f, 0.09f, 0.02f, 1f);
        }
    }

    private void OnDestroy()
    {
        if (fund != null)
            fund.InventoryChanged -= RefreshCounts;
    }
}
