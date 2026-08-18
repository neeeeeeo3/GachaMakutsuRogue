using System.Collections.Generic;
using UnityEngine;

public class GachaRarityVisual : MonoBehaviour
{
    [Header("References")]
    public GachaRarityHolder rarityHolder;
    public bool autoFindRarityHolder = true;

    [Header("Build")]
    public string visualRootName = "GachaRarityVisual";
    public bool rebuildOnStart = true;
    public bool rebuildOnEnable = false;
    public bool hideVisualForCommon = true;

    [Header("Transform")]
    public Vector3 visualLocalOffset = new Vector3(0f, -0.05f, 0f);
    public float visualScale = 1f;

    [Header("Color")]
    public bool useHolderCapsuleColor = true;

    [Range(0f, 1f)]
    public float holderColorBlend = 0.45f;

    public Color commonColor = new Color(1f, 1f, 1f, 0.45f);
    public Color rareColor = new Color(0.25f, 0.75f, 1f, 0.85f);
    public Color epicColor = new Color(0.95f, 0.35f, 1f, 0.95f);

    [Header("Sorting")]
    public bool useNearestSpriteRendererSorting = true;
    public string sortingLayerName = "";
    public int fallbackSortingOrder = 2500;
    public int sortingOrderOffset = 30;

    [Header("Aura Size")]
    public float commonAuraSize = 0.55f;
    public float rareAuraSize = 0.72f;
    public float epicAuraSize = 0.92f;

    [Header("Animation")]
    public bool animate = true;
    public float pulseSpeed = 2.6f;
    public float pulseAmount = 0.08f;
    public float ringRotateSpeed = 45f;
    public float epicRingRotateSpeed = 70f;
    public float sparkleTwinkleSpeed = 5.5f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Sprite circleSprite;
    private Sprite ringSprite;
    private Sprite squareSprite;

    private Transform visualRoot;
    private Transform rotatingRing;
    private Transform counterRotatingRing;

    private Vector3 visualRootBaseScale;
    private readonly List<SpriteRenderer> allRenderers = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> sparkleRenderers = new List<SpriteRenderer>();

    private void Start()
    {
        if (rebuildOnStart)
        {
            RebuildVisual();
        }
    }

    private void OnEnable()
    {
        if (rebuildOnEnable)
        {
            RebuildVisual();
        }
    }

    private void Update()
    {
        if (!animate)
        {
            return;
        }

        if (visualRoot == null)
        {
            return;
        }

        float wave = Mathf.Sin(Time.time * pulseSpeed);
        float scale = 1f + wave * pulseAmount;

        visualRoot.localScale = visualRootBaseScale * scale;

        if (rotatingRing != null)
        {
            rotatingRing.Rotate(0f, 0f, ringRotateSpeed * Time.deltaTime);
        }

        if (counterRotatingRing != null)
        {
            counterRotatingRing.Rotate(0f, 0f, -epicRingRotateSpeed * Time.deltaTime);
        }

        for (int i = 0; i < sparkleRenderers.Count; i++)
        {
            SpriteRenderer sparkle = sparkleRenderers[i];

            if (sparkle == null)
            {
                continue;
            }

            Color color = sparkle.color;
            float twinkle = (Mathf.Sin(Time.time * sparkleTwinkleSpeed + i * 0.87f) + 1f) * 0.5f;
            color.a = Mathf.Lerp(0.25f, 0.95f, twinkle);
            sparkle.color = color;
        }
    }

    [ContextMenu("Rebuild Rarity Visual")]
    public void RebuildVisual()
    {
        PrepareSprites();
        AutoFindReferences();
        ClearOldVisual();

        allRenderers.Clear();
        sparkleRenderers.Clear();
        rotatingRing = null;
        counterRotatingRing = null;

        if (rarityHolder == null)
        {
            DebugLog("RarityHolder not found.");
            return;
        }

        if (rarityHolder.rarity == GachaRarityType.Common && hideVisualForCommon)
        {
            DebugLog("Common visual hidden.");
            return;
        }

        int sortingLayerId = GetSortingLayerId();
        int baseSortingOrder = GetBaseSortingOrder();

        GameObject rootObject = new GameObject(visualRootName);
        rootObject.transform.SetParent(transform);
        rootObject.transform.localPosition = visualLocalOffset;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        rootObject.layer = gameObject.layer;

        visualRoot = rootObject.transform;
        visualRootBaseScale = visualRoot.localScale;

        Color rarityColor = GetVisualColor();

        switch (rarityHolder.rarity)
        {
            case GachaRarityType.Common:
                BuildCommonVisual(visualRoot, rarityColor, sortingLayerId, baseSortingOrder);
                break;

            case GachaRarityType.Rare:
                BuildRareVisual(visualRoot, rarityColor, sortingLayerId, baseSortingOrder);
                break;

            case GachaRarityType.Epic:
                BuildEpicVisual(visualRoot, rarityColor, sortingLayerId, baseSortingOrder);
                break;
        }

        DebugLog("Rarity visual rebuilt: " + rarityHolder.rarity);
    }

