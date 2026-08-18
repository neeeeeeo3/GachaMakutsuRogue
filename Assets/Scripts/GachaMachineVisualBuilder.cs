using TMPro;
using UnityEngine;

public class GachaMachineVisualBuilder : MonoBehaviour
{
    [Header("Visual Root")]
    public string visualRootName = "RealGachaMachineVisual";
    public bool rebuildOnStart = true;

    [Tooltip("古い筐体のSpriteRenderer / MeshRenderer / TextMeshProなどをまとめて隠します。")]
    public bool hideExistingSpriteRenderersOnBuild = true;

    [Header("Layer / Sorting")]
    public bool forceLayer = true;
    public string targetLayerName = "GachaMachine";
    public string sortingLayerName = "GachaMachine";
    public int baseSortingOrder = 1000;

    [Header("Size / Position")]
    public float visualScale = 1f;
    public Vector3 visualLocalOffset = Vector3.zero;

    [Header("Machine Colors")]
    public Color bodyColor = new Color(0.92f, 0.92f, 0.86f, 1f);
    public Color bodyShadowColor = new Color(0.54f, 0.57f, 0.62f, 1f);
    public Color bodyDarkColor = new Color(0.25f, 0.27f, 0.32f, 1f);
    public Color accentColor = new Color(0.90f, 0.12f, 0.16f, 1f);
    public Color accentSecondColor = new Color(0.18f, 0.45f, 0.95f, 1f);

    [Header("Glass")]
    public Color glassColor = new Color(0.72f, 0.92f, 1.00f, 0.35f);
    public Color glassRimColor = new Color(0.95f, 0.98f, 1.00f, 0.78f);
    public Color glassShadowColor = new Color(0.10f, 0.18f, 0.28f, 0.22f);
    public Color glassHighlightColor = new Color(1f, 1f, 1f, 0.72f);

    [Header("Handle")]
    public Color metalColor = new Color(0.76f, 0.78f, 0.82f, 1f);
    public Color metalShadowColor = new Color(0.36f, 0.38f, 0.42f, 1f);
    public Color handleKnobColor = new Color(0.95f, 0.18f, 0.12f, 1f);

    [Header("Capsule Pile")]
    public bool createCapsulePile = true;
    public Color[] capsulePileColors =
    {
        new Color(0.95f, 0.20f, 0.20f, 1f),
        new Color(0.22f, 0.50f, 0.96f, 1f),
        new Color(0.98f, 0.78f, 0.20f, 1f),
        new Color(0.25f, 0.78f, 0.35f, 1f),
        new Color(0.68f, 0.38f, 1.00f, 1f),
        new Color(1.00f, 0.45f, 0.18f, 1f)
    };

    [Header("Labels")]
    public bool createTextLabels = true;
    public Color textColor = new Color(0.10f, 0.10f, 0.12f, 1f);
    public Color labelPanelColor = new Color(1f, 0.96f, 0.74f, 1f);

    [Header("Idle Animation")]
    public bool animateIdle = true;
    public float idlePulseSpeed = 2.2f;
    public float lampPulseAmount = 0.35f;
    public float glassShineMoveAmount = 0.035f;
    public float bodyBreathAmount = 0.008f;

    [Header("Generated References")]
    public Transform generatedHandlePivot;

    private Sprite squareSprite;
    private Sprite circleSprite;
    private Sprite roundedRectSprite;

    private Transform visualRoot;
    private Transform glassBigHighlight;
    private Vector3 glassBigHighlightStartPosition;

    private SpriteRenderer lampCoreRenderer;
    private SpriteRenderer lampGlowRenderer;
    private Color lampCoreBaseColor;
    private Color lampGlowBaseColor;

    private Vector3 visualRootBaseScale;

    private int cachedSortingLayerId;
    private int cachedTargetLayer;

    private void Start()
    {
        if (rebuildOnStart)
        {
            RebuildVisual();
        }
    }

