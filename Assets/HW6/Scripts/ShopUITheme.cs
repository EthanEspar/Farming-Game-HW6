using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a presentation-only theme to the reusable shop. The original UI
/// objects stay in place, so all Inspector references and button events remain
/// valid. This class only changes layout, colors, fonts, and backgrounds.
/// </summary>
public static class ShopUITheme
{
    private static readonly Color Cream = Hex("FFF3D6");
    private static readonly Color Ink = Hex("3A2416");
    private static readonly Color Wood = Hex("342015", 0.96f);
    private static readonly Color Card = Hex("F2D69B", 0.96f);
    private static readonly Color CardDark = Hex("D8B673", 0.98f);
    private static readonly Color Green = Hex("5A964E");
    private static readonly Color Orange = Hex("C97B38");
    private static readonly Color Blue = Hex("487E9D");
    private static readonly Color Gold = Hex("D3A83E");

    private static Sprite roundedSprite;

    public static void Apply(ShopUpgradeManager shop, SeedsMoneyScene2 seedShop)
    {
        if (shop == null || seedShop == null)
            return;

        Canvas canvas = shop.nextLevelButton != null
            ? shop.nextLevelButton.GetComponentInParent<Canvas>()
            : null;
        if (canvas == null && shop.instructionsText != null)
            canvas = shop.instructionsText.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.transform.Find("Polished Shop Theme") != null)
            return;

        TMP_FontAsset font = shop.instructionsText != null
            ? shop.instructionsText.font
            : TMP_Settings.defaultFontAsset;

        CreateBackdrop(canvas.transform);
        Transform theme = CreateThemeRoot(canvas.transform);

        CreatePanel(theme, "Main Shop Card", Vector2.zero,
            new Vector2(1480f, 900f), Wood, 0);
        CreatePanel(theme, "Seed Card", new Vector2(-360f, 15f),
            new Vector2(580f, 440f), Card, 1);
        CreatePanel(theme, "Harvest Card", new Vector2(360f, 15f),
            new Vector2(580f, 440f), Card, 1);
        CreatePanel(theme, "Seed Counter", new Vector2(250f, 340f),
            new Vector2(230f, 90f), CardDark, 1);
        CreatePanel(theme, "Money Counter", new Vector2(520f, 340f),
            new Vector2(260f, 90f), Gold, 1);

        CreateLabel(theme, font, "Farm Market Title", "FARM MARKET",
            new Vector2(-470f, 350f), new Vector2(500f, 70f), 52f,
            FontStyles.Bold, Cream, TextAlignmentOptions.Left);
        CreateLabel(theme, font, "Seed Card Title", "BUY WHEAT SEEDS",
            new Vector2(-360f, 175f), new Vector2(500f, 55f), 34f,
            FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        CreateLabel(theme, font, "Seed Card Help", "Choose an amount, then purchase.",
            new Vector2(-360f, 125f), new Vector2(500f, 45f), 23f,
            FontStyles.Normal, Ink, TextAlignmentOptions.Center);
        CreateLabel(theme, font, "Harvest Card Title", "HARVEST MARKET",
            new Vector2(360f, 175f), new Vector2(500f, 55f), 34f,
            FontStyles.Bold, Ink, TextAlignmentOptions.Center);
        CreateLabel(theme, font, "Harvest Card Help", "Sell wheat to earn more money.",
            new Vector2(360f, 125f), new Vector2(500f, 45f), 23f,
            FontStyles.Normal, Ink, TextAlignmentOptions.Center);
        CreateLabel(theme, font, "Seed Counter Label", "SEEDS",
            new Vector2(205f, 355f), new Vector2(110f, 32f), 19f,
            FontStyles.Bold, Ink, TextAlignmentOptions.Left);
        CreateLabel(theme, font, "Money Counter Label", "MONEY",
            new Vector2(462f, 355f), new Vector2(120f, 32f), 19f,
            FontStyles.Bold, Ink, TextAlignmentOptions.Left);

        StyleExistingText(shop, seedShop);
        LayoutSeedControls(seedShop);
        LayoutShopControls(shop);
        StyleInput(seedShop.quantityInput);

        StyleButton(seedShop.buyButton, Green);
        StyleButton(seedShop.sellButton, Orange);
        StyleButton(shop.nextLevelButton, Blue);

        if (shop.marketPanel != null)
        {
            Image marketImage = shop.marketPanel.GetComponent<Image>();
            if (marketImage != null)
            {
                marketImage.sprite = GetRoundedSprite();
                marketImage.type = Image.Type.Sliced;
                marketImage.color = Hex("20130D", 0.97f);
            }

            foreach (Button offer in shop.marketPanel.GetComponentsInChildren<Button>(true))
            {
                SetRect(offer.GetComponent<RectTransform>(), new Vector2(175f, 18f),
                    new Vector2(300f, 76f));
                StyleButton(offer, Gold);
            }
        }
    }

