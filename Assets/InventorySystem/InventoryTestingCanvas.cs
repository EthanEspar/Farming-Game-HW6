using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Optional testing canvas adapted from the supplied example. Enable the
/// ShopUpgradeManager toggle to test inventory/shop math without farming.
/// Keep the toggle off for normal gameplay or recordings.
/// </summary>
public sealed class InventoryTestingCanvas : MonoBehaviour
{
    private FarmFund fund;
    private TMP_FontAsset font;

    public static InventoryTestingCanvas Create(
        Transform canvasRoot,
        FarmFund persistentFund,
        TMP_FontAsset uiFont
    )
    {
        GameObject panel = new GameObject(
            "Inventory Testing Canvas",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        panel.layer = canvasRoot.gameObject.layer;
        panel.transform.SetParent(canvasRoot, false);

        InventoryTestingCanvas testing =
            panel.AddComponent<InventoryTestingCanvas>();
        testing.Initialize(persistentFund, uiFont);
        return testing;
    }

    private void Initialize(FarmFund persistentFund, TMP_FontAsset uiFont)
    {
        fund = persistentFund;
        font = uiFont != null ? uiFont : TMP_Settings.defaultFontAsset;

        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(430f, 230f);

        GetComponent<Image>().color = new Color(0.09f, 0.16f, 0.23f, 0.95f);

        TMP_Text title = CreateText(
            "Title",
            transform,
            "INVENTORY TEST CANVAS\n(turn toggle off before recording)",
            20f
        );
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -35f), new Vector2(395f, 60f));

        CreateButton("Add $10", new Vector2(105f, -92f), () => fund.AddMoney(10));
        CreateButton("+ Wheat Seed", new Vector2(320f, -92f),
            () => fund.AddItem(FarmInventoryItemId.FirstPlantSeed));
        CreateButton("+ Wheat Crop", new Vector2(105f, -147f),
            () => fund.AddItem(FarmInventoryItemId.FirstPlantHarvest));
        CreateButton("+ Corn Seed", new Vector2(320f, -147f),
            () => fund.AddItem(FarmInventoryItemId.SecondPlantSeed));
        CreateButton("Use Wheat Seed", new Vector2(105f, -202f),
            () => fund.TryRemoveItem(FarmInventoryItemId.FirstPlantSeed));
        CreateButton("Sell Wheat ($5)", new Vector2(320f, -202f),
            () => fund.SellHarvestedPlants(5));
    }

    private void CreateButton(string label, Vector2 position, UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            label,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.layer = gameObject.layer;
        buttonObject.transform.SetParent(transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
            position, new Vector2(195f, 44f));

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.25f, 0.52f, 0.72f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TMP_Text buttonText = CreateText("Label", buttonObject.transform, label, 17f);
        buttonText.alignment = TextAlignmentOptions.Center;
        SetRect(buttonText.rectTransform, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(185f, 40f));
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize
    )
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.layer = gameObject.layer;
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
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
}
