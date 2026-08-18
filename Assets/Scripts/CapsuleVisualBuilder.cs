using UnityEngine;

public class CapsuleVisualBuilder : MonoBehaviour
{
    [Header("Visual Root")]
    public string visualRootName = "RealCapsuleVisual";
    public bool hideOriginalSpriteRenderer = true;
    public bool rebuildOnStart = true;

    [Header("Sorting")]
    public bool useOriginalRendererSorting = true;
    public string sortingLayerName = "";
    public int baseSortingOrder = 220;

    [Header("Size")]
    public float capsuleScale = 1f;
    public Vector3 visualLocalOffset = Vector3.zero;

    [Header("Color")]
    public bool randomizeBottomColorOnStart = true;
    public Color fixedBottomColor = new Color(0.95f, 0.20f, 0.20f, 1f);

    public Color[] bottomColorPalette =
    {
        new Color(0.93f, 0.18f, 0.18f, 1f),
        new Color(0.20f, 0.48f, 0.95f, 1f),
        new Color(0.96f, 0.76f, 0.20f, 1f),
        new Color(0.22f, 0.78f, 0.36f, 1f),
        new Color(0.62f, 0.34f, 1.00f, 1f),
        new Color(1.00f, 0.45f, 0.16f, 1f),
        new Color(1.00f, 0.35f, 0.70f, 1f)
    };

    [Header("Plastic Look")]
    public Color topPlasticColor = new Color(0.80f, 0.95f, 1.00f, 0.48f);
    public Color rimColor = new Color(0.95f, 0.98f, 1.00f, 0.82f);
    public Color rimShadowColor = new Color(0.12f, 0.16f, 0.20f, 0.25f);
    public Color highlightColor = new Color(1f, 1f, 1f, 0.76f);
    public Color innerShadowColor = new Color(0.15f, 0.25f, 0.35f, 0.24f);
    public Color bottomShadeColor = new Color(0f, 0f, 0f, 0.32f);
    public Color shadowColor = new Color(0f, 0f, 0f, 0.28f);

    private Sprite topHalfSprite;
    private Sprite bottomHalfSprite;
    private Sprite ovalSprite;
    private Sprite bottomShadeSprite;
    private Sprite topInnerShadowSprite;

    private Color runtimeBottomColor;
    private bool hasRuntimeBottomColor;

    private void Start()
    {
        if (randomizeBottomColorOnStart)
        {
            PickRandomBottomColor();
        }

        if (rebuildOnStart)
        {
            RebuildVisual();
        }
    }

    [ContextMenu("Rebuild Capsule Visual")]
    public void RebuildVisual()
    {
        PrepareSprites();

        int finalBaseOrder = GetBaseSortingOrder();
        int finalSortingLayerId = GetSortingLayerId();

        HideOriginalSpriteRenderer();
        ClearOldVisual();

        GameObject root = new GameObject(visualRootName);
        root.transform.SetParent(transform);
        root.transform.localPosition = visualLocalOffset;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = new Vector3(capsuleScale, capsuleScale, 1f);

        Color bottomColor = GetCurrentBottomColor();

        CreatePart(
            root.transform,
            "SoftShadow",
            ovalSprite,
            new Vector3(0f, -0.42f, 0f),
            new Vector3(0.82f, 0.18f, 1f),
            0f,
            shadowColor,
            finalSortingLayerId,
            finalBaseOrder - 10
        );

        CreatePart(
            root.transform,
            "BottomHalf",
            bottomHalfSprite,
            Vector3.zero,
            new Vector3(0.72f, 0.72f, 1f),
            0f,
            bottomColor,
            finalSortingLayerId,
            finalBaseOrder
        );

        CreatePart(
            root.transform,
            "BottomShade",
            bottomShadeSprite,
            Vector3.zero,
            new Vector3(0.72f, 0.72f, 1f),
            0f,
            bottomShadeColor,
            finalSortingLayerId,
            finalBaseOrder + 1
        );

        CreatePart(
            root.transform,
            "TopHalfPlastic",
            topHalfSprite,
            Vector3.zero,
            new Vector3(0.72f, 0.72f, 1f),
            0f,
            topPlasticColor,
            finalSortingLayerId,
            finalBaseOrder + 3
        );

        CreatePart(
            root.transform,
            "TopInnerShadow",
            topInnerShadowSprite,
            Vector3.zero,
            new Vector3(0.72f, 0.72f, 1f),
            0f,
            innerShadowColor,
            finalSortingLayerId,
            finalBaseOrder + 4
        );

        CreatePart(
            root.transform,
            "RimShadow",
            ovalSprite,
            new Vector3(0f, -0.025f, 0f),
            new Vector3(0.74f, 0.105f, 1f),
            0f,
            rimShadowColor,
            finalSortingLayerId,
            finalBaseOrder + 5
        );

        CreatePart(
            root.transform,
            "RimBand",
            ovalSprite,
            new Vector3(0f, 0f, 0f),
            new Vector3(0.74f, 0.085f, 1f),
            0f,
            rimColor,
            finalSortingLayerId,
            finalBaseOrder + 6
        );

        CreatePart(
            root.transform,
            "RimHighlight",
            ovalSprite,
            new Vector3(-0.02f, 0.032f, 0f),
            new Vector3(0.58f, 0.025f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.42f),
            finalSortingLayerId,
            finalBaseOrder + 7
        );

        CreatePart(
            root.transform,
            "HighlightBig",
            ovalSprite,
            new Vector3(-0.15f, 0.21f, 0f),
            new Vector3(0.075f, 0.30f, 1f),
            -32f,
            highlightColor,
            finalSortingLayerId,
            finalBaseOrder + 8
        );

        CreatePart(
            root.transform,
            "HighlightSmall",
            ovalSprite,
            new Vector3(0.04f, 0.285f, 0f),
            new Vector3(0.045f, 0.13f, 1f),
            -28f,
            new Color(1f, 1f, 1f, 0.55f),
            finalSortingLayerId,
            finalBaseOrder + 9
        );

        CreatePart(
            root.transform,
            "TinySpecular",
            ovalSprite,
            new Vector3(-0.26f, 0.08f, 0f),
            new Vector3(0.045f, 0.045f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.45f),
            finalSortingLayerId,
            finalBaseOrder + 10
        );
    }