    private void Update()
    {
        if (!animateIdle)
        {
            return;
        }

        if (visualRoot == null)
        {
            return;
        }

        float wave = (Mathf.Sin(Time.time * idlePulseSpeed) + 1f) * 0.5f;

        if (lampCoreRenderer != null)
        {
            Color color = lampCoreBaseColor;
            color.a = Mathf.Lerp(0.65f, 1f, wave);
            lampCoreRenderer.color = color;
        }

        if (lampGlowRenderer != null)
        {
            Color color = lampGlowBaseColor;
            color.a = Mathf.Lerp(0.18f, 0.18f + lampPulseAmount, wave);
            lampGlowRenderer.color = color;
        }

        if (glassBigHighlight != null)
        {
            Vector3 position = glassBigHighlightStartPosition;
            position.x += Mathf.Sin(Time.time * idlePulseSpeed * 0.55f) * glassShineMoveAmount;
            glassBigHighlight.localPosition = position;
        }

        float breath = 1f + Mathf.Sin(Time.time * idlePulseSpeed * 0.35f) * bodyBreathAmount;
        visualRoot.localScale = visualRootBaseScale * breath;
    }

    [ContextMenu("Rebuild Gacha Machine Visual")]
    public void RebuildVisual()
    {
        PrepareSprites();
        CacheLayerAndSorting();

        if (hideExistingSpriteRenderersOnBuild)
        {
            HideExistingRenderers();
        }

        ClearOldVisual();

        GameObject rootObject = new GameObject(visualRootName);
        rootObject.transform.SetParent(transform);
        rootObject.transform.localPosition = visualLocalOffset;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        visualRoot = rootObject.transform;
        visualRootBaseScale = visualRoot.localScale;
        SetLayer(rootObject);

        BuildBackShadow(visualRoot);
        BuildMainBody(visualRoot);
        BuildGlassDome(visualRoot);

        if (createCapsulePile)
        {
            BuildCapsulePile(visualRoot);
        }

        BuildGlassOverlay(visualRoot);
        BuildPanelArea(visualRoot);
        BuildHandle(visualRoot);
        BuildCoinSlot(visualRoot);
        BuildPrizeChute(visualRoot);
        BuildLamp(visualRoot);
        BuildBottomParts(visualRoot);

        if (createTextLabels)
        {
            BuildLabels(visualRoot);
        }
    }

    private void BuildBackShadow(Transform root)
    {
        CreatePart(root, "BackShadow", roundedRectSprite,
            new Vector3(0.045f, -0.06f, 0f),
            new Vector3(1.18f, 1.66f, 1f),
            0f,
            new Color(0f, 0f, 0f, 0.28f),
            baseSortingOrder - 20
        );

        CreatePart(root, "BottomGroundShadow", circleSprite,
            new Vector3(0.02f, -0.93f, 0f),
            new Vector3(1.28f, 0.20f, 1f),
            0f,
            new Color(0f, 0f, 0f, 0.25f),
            baseSortingOrder - 21
        );
    }

