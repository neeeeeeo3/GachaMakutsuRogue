using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlacementMessageUI : MonoBehaviour
{
    [Header("Auto Create UI")]
    public bool autoCreateUI = true;
    public Canvas targetCanvas;

    [Header("Panel")]
    public Vector2 panelSize = new Vector2(420f, 92f);
    public Vector2 anchoredPosition = new Vector2(0f, 120f);

    [Header("Timing")]
    public float showSeconds = 1.25f;

    [Header("Text")]
    public int titleFontSize = 20;
    public int bodyFontSize = 15;

    [Header("Colors")]
    public Color panelColor = new Color(0.08f, 0.035f, 0.035f, 0.9f);
    public Color titleColor = new Color(1f, 0.42f, 0.35f, 1f);
    public Color bodyColor = new Color(0.95f, 0.92f, 0.88f, 1f);

    [Header("Animation")]
    public bool animate = true;
    public float popScale = 1.08f;
    public float popDuration = 0.1f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private RectTransform root;
    private Image background;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    private Coroutine showRoutine;

    private void Start()
    {
        if (autoCreateUI)
        {
            CreateUIIfNeeded();
        }

        HideImmediate();
    }

    public void ShowPlacementError(string title, string body)
    {
        CreateUIIfNeeded();

        if (root == null)
        {
            return;
        }

        titleText.text = string.IsNullOrWhiteSpace(title) ? "CAN'T PLACE" : title;
        bodyText.text = string.IsNullOrWhiteSpace(body) ? "Invalid position." : body;

        background.color = panelColor;
        root.gameObject.SetActive(true);

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowRoutine());

        DebugLog(titleText.text + " / " + bodyText.text);
    }

    public void Hide()
    {
        HideImmediate();
    }

    private IEnumerator ShowRoutine()
    {
        if (root == null)
        {
            yield break;
        }

        if (animate)
        {
            Vector3 startScale = Vector3.one * popScale;
            Vector3 endScale = Vector3.one;

            float timer = 0f;

            while (timer < popDuration)
            {
                timer += Time.deltaTime;

                float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, popDuration));
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                root.localScale = Vector3.Lerp(startScale, endScale, eased);

                yield return null;
            }

            root.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(showSeconds);

        HideImmediate();
        showRoutine = null;
    }

    private void HideImmediate()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (root != null)
        {
            root.localScale = Vector3.one;
            root.gameObject.SetActive(false);
        }
    }

    private void CreateUIIfNeeded()
    {
        if (!autoCreateUI)
        {
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            Debug.LogWarning("PlacementMessageUI: Canvas not found.");
            return;
        }

        if (root != null)
        {
            return;
        }

        GameObject rootObject = new GameObject("PlacementMessagePanel");
        rootObject.transform.SetParent(targetCanvas.transform, false);

        root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0f);
        root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = panelSize;
        root.anchoredPosition = anchoredPosition;

        background = rootObject.AddComponent<Image>();
        background.color = panelColor;

        VerticalLayoutGroup layout = rootObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 10, 10);
        layout.spacing = 2;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = CreateText(
            "Title",
            rootObject.transform,
            titleFontSize,
            titleColor,
            28f
        );

        bodyText = CreateText(
            "Body",
            rootObject.transform,
            bodyFontSize,
            bodyColor,
            42f
        );
    }

    private TMP_Text CreateText(
        string objectName,
        Transform parent,
        int fontSize,
        Color color,
        float preferredHeight
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(0f, preferredHeight);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        text.richText = true;
        text.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleHeight = 0f;

        return text;
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("PlacementMessageUI: " + message);
    }
}