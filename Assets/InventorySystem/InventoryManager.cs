using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the persistent FarmFund inventory in three familiar inventory
/// slots. Item definitions provide names/prices/sprites; FarmFund owns counts.
/// The panel is created by ShopUpgradeManager once the Bag is owned.
/// </summary>
public sealed class InventoryManager : MonoBehaviour
{
    private sealed class SlotView
    {
        public Image icon;
        public TMP_Text count;
    }

    private readonly Dictionary<FarmInventoryItemId, InventoryItemData> definitions =
        new Dictionary<FarmInventoryItemId, InventoryItemData>();

    private readonly Dictionary<FarmInventoryItemId, SlotView> slots =
        new Dictionary<FarmInventoryItemId, SlotView>();

    private FarmFund fund;
    private TMP_FontAsset font;
    private bool initialized;

    public void Initialize(
        FarmFund persistentFund,
        TMP_FontAsset uiFont,
        bool showTestingCanvas
    )
    {
        if (initialized) return;
        initialized = true;

        fund = persistentFund;
        font = uiFont != null ? uiFont : TMP_Settings.defaultFontAsset;

        InventoryItemData[] loaded =
            Resources.LoadAll<InventoryItemData>("InventoryItems");

        foreach (InventoryItemData definition in loaded)
        {
            if (definition != null)
                definitions[definition.itemId] = definition;
        }

        BuildBagPanel();

        if (fund != null)
            fund.InventoryChanged += RefreshUI;

        RefreshUI();

        if (showTestingCanvas)
        {
            InventoryTestingCanvas.Create(
                transform.parent,
                fund,
                font
            );
        }
    }

    private void BuildBagPanel()
    {
        RectTransform panel = GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(1f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(1f, 0f);
        panel.anchoredPosition = new Vector2(-24f, 24f);
        panel.sizeDelta = new Vector2(470f, 224f);

        Image background = GetComponent<Image>();
        background.color = new Color(0.13f, 0.08f, 0.04f, 0.94f);

        TMP_Text title = CreateText(
            "Bag Title",
            transform,
            "BAG INVENTORY",
            28f,
            FontStyles.Bold,
            TextAlignmentOptions.Center
        );
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -26f), new Vector2(430f, 42f));

        FarmInventoryItemId[] order =
        {
            FarmInventoryItemId.FirstPlantSeed,
            FarmInventoryItemId.FirstPlantHarvest,
            FarmInventoryItemId.SecondPlantSeed
        };

        for (int i = 0; i < order.Length; i++)
        {
            float x = 18f + (i * 148f);
            BuildSlot(order[i], x);
        }
    }

    private void BuildSlot(FarmInventoryItemId itemId, float x)
    {
        InventoryItemData definition = GetDefinition(itemId);
        string displayName = definition != null
            ? definition.itemName
            : itemId.ToString();

        GameObject slotObject = CreateUIObject("Slot - " + displayName, transform);
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0f, 1f);
        slotRect.anchoredPosition = new Vector2(x, -63f);
        slotRect.sizeDelta = new Vector2(138f, 143f);

        Image slotBackground = slotObject.AddComponent<Image>();
        slotBackground.color = new Color(0.92f, 0.78f, 0.52f, 0.95f);

        GameObject iconObject = CreateUIObject("Item Sprite", slotObject.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.sprite = definition != null ? definition.itemSprite : null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        SetRect(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -45f), new Vector2(74f, 74f));

        TMP_Text label = CreateText(
            "Item Name",
            slotObject.transform,
            displayName,
            18f,
            FontStyles.Bold,
            TextAlignmentOptions.Center
        );
        label.color = new Color(0.15f, 0.08f, 0.03f, 1f);
        SetRect(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 28f), new Vector2(128f, 42f));

        GameObject countBadge = CreateUIObject("Count Badge", slotObject.transform);
        Image countBackground = countBadge.AddComponent<Image>();
        countBackground.color = new Color(0.15f, 0.08f, 0.03f, 0.9f);
        countBackground.raycastTarget = false;
        SetRect(countBadge.GetComponent<RectTransform>(), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-23f, 20f), new Vector2(48f, 36f));

        TMP_Text count = CreateText(
            "Item Count",
            countBadge.transform,
            "x0",
            23f,
            FontStyles.Bold,
            TextAlignmentOptions.Center
        );
        count.color = Color.white;
        SetRect(count.rectTransform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 34f));

        slots[itemId] = new SlotView { icon = icon, count = count };
    }

    public void RefreshUI()
    {
        if (fund == null) return;

        foreach (KeyValuePair<FarmInventoryItemId, SlotView> pair in slots)
        {
            int amount = fund.GetItemCount(pair.Key);
            pair.Value.count.text = "x" + amount;

            // Match the example: an empty stack removes its sprite from the
            // slot, while the counter remains visible and accurate.
            pair.Value.icon.enabled = amount > 0 && pair.Value.icon.sprite != null;
        }
    }

    private InventoryItemData GetDefinition(FarmInventoryItemId itemId)
    {
        definitions.TryGetValue(itemId, out InventoryItemData definition);
        return definition;
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment
    )
    {
        GameObject textObject = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnDestroy()
    {
        if (fund != null)
            fund.InventoryChanged -= RefreshUI;
    }
}