    [ContextMenu("Randomize Bottom Color And Rebuild")]
    public void RandomizeBottomColorAndRebuild()
    {
        PickRandomBottomColor();
        RebuildVisual();
    }

    public void SetBottomColor(Color color)
    {
        runtimeBottomColor = color;
        hasRuntimeBottomColor = true;
        RebuildVisual();
    }

    private void PickRandomBottomColor()
    {
        if (bottomColorPalette == null || bottomColorPalette.Length <= 0)
        {
            runtimeBottomColor = fixedBottomColor;
            hasRuntimeBottomColor = true;
            return;
        }

        runtimeBottomColor = bottomColorPalette[Random.Range(0, bottomColorPalette.Length)];
        hasRuntimeBottomColor = true;
    }

    private Color GetCurrentBottomColor()
    {
        if (hasRuntimeBottomColor)
        {
            return runtimeBottomColor;
        }

        return fixedBottomColor;
    }

    private void PrepareSprites()
    {
        if (topHalfSprite == null)
        {
            topHalfSprite = CreateHalfCircleSprite(96, true);
        }

        if (bottomHalfSprite == null)
        {
            bottomHalfSprite = CreateHalfCircleSprite(96, false);
        }

        if (ovalSprite == null)
        {
            ovalSprite = CreateOvalSprite(96);
        }

        if (bottomShadeSprite == null)
        {
            bottomShadeSprite = CreateBottomShadeSprite(96);
        }

        if (topInnerShadowSprite == null)
        {
            topInnerShadowSprite = CreateTopInnerShadowSprite(96);
        }
    }

    private void HideOriginalSpriteRenderer()
    {
        if (!hideOriginalSpriteRenderer)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
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

    private int GetBaseSortingOrder()
    {
        SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();

        if (useOriginalRendererSorting && originalRenderer != null)
        {
            return originalRenderer.sortingOrder;
        }

        return baseSortingOrder;
    }

    private int GetSortingLayerId()
    {
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            return SortingLayer.NameToID(sortingLayerName);
        }

        SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();

        if (useOriginalRendererSorting && originalRenderer != null)
        {
            return originalRenderer.sortingLayerID;
        }

        return SortingLayer.NameToID("Default");
    }

    private void CreatePart(
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

        SpriteRenderer spriteRenderer = partObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingLayerID = sortingLayerId;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private Sprite CreateHalfCircleSprite(int size, bool topHalf)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "CapsuleHalfCircle");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                bool insideCircle = distance <= radius;

                bool insideHalf;

                if (topHalf)
                {
                    insideHalf = y >= center.y;
                }
                else
                {
                    insideHalf = y <= center.y;
                }

                if (insideCircle && insideHalf)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();
        return CreateSprite(texture, size);
    }

    private Sprite CreateOvalSprite(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "CapsuleOval");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radiusX = size * 0.48f;
        float radiusY = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedX = (x - center.x) / radiusX;
                float normalizedY = (y - center.y) / radiusY;
                float value = normalizedX * normalizedX + normalizedY * normalizedY;

                if (value <= 1f)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();
        return CreateSprite(texture, size);
    }

    private Sprite CreateBottomShadeSprite(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "CapsuleBottomShade");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance > radius)
                {
                    continue;
                }

                if (y > center.y)
                {
                    continue;
                }

                float downward = Mathf.InverseLerp(center.y, center.y - radius, y);
                float side = Mathf.Abs(x - center.x) / radius;
                float alpha = Mathf.Clamp01(downward * 0.85f + side * 0.18f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return CreateSprite(texture, size);
    }

    private Sprite CreateTopInnerShadowSprite(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "CapsuleTopInnerShadow");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance > radius)
                {
                    continue;
                }

                if (y < center.y)
                {
                    continue;
                }

                float nearRim = 1f - Mathf.InverseLerp(center.y, center.y + radius, y);
                float rightSide = Mathf.InverseLerp(center.x - radius, center.x + radius, x);
                float alpha = Mathf.Clamp01(nearRim * rightSide * 0.75f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return CreateSprite(texture, size);
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

    private Sprite CreateSprite(Texture2D texture, int pixelsPerUnit)
    {
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit
        );
    }
}