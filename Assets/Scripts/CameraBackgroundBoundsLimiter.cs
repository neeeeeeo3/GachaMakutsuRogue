using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraBackgroundBoundsLimiter : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public bool autoFindCamera = true;

    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Tooltip("DepthBackgroundBuilder をドラッグ。空でも自動検索できます。")]
    public MonoBehaviour depthBackgroundBuilder;
    public bool autoFindDepthBackgroundBuilder = true;
    public string depthBackgroundBuilderTypeName = "DepthBackgroundBuilder";

    [Header("Use Background Size")]
    public bool useDepthBackgroundBuilderSettings = true;

    [Tooltip("DepthBackgroundBuilder が見つからない時の横余白です。")]
    public float fallbackHorizontalPadding = 8f;

    [Tooltip("DepthBackgroundBuilder が見つからない時の空の高さです。")]
    public float fallbackSkyHeight = 9f;

    [Tooltip("DepthBackgroundBuilder が見つからない時の地下の深さです。")]
    public float fallbackUndergroundExtraDepth = 14f;

    [Header("Clamp")]
    public bool clampX = true;
    public bool clampY = true;

    [Tooltip("ON推奨。カメラ中心ではなく、画面端が背景外に出ないように制限します。")]
    public bool keepCameraViewInsideBounds = true;

    [Tooltip("背景端から少し内側に制限したい時に使います。")]
    public Vector2 insideMargin = Vector2.zero;

    [Header("Manual Extra Bounds")]
    [Tooltip("さらに左へ見せたい量。通常は0でOK。")]
    public float extraLeft = 0f;

    [Tooltip("さらに右へ見せたい量。通常は0でOK。")]
    public float extraRight = 0f;

    [Tooltip("さらに上へ見せたい量。通常は0でOK。")]
    public float extraTop = 0f;

    [Tooltip("さらに下へ見せたい量。通常は0でOK。")]
    public float extraBottom = 0f;

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0.3f, 1f, 1f, 0.35f);
    public bool showDebugLog = false;

    private void Start()
    {
        AutoFindReferences();
        ClampCameraNow();
    }

    private void LateUpdate()
    {
        ClampCameraNow();
    }

    [ContextMenu("Clamp Camera Now")]
    public void ClampCameraNow()
    {
        AutoFindReferences();

        if (targetCamera == null)
        {
            return;
        }

        if (dungeonGridManager == null)
        {
            return;
        }

        Rect bounds = CalculateBackgroundBounds();

        Vector3 position = targetCamera.transform.position;

        float minCenterX = bounds.xMin;
        float maxCenterX = bounds.xMax;
        float minCenterY = bounds.yMin;
        float maxCenterY = bounds.yMax;

        if (keepCameraViewInsideBounds && targetCamera.orthographic)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            minCenterX += halfWidth;
            maxCenterX -= halfWidth;
            minCenterY += halfHeight;
            maxCenterY -= halfHeight;
        }

        minCenterX += insideMargin.x;
        maxCenterX -= insideMargin.x;
        minCenterY += insideMargin.y;
        maxCenterY -= insideMargin.y;

        if (clampX)
        {
            if (minCenterX > maxCenterX)
            {
                position.x = bounds.center.x;
            }
            else
            {
                position.x = Mathf.Clamp(position.x, minCenterX, maxCenterX);
            }
        }

        if (clampY)
        {
            if (minCenterY > maxCenterY)
            {
                position.y = bounds.center.y;
            }
            else
            {
                position.y = Mathf.Clamp(position.y, minCenterY, maxCenterY);
            }
        }

        targetCamera.transform.position = position;
    }

    private Rect CalculateBackgroundBounds()
    {
        float gridLeft = dungeonGridManager.origin.x - dungeonGridManager.tileSize * 0.5f;
        float gridRight = dungeonGridManager.origin.x + (dungeonGridManager.width - 1) * dungeonGridManager.tileSize + dungeonGridManager.tileSize * 0.5f;
        float gridBottom = dungeonGridManager.origin.y - dungeonGridManager.tileSize * 0.5f;
        float gridTop = dungeonGridManager.origin.y + (dungeonGridManager.height - 1) * dungeonGridManager.tileSize + dungeonGridManager.tileSize * 0.5f;

        float horizontalPadding = fallbackHorizontalPadding;
        float skyHeight = fallbackSkyHeight;
        float undergroundExtraDepth = fallbackUndergroundExtraDepth;

        if (useDepthBackgroundBuilderSettings && depthBackgroundBuilder != null)
        {
            horizontalPadding = GetFloatFieldValue(depthBackgroundBuilder, "horizontalPadding", horizontalPadding);
            skyHeight = GetFloatFieldValue(depthBackgroundBuilder, "skyHeight", skyHeight);
            undergroundExtraDepth = GetFloatFieldValue(depthBackgroundBuilder, "undergroundExtraDepth", undergroundExtraDepth);
        }

        float left = gridLeft - horizontalPadding - extraLeft;
        float right = gridRight + horizontalPadding + extraRight;
        float top = gridTop + skyHeight + extraTop;
        float bottom = gridBottom - undergroundExtraDepth - extraBottom;

        return Rect.MinMaxRect(left, bottom, right, top);
    }

    private float GetFloatFieldValue(MonoBehaviour target, string fieldName, float fallbackValue)
    {
        if (target == null)
        {
            return fallbackValue;
        }

        FieldInfo fieldInfo = target.GetType().GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.Instance
        );

        if (fieldInfo == null)
        {
            return fallbackValue;
        }

        object value = fieldInfo.GetValue(target);

        if (value is float floatValue)
        {
            return floatValue;
        }

        return fallbackValue;
    }

    private void AutoFindReferences()
    {
        if (autoFindCamera && targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

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

        if (autoFindDepthBackgroundBuilder && depthBackgroundBuilder == null)
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.GetType().Name == depthBackgroundBuilderTypeName)
                {
                    depthBackgroundBuilder = behaviour;
                    break;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        if (dungeonGridManager == null)
        {
            return;
        }

        Rect bounds = CalculateBackgroundBounds();

        Gizmos.color = gizmoColor;

        Vector3 center = new Vector3(bounds.center.x, bounds.center.y, 0f);
        Vector3 size = new Vector3(bounds.width, bounds.height, 0f);

        Gizmos.DrawCube(center, size);
        Gizmos.DrawWireCube(center, size);
    }
}