    public void ApplyHolderAndRebuild(GachaRarityHolder newHolder)
    {
        rarityHolder = newHolder;
        RebuildVisual();
    }

    private void BuildCommonVisual(
        Transform root,
        Color color,
        int sortingLayerId,
        int baseSortingOrder
    )
    {
        Color auraColor = color;
        auraColor.a = 0.18f;

        CreatePart(
            root,
            "CommonAura",
            circleSprite,
            Vector3.zero,
            new Vector3(commonAuraSize, commonAuraSize * 0.28f, 1f),
            0f,
            auraColor,
            sortingLayerId,
            baseSortingOrder
        );
    }

    private void BuildRareVisual(
        Transform root,
        Color color,
        int sortingLayerId,
        int baseSortingOrder
    )
    {
        Color auraColor = color;
        auraColor.a = 0.26f;

        CreatePart(
            root,
            "RareAuraBack",
            circleSprite,
            Vector3.zero,
            new Vector3(rareAuraSize, rareAuraSize * 0.32f, 1f),
            0f,
            auraColor,
            sortingLayerId,
            baseSortingOrder
        );

        Color ringColor = color;
        ringColor.a = 0.72f;

        GameObject ringObject = CreatePart(
            root,
            "RareRing",
            ringSprite,
            Vector3.zero,
            new Vector3(rareAuraSize * 0.95f, rareAuraSize * 0.36f, 1f),
            0f,
            ringColor,
            sortingLayerId,
            baseSortingOrder + 1
        );

        rotatingRing = ringObject.transform;

        CreateSparkles(
            root,
            7,
            rareAuraSize * 0.48f,
            color,
            0.065f,
            sortingLayerId,
            baseSortingOrder + 2
        );
    }

    private void BuildEpicVisual(
        Transform root,
        Color color,
        int sortingLayerId,
        int baseSortingOrder
    )
    {
        Color auraColor = color;
        auraColor.a = 0.34f;

        CreatePart(
            root,
            "EpicAuraBack",
            circleSprite,
            Vector3.zero,
            new Vector3(epicAuraSize, epicAuraSize * 0.38f, 1f),
            0f,
            auraColor,
            sortingLayerId,
            baseSortingOrder
        );

        Color whiteGlow = new Color(1f, 1f, 1f, 0.18f);

        CreatePart(
            root,
            "EpicWhiteGlow",
            circleSprite,
            Vector3.zero,
            new Vector3(epicAuraSize * 0.72f, epicAuraSize * 0.24f, 1f),
            0f,
            whiteGlow,
            sortingLayerId,
            baseSortingOrder + 1
        );

        Color ringColor = color;
        ringColor.a = 0.82f;

        GameObject ringObject = CreatePart(
            root,
            "EpicRingOuter",
            ringSprite,
            Vector3.zero,
            new Vector3(epicAuraSize * 1.02f, epicAuraSize * 0.42f, 1f),
            0f,
            ringColor,
            sortingLayerId,
            baseSortingOrder + 2
        );

        rotatingRing = ringObject.transform;

        Color innerRingColor = new Color(1f, 1f, 1f, 0.62f);

        GameObject innerRingObject = CreatePart(
            root,
            "EpicRingInner",
            ringSprite,
            Vector3.zero,
            new Vector3(epicAuraSize * 0.72f, epicAuraSize * 0.28f, 1f),
            0f,
            innerRingColor,
            sortingLayerId,
            baseSortingOrder + 3
        );

        counterRotatingRing = innerRingObject.transform;

        CreateEpicRays(
            root,
            color,
            sortingLayerId,
            baseSortingOrder + 4
        );

        CreateSparkles(
            root,
            12,
            epicAuraSize * 0.58f,
            color,
            0.075f,
            sortingLayerId,
            baseSortingOrder + 5
        );
    }