    private void BuildMainBody(Transform root)
    {
        CreatePart(root, "BodyMain", roundedRectSprite,
            new Vector3(0f, -0.15f, 0f),
            new Vector3(1.05f, 1.48f, 1f),
            0f,
            bodyColor,
            baseSortingOrder
        );

        CreatePart(root, "BodyRightShadow", roundedRectSprite,
            new Vector3(0.075f, -0.18f, 0f),
            new Vector3(0.93f, 1.35f, 1f),
            0f,
            new Color(0f, 0f, 0f, 0.10f),
            baseSortingOrder + 1
        );

        CreatePart(root, "BodyFrontPanel", roundedRectSprite,
            new Vector3(0f, -0.32f, 0f),
            new Vector3(0.86f, 0.72f, 1f),
            0f,
            new Color(0.98f, 0.98f, 0.92f, 1f),
            baseSortingOrder + 2
        );

        CreatePart(root, "BodyPanelShade", roundedRectSprite,
            new Vector3(0f, -0.38f, 0f),
            new Vector3(0.76f, 0.54f, 1f),
            0f,
            new Color(0f, 0f, 0f, 0.08f),
            baseSortingOrder + 3
        );

        CreatePart(root, "RedSideStripeLeft", roundedRectSprite,
            new Vector3(-0.46f, -0.17f, 0f),
            new Vector3(0.09f, 1.20f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "BlueSideStripeRight", roundedRectSprite,
            new Vector3(0.46f, -0.17f, 0f),
            new Vector3(0.09f, 1.20f, 1f),
            0f,
            accentSecondColor,
            baseSortingOrder + 4
        );
    }

    private void BuildGlassDome(Transform root)
    {
        CreatePart(root, "GlassBackRim", circleSprite,
            new Vector3(0f, 0.43f, 0f),
            new Vector3(0.94f, 0.84f, 1f),
            0f,
            glassRimColor,
            baseSortingOrder + 8
        );

        CreatePart(root, "GlassInnerDark", circleSprite,
            new Vector3(0f, 0.40f, 0f),
            new Vector3(0.84f, 0.72f, 1f),
            0f,
            new Color(0.05f, 0.10f, 0.14f, 0.18f),
            baseSortingOrder + 9
        );

        CreatePart(root, "GlassBaseLip", roundedRectSprite,
            new Vector3(0f, 0.06f, 0f),
            new Vector3(0.88f, 0.16f, 1f),
            0f,
            glassRimColor,
            baseSortingOrder + 18
        );
    }

    private void BuildCapsulePile(Transform root)
    {
        CreateTinyCapsule(root, new Vector3(-0.25f, 0.24f, 0f), 0.18f, GetCapsuleColor(0), baseSortingOrder + 11);
        CreateTinyCapsule(root, new Vector3(-0.05f, 0.22f, 0f), 0.19f, GetCapsuleColor(1), baseSortingOrder + 12);
        CreateTinyCapsule(root, new Vector3(0.18f, 0.25f, 0f), 0.18f, GetCapsuleColor(2), baseSortingOrder + 11);
        CreateTinyCapsule(root, new Vector3(-0.17f, 0.43f, 0f), 0.16f, GetCapsuleColor(3), baseSortingOrder + 10);
        CreateTinyCapsule(root, new Vector3(0.06f, 0.43f, 0f), 0.17f, GetCapsuleColor(4), baseSortingOrder + 10);
        CreateTinyCapsule(root, new Vector3(0.25f, 0.42f, 0f), 0.15f, GetCapsuleColor(5), baseSortingOrder + 10);
        CreateTinyCapsule(root, new Vector3(-0.31f, 0.58f, 0f), 0.13f, GetCapsuleColor(2), baseSortingOrder + 9);
        CreateTinyCapsule(root, new Vector3(0.03f, 0.60f, 0f), 0.14f, GetCapsuleColor(0), baseSortingOrder + 9);
    }

    private void BuildGlassOverlay(Transform root)
    {
        CreatePart(root, "GlassFront", circleSprite,
            new Vector3(0f, 0.43f, 0f),
            new Vector3(0.88f, 0.78f, 1f),
            0f,
            glassColor,
            baseSortingOrder + 20
        );

        CreatePart(root, "GlassLowerShadow", circleSprite,
            new Vector3(0.05f, 0.25f, 0f),
            new Vector3(0.76f, 0.42f, 1f),
            0f,
            glassShadowColor,
            baseSortingOrder + 21
        );

        GameObject bigHighlight = CreatePart(root, "GlassHighlightBig", circleSprite,
            new Vector3(-0.23f, 0.62f, 0f),
            new Vector3(0.08f, 0.35f, 1f),
            -32f,
            glassHighlightColor,
            baseSortingOrder + 24
        );

        glassBigHighlight = bigHighlight.transform;
        glassBigHighlightStartPosition = glassBigHighlight.localPosition;

        CreatePart(root, "GlassHighlightSmall", circleSprite,
            new Vector3(0.08f, 0.70f, 0f),
            new Vector3(0.055f, 0.18f, 1f),
            -28f,
            new Color(1f, 1f, 1f, 0.56f),
            baseSortingOrder + 25
        );

        CreatePart(root, "GlassTinySpecular", circleSprite,
            new Vector3(-0.34f, 0.42f, 0f),
            new Vector3(0.06f, 0.06f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.42f),
            baseSortingOrder + 25
        );
    }

    private void BuildPanelArea(Transform root)
    {
        CreatePart(root, "StickerPanel", roundedRectSprite,
            new Vector3(0f, -0.09f, 0f),
            new Vector3(0.58f, 0.22f, 1f),
            0f,
            labelPanelColor,
            baseSortingOrder + 14
        );

        CreatePart(root, "StickerPanelShadow", roundedRectSprite,
            new Vector3(0.02f, -0.105f, 0f),
            new Vector3(0.52f, 0.16f, 1f),
            0f,
            new Color(0f, 0f, 0f, 0.10f),
            baseSortingOrder + 15
        );

        CreatePart(root, "SmallDecorRed", circleSprite,
            new Vector3(-0.32f, -0.09f, 0f),
            new Vector3(0.08f, 0.08f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 16
        );

        CreatePart(root, "SmallDecorBlue", circleSprite,
            new Vector3(0.32f, -0.09f, 0f),
            new Vector3(0.08f, 0.08f, 1f),
            0f,
            accentSecondColor,
            baseSortingOrder + 16
        );
    }

    private void BuildHandle(Transform root)
    {
        GameObject handlePivotObject = new GameObject("HandlePivot");
        handlePivotObject.transform.SetParent(root);
        handlePivotObject.transform.localPosition = new Vector3(0.28f, -0.34f, 0f);
        handlePivotObject.transform.localRotation = Quaternion.identity;
        handlePivotObject.transform.localScale = Vector3.one;
        SetLayer(handlePivotObject);

        generatedHandlePivot = handlePivotObject.transform;

        CreatePart(root, "HandleOuterBase", circleSprite,
            new Vector3(0.28f, -0.34f, 0f),
            new Vector3(0.30f, 0.30f, 1f),
            0f,
            metalShadowColor,
            baseSortingOrder + 20
        );

        CreatePart(root, "HandleInnerBase", circleSprite,
            new Vector3(0.28f, -0.34f, 0f),
            new Vector3(0.22f, 0.22f, 1f),
            0f,
            metalColor,
            baseSortingOrder + 21
        );

        CreatePart(handlePivotObject.transform, "HandleArm", roundedRectSprite,
            new Vector3(0.16f, 0f, 0f),
            new Vector3(0.34f, 0.075f, 1f),
            0f,
            metalColor,
            baseSortingOrder + 24
        );

        CreatePart(handlePivotObject.transform, "HandleArmShade", roundedRectSprite,
            new Vector3(0.17f, -0.018f, 0f),
            new Vector3(0.30f, 0.025f, 1f),
            0f,
            metalShadowColor,
            baseSortingOrder + 25
        );

        CreatePart(handlePivotObject.transform, "HandleKnob", circleSprite,
            new Vector3(0.36f, 0f, 0f),
            new Vector3(0.18f, 0.18f, 1f),
            0f,
            handleKnobColor,
            baseSortingOrder + 26
        );

        CreatePart(handlePivotObject.transform, "HandleKnobHighlight", circleSprite,
            new Vector3(0.32f, 0.04f, 0f),
            new Vector3(0.055f, 0.055f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.55f),
            baseSortingOrder + 27
        );
    }

    private void BuildCoinSlot(Transform root)
    {
        CreatePart(root, "CoinPlate", roundedRectSprite,
            new Vector3(-0.27f, -0.33f, 0f),
            new Vector3(0.25f, 0.20f, 1f),
            0f,
            metalColor,
            baseSortingOrder + 18
        );

        CreatePart(root, "CoinSlotBlack", roundedRectSprite,
            new Vector3(-0.27f, -0.32f, 0f),
            new Vector3(0.18f, 0.035f, 1f),
            0f,
            new Color(0.03f, 0.03f, 0.04f, 0.95f),
            baseSortingOrder + 19
        );

        CreatePart(root, "CoinSlotHighlight", roundedRectSprite,
            new Vector3(-0.27f, -0.245f, 0f),
            new Vector3(0.18f, 0.025f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.34f),
            baseSortingOrder + 20
        );
    }

    private void BuildPrizeChute(Transform root)
    {
        CreatePart(root, "PrizeChuteOuter", roundedRectSprite,
            new Vector3(0f, -0.66f, 0f),
            new Vector3(0.58f, 0.25f, 1f),
            0f,
            bodyDarkColor,
            baseSortingOrder + 18
        );

        CreatePart(root, "PrizeChuteInner", roundedRectSprite,
            new Vector3(0f, -0.66f, 0f),
            new Vector3(0.45f, 0.14f, 1f),
            0f,
            new Color(0.025f, 0.025f, 0.03f, 1f),
            baseSortingOrder + 19
        );

        CreatePart(root, "PrizeChuteLip", roundedRectSprite,
            new Vector3(0f, -0.56f, 0f),
            new Vector3(0.50f, 0.045f, 1f),
            0f,
            metalColor,
            baseSortingOrder + 20
        );

        CreatePart(root, "PrizeChuteGlow", circleSprite,
            new Vector3(0f, -0.64f, 0f),
            new Vector3(0.34f, 0.07f, 1f),
            0f,
            new Color(1f, 0.90f, 0.35f, 0.18f),
            baseSortingOrder + 21
        );
    }

    private void BuildLamp(Transform root)
    {
        GameObject glow = CreatePart(root, "ReadyLampGlow", circleSprite,
            new Vector3(-0.40f, -0.28f, 0f),
            new Vector3(0.18f, 0.18f, 1f),
            0f,
            new Color(1f, 0.86f, 0.20f, 0.30f),
            baseSortingOrder + 22
        );

        GameObject core = CreatePart(root, "ReadyLampCore", circleSprite,
            new Vector3(-0.40f, -0.28f, 0f),
            new Vector3(0.085f, 0.085f, 1f),
            0f,
            new Color(1f, 0.80f, 0.08f, 1f),
            baseSortingOrder + 23
        );

        lampGlowRenderer = glow.GetComponent<SpriteRenderer>();
        lampCoreRenderer = core.GetComponent<SpriteRenderer>();

        lampGlowBaseColor = lampGlowRenderer.color;
        lampCoreBaseColor = lampCoreRenderer.color;
    }

    private void BuildBottomParts(Transform root)
    {
        CreatePart(root, "BottomBase", roundedRectSprite,
            new Vector3(0f, -0.86f, 0f),
            new Vector3(1.08f, 0.18f, 1f),
            0f,
            bodyShadowColor,
            baseSortingOrder + 6
        );

        CreatePart(root, "FootLeft", roundedRectSprite,
            new Vector3(-0.34f, -0.99f, 0f),
            new Vector3(0.24f, 0.12f, 1f),
            0f,
            bodyDarkColor,
            baseSortingOrder + 5
        );

        CreatePart(root, "FootRight", roundedRectSprite,
            new Vector3(0.34f, -0.99f, 0f),
            new Vector3(0.24f, 0.12f, 1f),
            0f,
            bodyDarkColor,
            baseSortingOrder + 5
        );
    }

    private void BuildLabels(Transform root)
    {
        CreateText(root, "LabelGacha", "GACHA",
            new Vector3(0f, -0.095f, 0f),
            1.25f,
            0.18f,
            textColor,
            baseSortingOrder + 30
        );

        CreateText(root, "LabelPlay", "1 PLAY",
            new Vector3(-0.27f, -0.43f, 0f),
            0.70f,
            0.11f,
            textColor,
            baseSortingOrder + 30
        );

        CreateText(root, "LabelTurn", "TURN",
            new Vector3(0.28f, -0.52f, 0f),
            0.70f,
            0.10f,
            textColor,
            baseSortingOrder + 30
        );

        CreateText(root, "LabelChute", "PRIZE",
            new Vector3(0f, -0.80f, 0f),
            0.68f,
            0.10f,
            new Color(1f, 1f, 1f, 0.95f),
            baseSortingOrder + 30
        );
    }

    private void CreateTinyCapsule(Transform root, Vector3 position, float size, Color bottomColor, int sortingOrder)
    {
        CreatePart(root, "TinyCapsuleBottom", circleSprite,
            position + new Vector3(0f, -size * 0.12f, 0f),
            new Vector3(size, size * 0.80f, 1f),
            0f,
            bottomColor,
            sortingOrder
        );

        CreatePart(root, "TinyCapsuleTopPlastic", circleSprite,
            position + new Vector3(0f, size * 0.10f, 0f),
            new Vector3(size, size * 0.70f, 1f),
            0f,
            new Color(0.85f, 0.96f, 1f, 0.48f),
            sortingOrder + 1
        );

        CreatePart(root, "TinyCapsuleRim", circleSprite,
            position,
            new Vector3(size * 1.02f, size * 0.13f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.55f),
            sortingOrder + 2
        );

        CreatePart(root, "TinyCapsuleHighlight", circleSprite,
            position + new Vector3(-size * 0.22f, size * 0.20f, 0f),
            new Vector3(size * 0.18f, size * 0.28f, 1f),
            -25f,
            new Color(1f, 1f, 1f, 0.42f),
            sortingOrder + 3
        );
    }

    private Color GetCapsuleColor(int index)
    {
        if (capsulePileColors == null || capsulePileColors.Length <= 0)
        {
            return accentColor;
        }

        return capsulePileColors[Mathf.Abs(index) % capsulePileColors.Length];
    }

    private GameObject CreatePart(
        Transform parent,
        string partName,
        Sprite sprite,
        Vector3 localPosition,
        Vector3 localScale,
        float localRotationZ,
        Color color,
        int sortingOrder
    )
    {
        GameObject partObject = new GameObject(partName);
        partObject.transform.SetParent(parent);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localScale = localScale;
        partObject.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

        SetLayer(partObject);

        SpriteRenderer spriteRenderer = partObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingLayerID = cachedSortingLayerId;
        spriteRenderer.sortingOrder = sortingOrder;

        return partObject;
    }

    private void CreateText(
        Transform parent,
        string objectName,
        string text,
        Vector3 localPosition,
        float fontSize,
        float localScale,
        Color color,
        int sortingOrder
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent);
        textObject.transform.localPosition = localPosition;
        textObject.transform.localRotation = Quaternion.identity;
        textObject.transform.localScale = new Vector3(localScale, localScale, 1f);

        SetLayer(textObject);

        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.enableWordWrapping = false;
        textMesh.rectTransform.sizeDelta = new Vector2(3f, 0.6f);

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerID = cachedSortingLayerId;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }

    private void PrepareSprites()
    {
        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite(96);
        }

        if (roundedRectSprite == null)
        {
            roundedRectSprite = CreateRoundedRectSprite(96, 18);
        }
    }

