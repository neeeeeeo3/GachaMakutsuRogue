using UnityEngine;

[ExecuteAlways]
public class GameplayViewportFitter : MonoBehaviour
{
    [Header("UI Margins In Pixels")]
    public int leftMargin = 230;
    public int rightMargin = 300;
    public int topMargin = 80;
    public int bottomMargin = 180;

    [Header("Dungeon Auto Fit")]
    public bool autoFitDungeonGrid = true;
    public bool autoCenterOnDungeonGrid = true;
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Camera Padding")]
    public float worldPadding = 0.6f;
    public float minOrthographicSize = 3f;

    [Header("Update")]
    public bool updateEveryFrame = true;

    private Camera targetCamera;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ApplyViewport();
    }

    private void Start()
    {
        ApplyViewport();
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            ApplyViewport();
        }
    }

    private void OnValidate()
    {
        targetCamera = GetComponent<Camera>();
        ApplyViewport();
    }

    public void ApplyViewport()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        int screenWidth = Mathf.Max(Screen.width, 1);
        int screenHeight = Mathf.Max(Screen.height, 1);

        int safeLeft = Mathf.Clamp(leftMargin, 0, screenWidth - 1);
        int safeRight = Mathf.Clamp(rightMargin, 0, screenWidth - safeLeft - 1);
        int safeTop = Mathf.Clamp(topMargin, 0, screenHeight - 1);
        int safeBottom = Mathf.Clamp(bottomMargin, 0, screenHeight - safeTop - 1);

        int viewPixelWidth = Mathf.Max(100, screenWidth - safeLeft - safeRight);
        int viewPixelHeight = Mathf.Max(100, screenHeight - safeTop - safeBottom);

        float viewportX = (float)safeLeft / screenWidth;
        float viewportY = (float)safeBottom / screenHeight;
        float viewportWidth = (float)viewPixelWidth / screenWidth;
        float viewportHeight = (float)viewPixelHeight / screenHeight;

        targetCamera.rect = new Rect(
            viewportX,
            viewportY,
            viewportWidth,
            viewportHeight
        );

        if (autoFitDungeonGrid)
        {
            FitDungeonGrid(viewPixelWidth, viewPixelHeight);
        }
    }

    private void FitDungeonGrid(int viewPixelWidth, int viewPixelHeight)
    {
        DungeonGridManager grid = GetDungeonGridManager();

        if (grid == null)
        {
            return;
        }

        if (!targetCamera.orthographic)
        {
            targetCamera.orthographic = true;
        }

        float tileSize = grid.tileSize;

        float minX = grid.origin.x - tileSize * 0.5f;
        float maxX = grid.origin.x + (grid.width - 1) * tileSize + tileSize * 0.5f;

        float minY = grid.origin.y - tileSize * 0.5f;
        float maxY = grid.origin.y + (grid.height - 1) * tileSize + tileSize * 0.5f;

        float worldWidth = maxX - minX;
        float worldHeight = maxY - minY;

        float viewportAspect = (float)viewPixelWidth / Mathf.Max(viewPixelHeight, 1);

        float sizeByHeight = worldHeight * 0.5f;
        float sizeByWidth = worldWidth / (2f * viewportAspect);

        float targetSize = Mathf.Max(sizeByHeight, sizeByWidth) + worldPadding;

        if (targetSize < minOrthographicSize)
        {
            targetSize = minOrthographicSize;
        }

        targetCamera.orthographicSize = targetSize;

        if (autoCenterOnDungeonGrid)
        {
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;

            Vector3 cameraPosition = targetCamera.transform.position;
            cameraPosition.x = centerX;
            cameraPosition.y = centerY;

            if (Mathf.Abs(cameraPosition.z) < 0.01f)
            {
                cameraPosition.z = -10f;
            }

            targetCamera.transform.position = cameraPosition;
        }
    }

    private DungeonGridManager GetDungeonGridManager()
    {
        if (dungeonGridManager == null && autoFindDungeonGridManager)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }

        return dungeonGridManager;
    }
}