using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaHistoryUI : MonoBehaviour
{
    public static GachaHistoryUI Instance;

    [System.Serializable]
    public class HistoryEntry
    {
        public string displayName;
        public GachaRarityType rarity;
        public Color color;
    }

    [Header("Auto Create UI")]
    public bool autoCreateUI = true;
    public Canvas targetCanvas;

    [Header("Generated UI")]
    public RectTransform panelRoot;
    public CanvasGroup canvasGroup;
    public Image panelBackground;
    public TMP_Text titleText;
    public TMP_Text historyText;

    [Header("Layout")]
    public Vector2 panelSize = new Vector2(250f, 230f);
    public Vector2 panelAnchoredPosition = new Vector2(-18f, -230f);

    [Header("Display")]
    public string titleLabel = "GACHA HISTORY";
    public int maxEntries = 7;
    public bool newestOnTop = true;
    public bool hideWhenEmpty = false;

    [Header("Text")]
    public int titleFontSize = 20;
    public int entryFontSize = 18;
    public string emptyText = "NO ROLLS YET";

    [Header("Animation")]
    public bool pulseWhenAdded = true;
    public float pulseScale = 1.06f;
    public float pulseDuration = 0.14f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private readonly List<HistoryEntry> entries = new List<HistoryEntry>();
    private Vector3 basePanelScale = Vector3.one;
    private float pulseTimer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple GachaHistoryUI instances found. Keeping the first one.");
        }

        if (autoCreateUI)
        {
            EnsureUI();
        }

        RefreshUI();
    }

    private void Update()
    {
        if (!pulseWhenAdded)
        {
            return;
        }

        if (panelRoot == null)
        {
            return;
        }

        if (pulseTimer <= 0f)
        {
            panelRoot.localScale = basePanelScale;
            return;
        }

        pulseTimer -= Time.deltaTime;

        float t = Mathf.Clamp01(pulseTimer / Mathf.Max(0.01f, pulseDuration));
        float scale = Mathf.Lerp(1f, pulseScale, t);

        panelRoot.localScale = basePanelScale * scale;
    }

    public void AddEntry(string displayName, GachaRarityType rarity, Color color)
    {
        EnsureUI();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "UNKNOWN";
        }

        HistoryEntry entry = new HistoryEntry();
        entry.displayName = displayName.ToUpperInvariant();
        entry.rarity = rarity;
        entry.color = color;

        if (newestOnTop)
        {
            entries.Insert(0, entry);
        }
        else
        {
            entries.Add(entry);
        }

        while (entries.Count > Mathf.Max(1, maxEntries))
        {
            if (newestOnTop)
            {
                entries.RemoveAt(entries.Count - 1);
            }
            else
            {
                entries.RemoveAt(0);
            }
        }

        if (pulseWhenAdded)
        {
            pulseTimer = pulseDuration;
        }

        RefreshUI();

        if (showDebugLog)
        {
            Debug.Log("GachaHistoryUI added: " + entry.displayName);
        }
    }

    public void ClearHistory()
    {
        entries.Clear();
        RefreshUI();
    }

    private void RefreshUI()
    {
        EnsureUI();

        if (panelRoot == null || historyText == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = titleLabel;
        }

        if (entries.Count <= 0)
        {
            historyText.text = emptyText;

            if (hideWhenEmpty && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            return;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < entries.Count; i++)
        {
            HistoryEntry entry = entries[i];

            if (entry == null)
            {
                continue;
            }

            builder.Append(GetRarityIcon(entry.rarity));
            builder.Append(" ");
            builder.Append(entry.displayName);

            if (i < entries.Count - 1)
            {
                builder.AppendLine();
            }
        }

        historyText.text = builder.ToString();
    }

    private string GetRarityIcon(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return "◇";

            case GachaRarityType.Rare:
                return "◆";

            case GachaRarityType.Epic:
                return "★";
        }

        return "◇";
    }

    private void EnsureUI()
    {
        if (!autoCreateUI)
        {
            return;
        }

        if (panelRoot != null && canvasGroup != null && titleText != null && historyText != null)
        {
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("GachaHistoryCanvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 5100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("GachaHistoryPanel");
        panelObject.transform.SetParent(targetCanvas.transform, false);

        panelRoot = panelObject.AddComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(1f, 1f);
        panelRoot.anchorMax = new Vector2(1f, 1f);
        panelRoot.pivot = new Vector2(1f, 1f);
        panelRoot.anchoredPosition = panelAnchoredPosition;
        panelRoot.sizeDelta = panelSize;

        canvasGroup = panelObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        panelBackground = panelObject.AddComponent<Image>();
        panelBackground.color = new Color(0.03f, 0.04f, 0.06f, 0.74f);
        panelBackground.raycastTarget = false;

        titleText = CreateText(
            panelRoot,
            "HistoryTitleText",
            new Vector2(0f, -12f),
            new Vector2(panelSize.x - 24f, 32f),
            titleFontSize,
            TextAlignmentOptions.Center,
            new Color(0.8f, 0.95f, 1f, 1f)
        );

        historyText = CreateText(
            panelRoot,
            "HistoryEntryText",
            new Vector2(0f, -50f),
            new Vector2(panelSize.x - 24f, panelSize.y - 62f),
            entryFontSize,
            TextAlignmentOptions.TopLeft,
            new Color(1f, 0.94f, 0.78f, 1f)
        );

        basePanelScale = panelRoot.localScale;
    }

    private TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        int fontSize,
        TextAlignmentOptions alignment,
        Color color
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;

        return text;
    }
}