    private void CacheLayerAndSorting()
    {
        cachedSortingLayerId = GetSortingLayerId();
        cachedTargetLayer = GetTargetLayer();
    }

    private int GetSortingLayerId()
    {
        if (!string.IsNullOrEmpty(sortingLayerName) && SortingLayerExists(sortingLayerName))
        {
            return SortingLayer.NameToID(sortingLayerName);
        }

        SpriteRenderer sourceRenderer = GetComponentInChildren<SpriteRenderer>();

        if (sourceRenderer != null)
        {
            return sourceRenderer.sortingLayerID;
        }

        return SortingLayer.NameToID("Default");
    }

    private int GetTargetLayer()
    {
        if (forceLayer && !string.IsNullOrEmpty(targetLayerName))
        {
            int layer = LayerMask.NameToLayer(targetLayerName);

            if (layer >= 0)
            {
                return layer;
            }
        }

        return gameObject.layer;
    }

    private bool SortingLayerExists(string targetSortingLayerName)
    {
        SortingLayer[] sortingLayers = SortingLayer.layers;

        foreach (SortingLayer sortingLayer in sortingLayers)
        {
            if (sortingLayer.name == targetSortingLayerName)
            {
                return true;
            }
        }

        return false;
    }

    private void SetLayer(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        targetObject.layer = cachedTargetLayer;
    }

