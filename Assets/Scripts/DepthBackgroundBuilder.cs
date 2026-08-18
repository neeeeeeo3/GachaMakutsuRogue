using UnityEngine;

public class DepthBackgroundBuilder : MonoBehaviour
{
    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Build")]
    public bool rebuildOnStart = true;
    public string generatedRootName = "GeneratedDepthBackground";

    [Header("World Size")]
    public float horizontalPadding = 8f;
    public float skyHeight = 9f;
    public float undergroundExtraDepth = 14f;
    public float surfaceThickness = 0.35f;

    [Header("Sorting")]
    public string sortingLayerName = "Default";
    public int baseSortingOrder = -300;

    [Header("Sky")]
    public Color skyColor = new Color(0.46f, 0.74f, 1f, 1f);
    public bool createSun = true;
    public Color sunColor = new Color(1f, 0.86f, 0.32f, 1f);
    public Vector2 sunOffsetFromLeftTop = new Vector2(2.2f, -2.0f);
    public float sunSize = 1.2f;

    [Header("Clouds")]
    public bool createClouds = true;
    public Color cloudColor = new Color(1f, 1f, 1f, 0.9f);
    public int cloudCount = 4;

    [Header("Surface")]
    public Color grassColor = new Color(0.28f, 0.72f, 0.25f, 1f);
    public Color surfaceDirtColor = new Color(0.42f, 0.27f, 0.12f, 1f);

    [Header("Underground")]
    public Color undergroundBaseColor = new Color(0.18f, 0.11f, 0.065f, 1f);
    public Color deepUndergroundColor = new Color(0.10f, 0.065f, 0.045f, 1f);

    [Header("Underground Details")]
    public bool createStrataLines = true;
    public int strataLineCount = 12;
    public Color strataLineColor = new Color(0.34f, 0.22f, 0.12f, 0.9f);

    public bool createRoots = true;
    public int rootCount = 9;
    public Color rootColor = new Color(0.23f, 0.13f, 0.065f, 1f);

    public bool createRocks = true;
    public int rockCount = 28;
    public Color rockColor = new Color(0.28f, 0.25f, 0.22f, 1f);

    [Header("Random")]
    public int randomSeed = 1207;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Transform generatedRoot;
    private Sprite squareSprite;
    private Sprite circleSprite;

    private void Start()
    {
        if (rebuildOnStart)
        {
            RebuildBackground();
        }
    }