    private void CreateSparkles(
        Transform root,
        int count,
        float radius,
        Color color,
        float sparkleSize,
        int sortingLayerId,
        int sortingOrder
    )
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count);
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;

            float ySquash = 0.42f;
            Vector3 position = new Vector3(
                direction.x * radius,
                direction.y * radius * ySquash,
                0f
            );

            Color sparkleColor = i % 2 == 0 ? color : Color.white;
            sparkleColor.a = 0.85f;

            GameObject sparkleObject = CreatePart(
                root,
                "Sparkle_" + i,
                squareSprite,
                position,
                new Vector3(sparkleSize, sparkleSize, 1f),
                45f,
                sparkleColor,
                sortingLayerId,
                sortingOrder
            );

            SpriteRenderer sparkleRenderer = sparkleObject.GetComponent<SpriteRenderer>();

            if (sparkleRenderer != null)
            {
                sparkleRenderers.Add(sparkleRenderer);
            }
        }
    }

    private void CreateEpicRays(
        Transform root,
        Color color,
        int sortingLayerId,
        int sortingOrder
    )
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;

            Vector3 position = new Vector3(
                direction.x * 0.34f,
                direction.y * 0.18f,
                0f
            );

            Color rayColor = color;
            rayColor.a = 0.38f;

            CreatePart(
                root,
                "EpicRay_" + i,
                squareSprite,
                position,
                new Vector3(0.045f, 0.28f, 1f),
                angle,
                rayColor,
                sortingLayerId,
                sortingOrder
            );
        }
    }

    private GameObject CreatePart(
        Transform parent,
        string partName,
        Sprite sprite,
        Vector3 localPosition,
        Vector3 localScale,
        float localRotationZ,
        Color color,
        int sortingLayerId,
        int sortingOrder
    )
    {
        GameObject partObject = new GameObject(partName);
        partObject.transform.SetParent(parent);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localScale = localScale;
        partObject.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
        partObject.layer = gameObject.layer;

        SpriteRenderer spriteRenderer = partObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingLayerID = sortingLayerId;
        spriteRenderer.sortingOrder = sortingOrder;

        allRenderers.Add(spriteRenderer);

        return partObject;
    }

    private Color GetVisualColor()
    {
        Color baseColor = GetDefaultRarityColor(rarityHolder.rarity);

        if (!useHolderCapsuleColor)
        {
            return baseColor;
        }

        if (rarityHolder == null)
        {
            return baseColor;
        }

        Color holderColor = rarityHolder.capsuleColor;

        if (holderColor.a <= 0f)
        {
            holderColor.a = 1f;
        }

        Color mixedColor = Color.Lerp(baseColor, holderColor, holderColorBlend);
        mixedColor.a = baseColor.a;

        return mixedColor;
    }

    private Color GetDefaultRarityColor(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return commonColor;

            case GachaRarityType.Rare:
                return rareColor;

            case GachaRarityType.Epic:
                return epicColor;
        }

        return Color.white;
    }

    private void PrepareSprites()
    {
        if (circleSprite == null)
        {
            circleSprite = CreateCircleSprite(96);
        }

        if (ringSprite == null)
        {
            ringSprite = CreateRingSprite(96, 0.68f);
        }

        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }
    }

    private void AutoFindReferences()
    {
        if (!autoFindRarityHolder)
        {
            return;
        }

        if (rarityHolder != null)
        {
            return;
        }

        rarityHolder = GetComponent<GachaRarityHolder>();

        if (rarityHolder == null)
        {
            rarityHolder = GetComponentInParent<GachaRarityHolder>();
        }
    }

    private int GetSortingLayerId()
    {
        if (!string.IsNullOrEmpty(sortingLayerName) && SortingLayerExists(sortingLayerName))
        {
            return SortingLayer.NameToID(sortingLayerName);
        }

        if (useNearestSpriteRendererSorting)
        {
            SpriteRenderer sourceRenderer = GetSourceSpriteRenderer();

            if (sourceRenderer != null)
            {
                return sourceRenderer.sortingLayerID;
            }
        }

        return SortingLayer.NameToID("Default");
    }

    private int GetBaseSortingOrder()
    {
        if (useNearestSpriteRendererSorting)
        {
            SpriteRenderer sourceRenderer = GetSourceSpriteRenderer();

            if (sourceRenderer != null)
            {
                return sourceRenderer.sortingOrder + sortingOrderOffset;
            }
        }

        return fallbackSortingOrder;
    }

    private SpriteRenderer GetSourceSpriteRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (visualRoot != null && renderer.transform.IsChildOf(visualRoot))
            {
                continue;
            }

            if (renderer.transform.name.Contains(visualRootName))
            {
                continue;
            }

            return renderer;
        }

        return null;
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
        Texture2D texture = CreateTransparentTexture(4, 4, "RarityVisualSquare");

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
        Texture2D texture = CreateTransparentTexture(size, size, "RarityVisualCircle");

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

    private Sprite CreateRingSprite(int size, float innerRadiusRatio)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "RarityVisualRing");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float outerRadius = size * 0.48f;
        float innerRadius = outerRadius * Mathf.Clamp01(innerRadiusRatio);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= outerRadius && distance >= innerRadius)
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

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("GachaRarityVisual: " + message);
    }
}