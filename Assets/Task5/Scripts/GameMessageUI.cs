using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum GameMessageKind
{
    Info,
    Success,
    Warning,
    Error
}

public sealed class GameMessageUI : MonoBehaviour
{
    private sealed class Toast
    {
        public RectTransform rect;
        public CanvasGroup group;
        public TMP_Text text;
        public float age;
        public float timeLeft;
    }

    private const float ToastWidth = 1000f;
    private const float ToastHeight = 62f;
    private const float ToastSpacing = 8f;
    private const float FadeSeconds = 0.35f;

    private readonly List<Toast> toasts = new List<Toast>();
    private RectTransform stack;
    private float showSeconds = 3f;
    private int maxToasts = 3;

    public void Build(float secondsOnScreen, int maxOnScreen, float topOffset)
    {
        showSeconds = secondsOnScreen;
        maxToasts = Mathf.Max(1, maxOnScreen);

        GameObject canvasObject = new GameObject("Game Message Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // above every scene canvas

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject stackObject = new GameObject("Message Stack", typeof(RectTransform));
        stackObject.layer = canvasObject.layer;
        stackObject.transform.SetParent(canvasObject.transform, false);
        stack = stackObject.GetComponent<RectTransform>();
        stack.anchorMin = new Vector2(0.5f, 1f);
        stack.anchorMax = new Vector2(0.5f, 1f);
        stack.pivot = new Vector2(0.5f, 1f);
        stack.anchoredPosition = new Vector2(0f, -topOffset);
        stack.sizeDelta = new Vector2(ToastWidth, ToastHeight);
    }

    public void Show(string message, GameMessageKind kind)
    {
        if (stack == null || string.IsNullOrWhiteSpace(message)) return;

        // the same message again just stays up longer instead of stacking
        foreach (Toast existing in toasts)
        {
            if (existing.text.text == message)
            {
                existing.timeLeft = showSeconds;
                return;
            }
        }

        while (toasts.Count >= maxToasts)
        {
            Toast oldest = toasts[toasts.Count - 1];
            toasts.RemoveAt(toasts.Count - 1);
            Destroy(oldest.rect.gameObject);
        }

        toasts.Insert(0, CreateToast(message, kind));
        Layout();
    }

    private Toast CreateToast(string message, GameMessageKind kind)
    {
        GameObject toastObject = new GameObject("Message - " + kind, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        toastObject.layer = stack.gameObject.layer;
        toastObject.transform.SetParent(stack, false);

        RectTransform rect = toastObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(ToastWidth, ToastHeight);

        Image background = toastObject.GetComponent<Image>();
        background.color = ColorFor(kind);
        background.raycastTarget = false;

        CanvasGroup group = toastObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = toastObject.layer;
        textObject.transform.SetParent(toastObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 4f);
        textRect.offsetMax = new Vector2(-18f, -4f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        text.text = message;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 30f;
        text.raycastTarget = false;

        return new Toast { rect = rect, group = group, text = text, age = 0f, timeLeft = showSeconds };
    }

    private void Update()
    {
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            Toast toast = toasts[i];
            toast.timeLeft -= Time.unscaledDeltaTime;
            toast.age += Time.unscaledDeltaTime;

            float fadeIn = Mathf.Clamp01(toast.age / 0.15f);
            float fadeOut = Mathf.Clamp01(toast.timeLeft / FadeSeconds);
            toast.group.alpha = Mathf.Min(fadeIn, fadeOut);

            if (toast.timeLeft <= 0f)
            {
                toasts.RemoveAt(i);
                Destroy(toast.rect.gameObject);
                Layout();
            }
        }
    }

    private void Layout()
    {
        for (int i = 0; i < toasts.Count; i++)
            toasts[i].rect.anchoredPosition = new Vector2(0f, -i * (ToastHeight + ToastSpacing));
    }

    private static Color ColorFor(GameMessageKind kind)
    {
        switch (kind)
        {
            case GameMessageKind.Success: return new Color(0.13f, 0.42f, 0.18f, 0.92f);
            case GameMessageKind.Warning: return new Color(0.66f, 0.40f, 0.05f, 0.94f);
            case GameMessageKind.Error: return new Color(0.62f, 0.13f, 0.13f, 0.94f);
            default: return new Color(0.10f, 0.13f, 0.18f, 0.90f);
        }
    }
}
