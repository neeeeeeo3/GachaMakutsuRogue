using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveWarningUI : MonoBehaviour
{
    public static WaveWarningUI Instance;

    public enum PreviewRestMode
    {
        StayFull,
        CollapseToMini,
        Hide
    }

    [Header("Auto Create UI")]
    public bool autoCreateUI = true;
    public Canvas targetCanvas;

    [Header("Generated Full UI")]
    public RectTransform panelRoot;
    public CanvasGroup canvasGroup;
    public Image panelBackground;
    public TMP_Text titleText;
    public TMP_Text waveText;
    public TMP_Text detailText;
    public TMP_Text countdownText;

    [Header("Generated Mini UI")]
    public RectTransform miniRoot;
    public CanvasGroup miniCanvasGroup;
    public Image miniBackground;
    public TMP_Text miniText;

    [Header("Full Layout")]
    public Vector2 panelSize = new Vector2(560f, 190f);
    public Vector2 panelAnchoredPosition = new Vector2(0f, -28f);

    [Header("Mini Layout")]
    public Vector2 miniPanelSize = new Vector2(420f, 58f);
    public Vector2 miniPanelAnchoredPosition = new Vector2(-18f, -120f);

    [Header("Text")]
    public string titleLabel = "NEXT WAVE";
    public string countdownPrefix = "START IN ";
    public string readyText = "READY";
    public string noEnemyText = "UNKNOWN HEROES";

    [Header("Animation")]
    public bool useAnimation = true;
    public float fadeInDuration = 0.18f;
    public float fadeOutDuration = 0.25f;
    public float panelPopScale = 1.08f;

    [Header("Preview Stability")]
    [Tooltip("ON推奨。同じ予告内容を何度も表示しても、フェードインを再再生しません。点滅防止用。")]
    public bool preventRepeatedPreviewAnimation = true;

    [Header("Preview Auto Rest")]
    [Tooltip("ON推奨。ダンジョン作成フェーズ中の予告を数秒後に畳む/消す設定です。")]
    public bool enablePreviewAutoRest = true;

    [Tooltip("大きい予告を何秒表示してから畳む/消すか。")]
    public float fullPreviewSeconds = 3f;

    [Tooltip("StayFull=居座る / CollapseToMini=小さくなる / Hide=消える")]
    public PreviewRestMode previewRestMode = PreviewRestMode.CollapseToMini;

    [Tooltip("ON推奨。小さく畳んだあと、同じ予告内容なら再び大きく表示しません。")]
    public bool keepRestedPreviewUntilContentChanges = true;

    [Header("Mini Text")]
    public int miniFontSize = 14;
    public float miniLineSpacing = -6f;
    public bool miniUseEllipsis = true;

    [Header("Display")]
    public bool hideWhenCountdownEnds = true;
    public float hideDelayAfterCountdown = 0.25f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Coroutine warningCoroutine;
    private Coroutine showCoroutine;
    private Coroutine hideCoroutine;
    private Coroutine previewRestCoroutine;

    private Vector3 basePanelScale = Vector3.one;
    private Vector3 baseMiniScale = Vector3.one;

    private string lastStaticTitle = "";
    private string lastStaticWaveLine = "";
    private string lastStaticDetail = "";
    private string lastStaticStatusLine = "";

    private bool hasVisibleStaticPreview;
    private bool isStaticPreviewRested;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple WaveWarningUI instances found. Keeping the first one.");
        }

        if (autoCreateUI)
        {
            EnsureUI();
        }

        HideInstant();
    }

    public void ShowPreview(
        int waveNumber,
        int normalCount,
        int fastCount,
        int tankCount,
        int thiefCount,
        string entranceName,
        string statusText
    )
    {
        string detail = BuildWaveDetailText(
            normalCount,
            fastCount,
            tankCount,
            thiefCount,
            entranceName
        );

        ShowStaticText(
            titleLabel,
            "WAVE " + Mathf.Max(1, waveNumber),
            detail,
            string.IsNullOrWhiteSpace(statusText) ? readyText : statusText
        );
    }

    public void ShowWarning(
        int waveNumber,
        int normalCount,
        int fastCount,
        int tankCount,
        int thiefCount,
        float countdownSeconds,
        string entranceName
    )
    {
        string detail = BuildWaveDetailText(
            normalCount,
            fastCount,
            tankCount,
            thiefCount,
            entranceName
        );

        ShowWarningText(
            titleLabel,
            "WAVE " + Mathf.Max(1, waveNumber),
            detail,
            countdownSeconds
        );
    }

    public void ShowSimpleWarning(string message, float countdownSeconds)
    {
        ShowWarningText(
            titleLabel,
            "INCOMING HEROES",
            message,
            countdownSeconds
        );
    }

    public void ShowStaticText(
        string title,
        string waveLine,
        string detail,
        string statusLine
    )
    {
        EnsureUI();

        if (panelRoot == null || canvasGroup == null)
        {
            return;
        }

        string safeTitle = string.IsNullOrWhiteSpace(title) ? titleLabel : title;
        string safeWaveLine = string.IsNullOrWhiteSpace(waveLine) ? "WAVE ?" : waveLine;
        string safeDetail = string.IsNullOrWhiteSpace(detail) ? noEnemyText : detail;
        string safeStatusLine = string.IsNullOrWhiteSpace(statusLine) ? readyText : statusLine;

        bool isSameStaticPreview =
            hasVisibleStaticPreview
            && lastStaticTitle == safeTitle
            && lastStaticWaveLine == safeWaveLine
            && lastStaticDetail == safeDetail
            && lastStaticStatusLine == safeStatusLine;

        if (preventRepeatedPreviewAnimation && isSameStaticPreview)
        {
            if (keepRestedPreviewUntilContentChanges && isStaticPreviewRested)
            {
                return;
            }

            if (panelRoot.gameObject.activeSelf || IsMiniVisible())
            {
                return;
            }
        }

        lastStaticTitle = safeTitle;
        lastStaticWaveLine = safeWaveLine;
        lastStaticDetail = safeDetail;
        lastStaticStatusLine = safeStatusLine;

        hasVisibleStaticPreview = true;
        isStaticPreviewRested = false;

        StopWarningCoroutine();
        StopShowCoroutine();
        StopHideCoroutine();
        StopPreviewRestCoroutine();

        HideMiniInstant();

        panelRoot.gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = safeTitle;
        }

        if (waveText != null)
        {
            waveText.text = safeWaveLine;
        }

        if (detailText != null)
        {
            detailText.text = safeDetail;
        }

        if (countdownText != null)
        {
            countdownText.text = safeStatusLine;
        }

        if (useAnimation)
        {
            showCoroutine = StartCoroutine(ShowFullRoutine());
        }
        else
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            panelRoot.localScale = basePanelScale;
        }

        if (enablePreviewAutoRest && previewRestMode != PreviewRestMode.StayFull)
        {
            previewRestCoroutine = StartCoroutine(PreviewAutoRestRoutine());
        }
    }

    public void ShowWarningText(
        string title,
        string waveLine,
        string detail,
        float countdownSeconds
    )
    {
        EnsureUI();

        hasVisibleStaticPreview = false;
        isStaticPreviewRested = false;

        StopWarningCoroutine();
        StopShowCoroutine();
        StopHideCoroutine();
        StopPreviewRestCoroutine();

        HideMiniInstant();

        warningCoroutine = StartCoroutine(WarningRoutine(
            title,
            waveLine,
            detail,
            countdownSeconds
        ));
    }

    public void Hide()
    {
        EnsureUI();

        hasVisibleStaticPreview = false;
        isStaticPreviewRested = false;

        StopWarningCoroutine();
        StopShowCoroutine();
        StopHideCoroutine();
        StopPreviewRestCoroutine();

        HideMiniInstant();

        hideCoroutine = StartCoroutine(HideFullRoutine(true));
    }

    public void HideInstant()
    {
        hasVisibleStaticPreview = false;
        isStaticPreviewRested = false;

        StopWarningCoroutine();
        StopShowCoroutine();
        StopHideCoroutine();
        StopPreviewRestCoroutine();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.localScale = basePanelScale;
            panelRoot.gameObject.SetActive(false);
        }

        HideMiniInstant();
    }

    private IEnumerator PreviewAutoRestRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, fullPreviewSeconds));

        if (!hasVisibleStaticPreview)
        {
            yield break;
        }

        if (previewRestMode == PreviewRestMode.CollapseToMini)
        {
            ShowMiniPreview();
            yield return HideFullRoutine(false);
            isStaticPreviewRested = true;
        }
        else if (previewRestMode == PreviewRestMode.Hide)
        {
            yield return HideFullRoutine(false);
            isStaticPreviewRested = true;
        }

        previewRestCoroutine = null;
    }

    private void ShowMiniPreview()
    {
        EnsureUI();

        if (miniRoot == null || miniCanvasGroup == null || miniText == null)
        {
            return;
        }

        miniRoot.gameObject.SetActive(true);
        miniRoot.localScale = baseMiniScale;

        miniText.text = BuildMiniText(
            lastStaticWaveLine,
            lastStaticDetail,
            lastStaticStatusLine
        );

        miniText.fontSize = miniFontSize;
        miniText.enableWordWrapping = false;
        miniText.lineSpacing = miniLineSpacing;

        if (miniUseEllipsis)
        {
            miniText.overflowMode = TextOverflowModes.Ellipsis;
        }
        else
        {
            miniText.overflowMode = TextOverflowModes.Overflow;
        }

        miniCanvasGroup.alpha = 1f;
        miniCanvasGroup.interactable = false;
        miniCanvasGroup.blocksRaycasts = false;
    }

    private string BuildMiniText(string waveLine, string detail, string statusLine)
    {
        string shortWave = string.IsNullOrWhiteSpace(waveLine)
            ? "W?"
            : waveLine.Replace("WAVE ", "W");

        string firstDetailLine = detail;

        if (!string.IsNullOrWhiteSpace(firstDetailLine))
        {
            string[] lines = firstDetailLine.Split('\n');

            if (lines.Length > 0)
            {
                firstDetailLine = lines[0];
            }
        }

        if (string.IsNullOrWhiteSpace(firstDetailLine))
        {
            firstDetailLine = noEnemyText;
        }

        string safeStatus = string.IsNullOrWhiteSpace(statusLine)
            ? readyText
            : statusLine;

        return shortWave + "  " + firstDetailLine + "\n" + safeStatus;
    }

    private bool IsMiniVisible()
    {
        return miniRoot != null
            && miniRoot.gameObject.activeSelf
            && miniCanvasGroup != null
            && miniCanvasGroup.alpha > 0.01f;
    }

    private void HideMiniInstant()
    {
        if (miniCanvasGroup != null)
        {
            miniCanvasGroup.alpha = 0f;
            miniCanvasGroup.interactable = false;
            miniCanvasGroup.blocksRaycasts = false;
        }

        if (miniRoot != null)
        {
            miniRoot.localScale = baseMiniScale;
            miniRoot.gameObject.SetActive(false);
        }
    }

    private IEnumerator WarningRoutine(
        string title,
        string waveLine,
        string detail,
        float countdownSeconds
    )
    {
        EnsureUI();

        if (panelRoot == null || canvasGroup == null)
        {
            yield break;
        }

        panelRoot.gameObject.SetActive(true);

        if (titleText != null)
        {
            titleText.text = string.IsNullOrWhiteSpace(title) ? titleLabel : title;
        }

        if (waveText != null)
        {
            waveText.text = string.IsNullOrWhiteSpace(waveLine) ? "WAVE ?" : waveLine;
        }

        if (detailText != null)
        {
            detailText.text = string.IsNullOrWhiteSpace(detail) ? noEnemyText : detail;
        }

        yield return ShowFullRoutine();

        float timer = Mathf.Max(0f, countdownSeconds);

        while (timer > 0f)
        {
            if (countdownText != null)
            {
                countdownText.text = countdownPrefix + Mathf.CeilToInt(timer);
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        if (countdownText != null)
        {
            countdownText.text = countdownPrefix + "0";
        }

        if (hideWhenCountdownEnds)
        {
            yield return new WaitForSeconds(hideDelayAfterCountdown);
            yield return HideFullRoutine(true);
        }

        warningCoroutine = null;
    }

    private IEnumerator ShowFullRoutine()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (!useAnimation)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (panelRoot != null)
            {
                panelRoot.localScale = basePanelScale;
            }

            yield break;
        }

        float timer = 0f;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (panelRoot != null)
        {
            panelRoot.localScale = basePanelScale * panelPopScale;
        }

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, fadeInDuration));
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            canvasGroup.alpha = easedT;

            if (panelRoot != null)
            {
                panelRoot.localScale = Vector3.Lerp(
                    basePanelScale * panelPopScale,
                    basePanelScale,
                    easedT
                );
            }

            yield return null;
        }

        canvasGroup.alpha = 1f;

        if (panelRoot != null)
        {
            panelRoot.localScale = basePanelScale;
        }

        showCoroutine = null;
    }

    private IEnumerator HideFullRoutine(bool resetStaticState)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        if (!useAnimation)
        {
            canvasGroup.alpha = 0f;

            if (panelRoot != null)
            {
                panelRoot.gameObject.SetActive(false);
            }

            if (resetStaticState)
            {
                hasVisibleStaticPreview = false;
                isStaticPreviewRested = false;
            }

            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, fadeOutDuration));
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        canvasGroup.alpha = 0f;

        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(false);
        }

        if (resetStaticState)
        {
            hasVisibleStaticPreview = false;
            isStaticPreviewRested = false;
        }

        hideCoroutine = null;
    }

    private string BuildWaveDetailText(
        int normalCount,
        int fastCount,
        int tankCount,
        int thiefCount,
        string entranceName
    )
    {
        StringBuilder builder = new StringBuilder();

        AppendEnemyLine(builder, "NORMAL", normalCount);
        AppendEnemyLine(builder, "FAST", fastCount);
        AppendEnemyLine(builder, "TANK", tankCount);
        AppendEnemyLine(builder, "THIEF", thiefCount);

        if (builder.Length <= 0)
        {
            builder.Append(noEnemyText);
        }

        if (!string.IsNullOrWhiteSpace(entranceName))
        {
            builder.AppendLine();
            builder.Append("FROM: ");
            builder.Append(entranceName.ToUpperInvariant());
        }

        return builder.ToString();
    }

    private void AppendEnemyLine(StringBuilder builder, string label, int count)
    {
        if (count <= 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(" / ");
        }

        builder.Append(label);
        builder.Append(" x");
        builder.Append(count);
    }

    private void EnsureUI()
    {
        if (!autoCreateUI)
        {
            return;
        }

        EnsureCanvas();
        EnsureFullPanel();
        EnsureMiniPanel();
    }

    private void EnsureCanvas()
    {
        if (targetCanvas != null)
        {
            return;
        }

        targetCanvas = FindFirstObjectByType<Canvas>();

        if (targetCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("WaveWarningCanvas");
        targetCanvas = canvasObject.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureFullPanel()
    {
        if (panelRoot != null
            && canvasGroup != null
            && titleText != null
            && waveText != null
            && detailText != null
            && countdownText != null)
        {
            return;
        }

        GameObject panelObject = new GameObject("WaveWarningPanel");
        panelObject.transform.SetParent(targetCanvas.transform, false);

        panelRoot = panelObject.AddComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.5f, 1f);
        panelRoot.anchorMax = new Vector2(0.5f, 1f);
        panelRoot.pivot = new Vector2(0.5f, 1f);
        panelRoot.anchoredPosition = panelAnchoredPosition;
        panelRoot.sizeDelta = panelSize;

        canvasGroup = panelObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        panelBackground = panelObject.AddComponent<Image>();
        panelBackground.color = new Color(0.03f, 0.04f, 0.06f, 0.82f);
        panelBackground.raycastTarget = false;

        titleText = CreateText(
            panelRoot,
            "TitleText",
            new Vector2(0f, -14f),
            new Vector2(panelSize.x - 32f, 36f),
            24,
            TextAlignmentOptions.Center,
            new Color(0.8f, 0.95f, 1f, 1f)
        );

        waveText = CreateText(
            panelRoot,
            "WaveText",
            new Vector2(0f, -48f),
            new Vector2(panelSize.x - 32f, 40f),
            31,
            TextAlignmentOptions.Center,
            Color.white
        );

        detailText = CreateText(
            panelRoot,
            "DetailText",
            new Vector2(0f, -92f),
            new Vector2(panelSize.x - 32f, 42f),
            21,
            TextAlignmentOptions.Center,
            new Color(1f, 0.92f, 0.72f, 1f)
        );

        countdownText = CreateText(
            panelRoot,
            "CountdownText",
            new Vector2(0f, -142f),
            new Vector2(panelSize.x - 32f, 34f),
            24,
            TextAlignmentOptions.Center,
            new Color(1f, 0.48f, 0.32f, 1f)
        );

        basePanelScale = panelRoot.localScale;

        DebugLog("Wave full warning UI generated.");
    }

    private void EnsureMiniPanel()
    {
        if (miniRoot != null
            && miniCanvasGroup != null
            && miniText != null)
        {
            return;
        }

        GameObject miniObject = new GameObject("WaveWarningMiniPanel");
        miniObject.transform.SetParent(targetCanvas.transform, false);

        miniRoot = miniObject.AddComponent<RectTransform>();
        miniRoot.anchorMin = new Vector2(1f, 1f);
        miniRoot.anchorMax = new Vector2(1f, 1f);
        miniRoot.pivot = new Vector2(1f, 1f);
        miniRoot.anchoredPosition = miniPanelAnchoredPosition;
        miniRoot.sizeDelta = miniPanelSize;

        miniCanvasGroup = miniObject.AddComponent<CanvasGroup>();
        miniCanvasGroup.interactable = false;
        miniCanvasGroup.blocksRaycasts = false;

        miniBackground = miniObject.AddComponent<Image>();
        miniBackground.color = new Color(0.03f, 0.04f, 0.06f, 0.68f);
        miniBackground.raycastTarget = false;

        miniText = CreateText(
            miniRoot,
            "MiniText",
            new Vector2(0f, -5f),
            new Vector2(miniPanelSize.x - 24f, miniPanelSize.y - 8f),
            miniFontSize,
            TextAlignmentOptions.Center,
            new Color(0.9f, 0.98f, 1f, 1f)
        );

        miniText.enableWordWrapping = false;
        miniText.overflowMode = TextOverflowModes.Ellipsis;
        miniText.lineSpacing = miniLineSpacing;

        baseMiniScale = miniRoot.localScale;
        HideMiniInstant();

        DebugLog("Wave mini warning UI generated.");
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

    private void StopWarningCoroutine()
    {
        if (warningCoroutine == null)
        {
            return;
        }

        StopCoroutine(warningCoroutine);
        warningCoroutine = null;
    }

    private void StopShowCoroutine()
    {
        if (showCoroutine == null)
        {
            return;
        }

        StopCoroutine(showCoroutine);
        showCoroutine = null;
    }

    private void StopHideCoroutine()
    {
        if (hideCoroutine == null)
        {
            return;
        }

        StopCoroutine(hideCoroutine);
        hideCoroutine = null;
    }

    private void StopPreviewRestCoroutine()
    {
        if (previewRestCoroutine == null)
        {
            return;
        }

        StopCoroutine(previewRestCoroutine);
        previewRestCoroutine = null;
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("WaveWarningUI: " + message);
    }
}