    [ContextMenu("Rebuild Background")]
    public void RebuildBackground()
    {
        AutoFindReferences();

        if (dungeonGridManager == null)
        {
            Debug.LogWarning("DepthBackgroundBuilder: DungeonGridManager not found.");
            return;
        }

        ClearGeneratedBackground();

        squareSprite = CreateSquareSprite();
        circleSprite = CreateCircleSprite(48);

        GameObject rootObject = new GameObject(generatedRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.position = Vector3.zero;
        generatedRoot = rootObject.transform;

        Random.State oldRandomState = Random.state;
        Random.InitState(randomSeed);

        BuildMainBackground();
        BuildSurface();

        if (createSun)
        {
            BuildSun();
        }

        if (createClouds)
        {
            BuildClouds();
        }

        if (createStrataLines)
        {
            BuildStrataLines();
        }

        if (createRoots)
        {
            BuildRoots();
        }

        if (createRocks)
        {
            BuildRocks();
        }

        Random.state = oldRandomState;

        DebugLog("Depth background rebuilt.");
    }

    private void BuildMainBackground()
    {
        float left = GetLeft();
        float right = GetRight();
        float width = right - left;
        float centerX = (left + right) * 0.5f;

        float surfaceY = GetSurfaceY();
        float skyCenterY = surfaceY + skyHeight * 0.5f;
        float undergroundBottomY = GetUndergroundBottomY();
        float undergroundHeight = surfaceY - undergroundBottomY;
        float undergroundCenterY = (surfaceY + undergroundBottomY) * 0.5f;

        CreateRect(
            "SkyBackground",
            new Vector3(centerX, skyCenterY, 0f),
            new Vector2(width, skyHeight),
            skyColor,
            0
        );

        CreateRect(
            "UndergroundBackground",
            new Vector3(centerX, undergroundCenterY, 0f),
            new Vector2(width, undergroundHeight),
            undergroundBaseColor,
            0
        );

        float deepHeight = undergroundHeight * 0.45f;
        float deepCenterY = undergroundBottomY + deepHeight * 0.5f;

        CreateRect(
            "DeepUndergroundTint",
            new Vector3(centerX, deepCenterY, 0f),
            new Vector2(width, deepHeight),
            deepUndergroundColor,
            1
        );
    }

    private void BuildSurface()
    {
        float left = GetLeft();
        float right = GetRight();
        float width = right - left;
        float centerX = (left + right) * 0.5f;
        float surfaceY = GetSurfaceY();

        CreateRect(
            "GrassSurface",
            new Vector3(centerX, surfaceY + surfaceThickness * 0.18f, 0f),
            new Vector2(width, surfaceThickness),
            grassColor,
            4
        );

        CreateRect(
            "SurfaceDirtLine",
            new Vector3(centerX, surfaceY - surfaceThickness * 0.48f, 0f),
            new Vector2(width, surfaceThickness * 0.35f),
            surfaceDirtColor,
            5
        );
    }

    private void BuildSun()
    {
        float left = GetLeft();
        float surfaceY = GetSurfaceY();
        float topY = surfaceY + skyHeight;

        Vector3 sunPosition = new Vector3(
            left + sunOffsetFromLeftTop.x,
            topY + sunOffsetFromLeftTop.y,
            0f
        );

        CreateCircle(
            "Sun",
            sunPosition,
            sunSize,
            sunColor,
            3
        );
    }

    private void BuildClouds()
    {
        float left = GetLeft();
        float right = GetRight();
        float surfaceY = GetSurfaceY();
        float topY = surfaceY + skyHeight;

        int safeCloudCount = Mathf.Max(0, cloudCount);

        for (int i = 0; i < safeCloudCount; i++)
        {
            float x = Random.Range(left + 1.5f, right - 1.5f);
            float y = Random.Range(surfaceY + skyHeight * 0.45f, topY - 0.8f);
            float size = Random.Range(0.5f, 0.9f);

            Transform cloudRoot = new GameObject("Cloud_" + i).transform;
            cloudRoot.SetParent(generatedRoot, false);
            cloudRoot.position = new Vector3(x, y, 0f);

            CreateCircle(
                "CloudPart_A",
                cloudRoot.position + new Vector3(-size * 0.55f, 0f, 0f),
                size * 0.92f,
                cloudColor,
                2
            ).transform.SetParent(cloudRoot, true);

            CreateCircle(
                "CloudPart_B",
                cloudRoot.position + new Vector3(0f, size * 0.12f, 0f),
                size * 1.15f,
                cloudColor,
                2
            ).transform.SetParent(cloudRoot, true);

            CreateCircle(
                "CloudPart_C",
                cloudRoot.position + new Vector3(size * 0.58f, -size * 0.03f, 0f),
                size * 0.86f,
                cloudColor,
                2
            ).transform.SetParent(cloudRoot, true);

            CreateRect(
                "CloudBase",
                cloudRoot.position + new Vector3(0f, -size * 0.18f, 0f),
                new Vector2(size * 2.3f, size * 0.48f),
                cloudColor,
                2
            ).transform.SetParent(cloudRoot, true);
        }
    }

    private void BuildStrataLines()
    {
        float left = GetLeft();
        float right = GetRight();
        float width = right - left;
        float centerX = (left + right) * 0.5f;
        float surfaceY = GetSurfaceY();
        float bottomY = GetUndergroundBottomY();

        int safeCount = Mathf.Max(0, strataLineCount);

        for (int i = 0; i < safeCount; i++)
        {
            float t = (i + 1f) / (safeCount + 1f);
            float y = Mathf.Lerp(surfaceY - 0.9f, bottomY + 0.8f, t);
            float thickness = Random.Range(0.035f, 0.08f);
            float lineWidth = width * Random.Range(0.82f, 1.08f);
            float xOffset = Random.Range(-0.45f, 0.45f);

            CreateRect(
                "StrataLine_" + i,
                new Vector3(centerX + xOffset, y, 0f),
                new Vector2(lineWidth, thickness),
                strataLineColor,
                2
            );
        }
    }

    private void BuildRoots()
    {
        float left = GetLeft();
        float right = GetRight();
        float surfaceY = GetSurfaceY();

        int safeCount = Mathf.Max(0, rootCount);

        for (int i = 0; i < safeCount; i++)
        {
            float x = Random.Range(left + 0.7f, right - 0.7f);
            float length = Random.Range(0.8f, 2.8f);
            float thickness = Random.Range(0.035f, 0.08f);

            CreateRect(
                "Root_" + i,
                new Vector3(x, surfaceY - length * 0.5f, 0f),
                new Vector2(thickness, length),
                rootColor,
                6
            );

            if (Random.value > 0.45f)
            {
                float branchLength = Random.Range(0.35f, 0.9f);
                float branchDirection = Random.value > 0.5f ? 1f : -1f;

                GameObject branch = CreateRect(
                    "RootBranch_" + i,
                    new Vector3(x + branchDirection * branchLength * 0.35f, surfaceY - length * 0.62f, 0f),
                    new Vector2(thickness * 0.75f, branchLength),
                    rootColor,
                    6
                );

                branch.transform.rotation = Quaternion.Euler(0f, 0f, branchDirection * -38f);
            }
        }
    }

    private void BuildRocks()
    {
        float left = GetLeft();
        float right = GetRight();
        float surfaceY = GetSurfaceY();
        float bottomY = GetUndergroundBottomY();

        int safeCount = Mathf.Max(0, rockCount);

        for (int i = 0; i < safeCount; i++)
        {
            float x = Random.Range(left + 0.4f, right - 0.4f);
            float y = Random.Range(bottomY + 0.4f, surfaceY - 0.7f);
            float size = Random.Range(0.06f, 0.18f);

            CreateCircle(
                "Rock_" + i,
                new Vector3(x, y, 0f),
                size,
                rockColor,
                4
            );
        }
    }

    private GameObject CreateRect(
        string objectName,
        Vector3 position,
        Vector2 size,
        Color color,
        int sortingOffset
    )
    {
        GameObject rectObject = new GameObject(objectName);
        rectObject.transform.SetParent(generatedRoot, false);
        rectObject.transform.position = position;
        rectObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = rectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = squareSprite;
        renderer.color = color;
        renderer.sortingOrder = baseSortingOrder + sortingOffset;

        ApplySortingLayer(renderer);

        return rectObject;
    }

    private GameObject CreateCircle(
        string objectName,
        Vector3 position,
        float diameter,
        Color color,
        int sortingOffset
    )
    {
        GameObject circleObject = new GameObject(objectName);
        circleObject.transform.SetParent(generatedRoot, false);
        circleObject.transform.position = position;
        circleObject.transform.localScale = new Vector3(diameter, diameter, 1f);

        SpriteRenderer renderer = circleObject.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.color = color;
        renderer.sortingOrder = baseSortingOrder + sortingOffset;

        ApplySortingLayer(renderer);

        return circleObject;
    }

    private void ApplySortingLayer(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        if (SortingLayerExists(sortingLayerName))
        {
            renderer.sortingLayerName = sortingLayerName;
        }
    }

    private bool SortingLayerExists(string targetSortingLayerName)
    {
        if (string.IsNullOrWhiteSpace(targetSortingLayerName))
        {
            return false;
        }

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

    private float GetLeft()
    {
        return dungeonGridManager.GetGridLeftEdgeWorldX() - horizontalPadding;
    }

    private float GetRight()
    {
        return dungeonGridManager.GetGridRightEdgeWorldX() + horizontalPadding;
    }

    private float GetSurfaceY()
    {
        return dungeonGridManager.GetGridTopEdgeWorldY();
    }

    private float GetUndergroundBottomY()
    {
        return dungeonGridManager.GetGridBottomEdgeWorldY() - undergroundExtraDepth;
    }

    private void AutoFindReferences()
    {
        if (autoFindDungeonGridManager && dungeonGridManager == null)
        {
            if (DungeonGridManager.Instance != null)
            {
                dungeonGridManager = DungeonGridManager.Instance;
            }
            else
            {
                dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
            }
        }
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "DepthBackgroundSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private Sprite CreateCircleSprite(int textureSize)
    {
        int safeSize = Mathf.Max(8, textureSize);

        Texture2D texture = new Texture2D(safeSize, safeSize, TextureFormat.RGBA32, false);
        texture.name = "DepthBackgroundCircleTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((safeSize - 1) * 0.5f, (safeSize - 1) * 0.5f);
        float radius = safeSize * 0.48f;

        for (int y = 0; y < safeSize; y++)
        {
            for (int x = 0; x < safeSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance);

                Color pixelColor = new Color(1f, 1f, 1f, alpha);
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, safeSize, safeSize),
            new Vector2(0.5f, 0.5f),
            safeSize
        );
    }

    private void ClearGeneratedBackground()
    {
        Transform existingRoot = transform.Find(generatedRootName);

        if (existingRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existingRoot.gameObject);
        }
        else
        {
            DestroyImmediate(existingRoot.gameObject);
        }
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("DepthBackgroundBuilder: " + message);
    }
}