    private void HideExistingRenderers()
    {
        Transform oldGeneratedRoot = transform.Find(visualRootName);

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (oldGeneratedRoot != null && renderer.transform.IsChildOf(oldGeneratedRoot))
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private void ClearOldVisual()
    {
        Transform oldRoot = transform.Find(visualRootName);

        if (oldRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(oldRoot.gameObject);
        }
        else
        {
            DestroyImmediate(oldRoot.gameObject);
        }
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = CreateTransparentTexture(4, 4, "GachaMachineSquare");

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width
        );
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "GachaMachineCircle");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
    }

    private Sprite CreateRoundedRectSprite(int size, int radius)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "GachaMachineRoundedRect");

        float half = size * 0.5f;
        float rectHalf = half - 1f;
        float cornerRadius = Mathf.Max(1f, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float dx = Mathf.Abs(px - half);
                float dy = Mathf.Abs(py - half);

                bool insideCoreX = dx <= rectHalf - cornerRadius;
                bool insideCoreY = dy <= rectHalf - cornerRadius;

                bool inside = false;

                if (insideCoreX && dy <= rectHalf)
                {
                    inside = true;
                }
                else if (insideCoreY && dx <= rectHalf)
                {
                    inside = true;
                }
                else
                {
                    float cornerX = rectHalf - cornerRadius;
                    float cornerY = rectHalf - cornerRadius;

                    float distanceX = dx - cornerX;
                    float distanceY = dy - cornerY;

                    if (distanceX * distanceX + distanceY * distanceY <= cornerRadius * cornerRadius)
                    {
                        inside = true;
                    }
                }

                if (inside)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
    }

    private Texture2D CreateTransparentTexture(int width, int height, string textureName)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.name = textureName;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        return texture;
    }
}