    public static Sprite GetRoundedSprite()
    {
        if (roundedSprite != null)
            return roundedSprite;

        const int size = 32;
        const float radius = 8f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Rounded UI";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cornerX = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius));
                float cornerY = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius));
                float distance = Mathf.Sqrt(
                    Mathf.Max(0f, cornerX) * Mathf.Max(0f, cornerX) +
                    Mathf.Max(0f, cornerY) * Mathf.Max(0f, cornerY)
                );
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        roundedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(10f, 10f, 10f, 10f)
        );
        roundedSprite.name = "Runtime Rounded UI Sprite";
        roundedSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedSprite;
    }

    private static void CreateBackdrop(Transform canvas)
    {
        GameObject backdrop = CreateUIObject("Shop Backdrop", canvas);
        backdrop.transform.SetAsFirstSibling();
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = backdrop.AddComponent<Image>();
        image.color = new Color(0.04f, 0.025f, 0.015f, 0.30f);
        image.raycastTarget = false;
    }

    private static Transform CreateThemeRoot(Transform canvas)
    {
        GameObject root = CreateUIObject("Polished Shop Theme", canvas);
        root.transform.SetSiblingIndex(1);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920f, 1080f);
        return root.transform;
    }

    private static void StyleExistingText(ShopUpgradeManager shop, SeedsMoneyScene2 seedShop)
    {
        StyleText(shop.instructionsText, 28f, Cream, FontStyles.Normal,
            TextAlignmentOptions.Left);
        if (shop.instructionsText != null)
            SetRect(shop.instructionsText.rectTransform, new Vector2(-470f, 295f),
                new Vector2(820f, 70f));

        StyleText(seedShop.seedsText, 38f, Ink, FontStyles.Bold,
            TextAlignmentOptions.Right);
        SetRect(seedShop.seedsText.rectTransform, new Vector2(310f, 340f),
            new Vector2(120f, 60f));

        StyleText(seedShop.moneyText, 38f, Ink, FontStyles.Bold,
            TextAlignmentOptions.Right);
        SetRect(seedShop.moneyText.rectTransform, new Vector2(585f, 340f),
            new Vector2(120f, 60f));

        StyleText(seedShop.harvestedText, 30f, Ink, FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetRect(seedShop.harvestedText.rectTransform, new Vector2(360f, 45f),
            new Vector2(480f, 65f));

        StyleText(shop.upgradeInfoText, 24f, Cream, FontStyles.Bold,
            TextAlignmentOptions.Left);
        if (shop.upgradeInfoText != null)
            SetRect(shop.upgradeInfoText.rectTransform, new Vector2(-160f, 35f),
                new Vector2(340f, 70f));

        StyleText(shop.marketMessageText, 22f, Cream, FontStyles.Normal,
            TextAlignmentOptions.Center);
        if (shop.marketMessageText != null)
            SetRect(shop.marketMessageText.rectTransform, new Vector2(0f, -52f),
                new Vector2(650f, 42f));

        HideLegacyLabel(seedShop.moneyText, "Money Text");
        HideLegacyLabel(seedShop.seedsText, "SeedsText");
    }

    private static void LayoutSeedControls(SeedsMoneyScene2 seedShop)
    {
        if (seedShop.quantityInput != null)
            SetRect(seedShop.quantityInput.GetComponent<RectTransform>(),
                new Vector2(-360f, 35f), new Vector2(340f, 76f));
        if (seedShop.buyButton != null)
            SetRect(seedShop.buyButton.GetComponent<RectTransform>(),
                new Vector2(-360f, -80f), new Vector2(340f, 82f));
        if (seedShop.sellButton != null)
            SetRect(seedShop.sellButton.GetComponent<RectTransform>(),
                new Vector2(360f, -80f), new Vector2(340f, 82f));
    }

    private static void LayoutShopControls(ShopUpgradeManager shop)
    {
        if (shop.nextLevelButton != null)
            SetRect(shop.nextLevelButton.GetComponent<RectTransform>(),
                new Vector2(250f, -350f), new Vector2(320f, 92f));

        if (shop.marketPanel != null)
        {
            SetRect(shop.marketPanel.GetComponent<RectTransform>(),
                new Vector2(-390f, -350f), new Vector2(720f, 170f));
        }
    }

    private static void StyleInput(TMP_InputField input)
    {
        if (input == null) return;
        Image image = input.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        if (input.textComponent != null)
        {
            input.textComponent.fontSize = 32f;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.color = Ink;
            input.textComponent.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void StyleButton(Button button, Color color)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.38f, 0.35f, 0.31f, 0.72f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        Shadow shadow = button.GetComponent<Shadow>();
        if (shadow == null) shadow = button.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -5f);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(label, 27f, Cream, FontStyles.Bold, TextAlignmentOptions.Center);
    }

    private static void StyleText(
        TMP_Text text,
        float size,
        Color color,
        FontStyles style,
        TextAlignmentOptions alignment
    )
    {
        if (text == null) return;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
    }

    private static void HideLegacyLabel(TMP_Text valueText, string objectName)
    {
        if (valueText == null) return;
        Transform canvas = valueText.GetComponentInParent<Canvas>().transform;
        Transform legacy = FindDeepChild(canvas, objectName);
        if (legacy != null && legacy != valueText.transform)
            legacy.gameObject.SetActive(false);
    }

    private static Transform FindDeepChild(Transform parent, string objectName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == objectName) return child;
            Transform nested = FindDeepChild(child, objectName);
            if (nested != null) return nested;
        }
        return null;
    }

    private static void CreatePanel(
        Transform parent,
        string name,
        Vector2 position,
        Vector2 size,
        Color color,
        int siblingIndex
    )
    {
        GameObject panel = CreateUIObject(name, parent);
        panel.transform.SetSiblingIndex(siblingIndex);
        SetRect(panel.GetComponent<RectTransform>(), position, size);
        Image image = panel.AddComponent<Image>();
        image.sprite = GetRoundedSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -8f);
    }

    private static void CreateLabel(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string value,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment
    )
    {
        GameObject labelObject = CreateUIObject(name, parent);
        SetRect(labelObject.GetComponent<RectTransform>(), position, size);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.font = font;
        StyleText(label, fontSize, color, style, alignment);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static Color Hex(string value, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        color.a = alpha;
        return color;
    }
}
