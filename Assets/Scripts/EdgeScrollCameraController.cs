using UnityEngine;
using UnityEngine.EventSystems;

public class EdgeScrollCameraController : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;
    public bool autoUseThisCamera = true;

    [Header("Forced Zoom")]
    public bool forceOrthographicSize = true;
    public float desiredOrthographicSize = 5f;
    public bool applyOrthographicSizeEveryFrame = true;

    [Header("Movement")]
    public bool enableEdgeScroll = true;
    public bool enableKeyboardScroll = true;

    [Tooltip("画面端から何px以内でスクロールするか。まずは大きめ推奨。")]
    public float edgeSizePixels = 140f;

    public float edgeScrollSpeed = 16f;
    public float keyboardScrollSpeed = 16f;
    public float diagonalSpeedMultiplier = 0.85f;

    [Header("Edge Detection")]
    [Tooltip("ON推奨。Screen.width / Screen.height基準で端判定します。")]
    public bool useScreenEdgeDetection = true;

    [Tooltip("ON推奨。CameraのViewport基準でも端判定します。複数カメラでScreen判定が怪しい時の保険。")]
    public bool useViewportEdgeDetection = true;

    [Tooltip("OFF推奨。ONだとPlay開始直後にマウスを動かすまで端スクロールしません。")]
    public bool blockEdgeScrollUntilMouseMovesAfterStart = false;

    public float mouseMoveDetectDistance = 4f;

    [Tooltip("ON推奨。Input.mousePosition が 0,0 付近の時だけ無視します。")]
    public bool ignoreZeroMousePosition = true;

    public float zeroMousePositionTolerance = 2f;

    [Header("Keyboard Keys")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;

    [Header("Bounds Clamp")]
    [Tooltip("最初はOFF推奨。ONだと範囲計算ミスでカメラがほぼ動かなくなることがあります。")]
    public bool useCameraBoundsClamp = false;

    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;
    public bool calculateBoundsFromDungeonTiles = true;

    public Vector2 manualMinBounds = new Vector2(-30f, -18f);
    public Vector2 manualMaxBounds = new Vector2(30f, 18f);
    public Vector2 boundsPadding = new Vector2(4f, 4f);

    public bool expandBoundsIfTooSmall = true;
    public Vector2 minimumScrollableWorldSize = new Vector2(36f, 24f);

    [Header("Smoothing")]
    [Tooltip("最初はOFF推奨。ONだと動きがかなり弱く見えることがあります。")]
    public bool useSmoothMove = false;

    public float smoothTime = 0.08f;

    [Header("Input Blocking")]
    [Tooltip("最初はOFF推奨。透明UIが画面全体にあると端スクロールが完全に止まります。")]
    public bool blockWhenPointerOverUI = false;

    public bool blockWhenMouseOutsideGameWindow = true;

    [Tooltip("最初はOFF推奨。ガチャカメラ判定が広く取られていると端スクロールが止まります。")]
    public bool blockWhenPointerOverGachaCamera = false;

    public Camera gachaCamera;
    public bool autoFindGachaCamera = true;
    public string gachaCameraName = "GachaMachineCamera";

    [Header("Debug")]
    public bool showDebugLog = true;
    public float debugLogInterval = 0.35f;
    public bool drawBoundsGizmo = true;

    private Vector2 worldMinBounds;
    private Vector2 worldMaxBounds;

    private Vector3 smoothVelocity;
    private Vector3 targetPosition;
    private bool hasTargetPosition;

    private bool hasBounds;

    private Vector2 startMousePosition;
    private bool hasMouseMovedAfterStart;

    private float nextDebugLogTime;

    private void Start()
    {
        AutoFindReferences();
        ApplyForcedOrthographicSize();
        RecalculateBounds();

        if (targetCamera != null)
        {
            targetPosition = targetCamera.transform.position;
            hasTargetPosition = true;
        }

        startMousePosition = Input.mousePosition;
        hasMouseMovedAfterStart = !blockEdgeScrollUntilMouseMovesAfterStart;
    }

    private void Update()
    {
        AutoFindReferences();
        ApplyForcedOrthographicSize();
        UpdateMouseMovedFlag();

        if (targetCamera == null)
        {
            DebugReason("TargetCamera is null.");
            return;
        }

        if (!hasTargetPosition)
        {
            targetPosition = targetCamera.transform.position;
            hasTargetPosition = true;
        }

        if (!hasBounds)
        {
            RecalculateBounds();
        }

        Vector2 moveDirection = GetMoveInput();

        if (moveDirection == Vector2.zero)
        {
            targetPosition = targetCamera.transform.position;
            smoothVelocity = Vector3.zero;
            return;
        }

        float speed = GetCurrentMoveSpeed();

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        if (Mathf.Abs(moveDirection.x) > 0f && Mathf.Abs(moveDirection.y) > 0f)
        {
            moveDirection *= diagonalSpeedMultiplier;
        }

        Vector3 move = new Vector3(
            moveDirection.x,
            moveDirection.y,
            0f
        ) * speed * Time.deltaTime;

        targetPosition += move;
        targetPosition.z = targetCamera.transform.position.z;

        if (useCameraBoundsClamp)
        {
            targetPosition = ClampCameraPosition(targetPosition);
        }

        if (useSmoothMove)
        {
            targetCamera.transform.position = Vector3.SmoothDamp(
                targetCamera.transform.position,
                targetPosition,
                ref smoothVelocity,
                smoothTime
            );
        }
        else
        {
            targetCamera.transform.position = targetPosition;
        }

        DebugReason(
            "Moving. Direction="
            + moveDirection
            + " Speed="
            + speed
            + " Mouse="
            + (Vector2)Input.mousePosition
            + " Screen="
            + Screen.width
            + "x"
            + Screen.height
        );
    }

    private void LateUpdate()
    {
        if (applyOrthographicSizeEveryFrame)
        {
            ApplyForcedOrthographicSize();
        }
    }

    private void ApplyForcedOrthographicSize()
    {
        if (!forceOrthographicSize)
        {
            return;
        }

        if (targetCamera == null)
        {
            AutoFindReferences();
        }

        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = Mathf.Max(0.1f, desiredOrthographicSize);
    }

    private Vector2 GetMoveInput()
    {
        Vector2 direction = Vector2.zero;

        Vector2 keyboardDirection = GetKeyboardDirection();

        if (enableKeyboardScroll)
        {
            direction += keyboardDirection;
        }

        if (enableEdgeScroll)
        {
            Vector2 edgeDirection = GetEdgeDirection();

            if (edgeDirection != Vector2.zero)
            {
                direction += edgeDirection;
            }
        }

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        return direction;
    }

    private Vector2 GetKeyboardDirection()
    {
        Vector2 direction = Vector2.zero;

        if (Input.GetKey(leftKey))
        {
            direction.x -= 1f;
        }

        if (Input.GetKey(rightKey))
        {
            direction.x += 1f;
        }

        if (Input.GetKey(downKey))
        {
            direction.y -= 1f;
        }

        if (Input.GetKey(upKey))
        {
            direction.y += 1f;
        }

        return direction;
    }

    private Vector2 GetEdgeDirection()
    {
        if (!CanUseEdgeScroll())
        {
            return Vector2.zero;
        }

        Vector2 direction = Vector2.zero;

        if (useScreenEdgeDetection)
        {
            direction += GetScreenEdgeDirection();
        }

        if (useViewportEdgeDetection)
        {
            direction += GetViewportEdgeDirection();
        }

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        if (direction == Vector2.zero)
        {
            DebugReason(
                "Edge scroll ready, but mouse is not inside edge area. Mouse="
                + (Vector2)Input.mousePosition
                + " EdgeSize="
                + edgeSizePixels
                + " Screen="
                + Screen.width
                + "x"
                + Screen.height
            );
        }
        else
        {
            DebugReason(
                "Edge direction detected: "
                + direction
                + " Mouse="
                + (Vector2)Input.mousePosition
            );
        }

        return direction;
    }

    private Vector2 GetScreenEdgeDirection()
    {
        Vector2 direction = Vector2.zero;
        Vector2 mousePosition = Input.mousePosition;

        if (mousePosition.x <= edgeSizePixels)
        {
            direction.x -= 1f;
        }

        if (mousePosition.x >= Screen.width - edgeSizePixels)
        {
            direction.x += 1f;
        }

        if (mousePosition.y <= edgeSizePixels)
        {
            direction.y -= 1f;
        }

        if (mousePosition.y >= Screen.height - edgeSizePixels)
        {
            direction.y += 1f;
        }

        return direction;
    }

    private Vector2 GetViewportEdgeDirection()
    {
        Vector2 direction = Vector2.zero;

        if (targetCamera == null)
        {
            return direction;
        }

        Vector3 viewportPosition = targetCamera.ScreenToViewportPoint(Input.mousePosition);

        if (viewportPosition.x < 0f || viewportPosition.x > 1f)
        {
            return direction;
        }

        if (viewportPosition.y < 0f || viewportPosition.y > 1f)
        {
            return direction;
        }

        float edgeX = Mathf.Clamp01(edgeSizePixels / Mathf.Max(1f, Screen.width));
        float edgeY = Mathf.Clamp01(edgeSizePixels / Mathf.Max(1f, Screen.height));

        if (viewportPosition.x <= edgeX)
        {
            direction.x -= 1f;
        }

        if (viewportPosition.x >= 1f - edgeX)
        {
            direction.x += 1f;
        }

        if (viewportPosition.y <= edgeY)
        {
            direction.y -= 1f;
        }

        if (viewportPosition.y >= 1f - edgeY)
        {
            direction.y += 1f;
        }

        return direction;
    }

    private bool CanUseEdgeScroll()
    {
        Vector2 mousePosition = Input.mousePosition;

        if (blockEdgeScrollUntilMouseMovesAfterStart && !hasMouseMovedAfterStart)
        {
            DebugReason("Blocked edge scroll: mouse has not moved after start.");
            return false;
        }

        if (ignoreZeroMousePosition)
        {
            if (mousePosition.x <= zeroMousePositionTolerance && mousePosition.y <= zeroMousePositionTolerance)
            {
                DebugReason("Blocked edge scroll: zero mouse position.");
                return false;
            }
        }

        if (blockWhenMouseOutsideGameWindow)
        {
            if (mousePosition.x < 0f || mousePosition.x > Screen.width)
            {
                DebugReason("Blocked edge scroll: mouse outside window X. Mouse=" + mousePosition);
                return false;
            }

            if (mousePosition.y < 0f || mousePosition.y > Screen.height)
            {
                DebugReason("Blocked edge scroll: mouse outside window Y. Mouse=" + mousePosition);
                return false;
            }
        }

        if (blockWhenPointerOverUI)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                DebugReason("Blocked edge scroll: pointer over UI.");
                return false;
            }
        }

        if (blockWhenPointerOverGachaCamera)
        {
            if (gachaCamera != null && gachaCamera.pixelRect.Contains(mousePosition))
            {
                DebugReason("Blocked edge scroll: pointer over gacha camera.");
                return false;
            }
        }

        return true;
    }

    private float GetCurrentMoveSpeed()
    {
        if (IsKeyboardInputPressed())
        {
            return keyboardScrollSpeed;
        }

        return edgeScrollSpeed;
    }

    private bool IsKeyboardInputPressed()
    {
        return Input.GetKey(leftKey)
            || Input.GetKey(rightKey)
            || Input.GetKey(upKey)
            || Input.GetKey(downKey);
    }

    private void UpdateMouseMovedFlag()
    {
        if (hasMouseMovedAfterStart)
        {
            return;
        }

        Vector2 currentMousePosition = Input.mousePosition;

        if (Vector2.Distance(startMousePosition, currentMousePosition) >= mouseMoveDetectDistance)
        {
            hasMouseMovedAfterStart = true;
        }
    }

    [ContextMenu("Recalculate Bounds")]
    public void RecalculateBounds()
    {
        AutoFindReferences();

        if (calculateBoundsFromDungeonTiles && dungeonGridManager != null)
        {
            if (TryCalculateBoundsFromTiles(out Vector2 min, out Vector2 max))
            {
                worldMinBounds = min - boundsPadding;
                worldMaxBounds = max + boundsPadding;

                if (expandBoundsIfTooSmall)
                {
                    ExpandBoundsIfTooSmall();
                }

                hasBounds = true;
                DebugReason("Bounds from tiles. Min=" + worldMinBounds + " Max=" + worldMaxBounds);
                return;
            }
        }

        worldMinBounds = manualMinBounds;
        worldMaxBounds = manualMaxBounds;

        if (expandBoundsIfTooSmall)
        {
            ExpandBoundsIfTooSmall();
        }

        hasBounds = true;
        DebugReason("Manual bounds. Min=" + worldMinBounds + " Max=" + worldMaxBounds);
    }

    private void ExpandBoundsIfTooSmall()
    {
        Vector2 center = (worldMinBounds + worldMaxBounds) * 0.5f;

        float width = worldMaxBounds.x - worldMinBounds.x;
        float height = worldMaxBounds.y - worldMinBounds.y;

        float targetWidth = Mathf.Max(width, minimumScrollableWorldSize.x);
        float targetHeight = Mathf.Max(height, minimumScrollableWorldSize.y);

        worldMinBounds = new Vector2(
            center.x - targetWidth * 0.5f,
            center.y - targetHeight * 0.5f
        );

        worldMaxBounds = new Vector2(
            center.x + targetWidth * 0.5f,
            center.y + targetHeight * 0.5f
        );
    }

    private Vector3 ClampCameraPosition(Vector3 position)
    {
        if (targetCamera == null)
        {
            return position;
        }

        if (!hasBounds)
        {
            return position;
        }

        if (!targetCamera.orthographic)
        {
            return position;
        }

        float cameraHalfHeight = targetCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * targetCamera.aspect;

        float minX = worldMinBounds.x + cameraHalfWidth;
        float maxX = worldMaxBounds.x - cameraHalfWidth;
        float minY = worldMinBounds.y + cameraHalfHeight;
        float maxY = worldMaxBounds.y - cameraHalfHeight;

        if (minX > maxX)
        {
            float centerX = (worldMinBounds.x + worldMaxBounds.x) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = (worldMinBounds.y + worldMaxBounds.y) * 0.5f;
            minY = centerY;
            maxY = centerY;
        }

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    private bool TryCalculateBoundsFromTiles(out Vector2 min, out Vector2 max)
    {
        min = Vector2.zero;
        max = Vector2.zero;

        if (dungeonGridManager == null)
        {
            return false;
        }

        DungeonTile[] tiles = dungeonGridManager.GetComponentsInChildren<DungeonTile>(true);

        if (tiles == null || tiles.Length <= 0)
        {
            return false;
        }

        bool hasAnyTile = false;

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < tiles.Length; i++)
        {
            DungeonTile tile = tiles[i];

            if (tile == null)
            {
                continue;
            }

            Vector3 position = tile.transform.position;

            minX = Mathf.Min(minX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxX = Mathf.Max(maxX, position.x);
            maxY = Mathf.Max(maxY, position.y);

            hasAnyTile = true;
        }

        if (!hasAnyTile)
        {
            return false;
        }

        min = new Vector2(minX, minY);
        max = new Vector2(maxX, maxY);

        return true;
    }

    private void AutoFindReferences()
    {
        if (autoUseThisCamera && targetCamera == null)
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

        if (autoFindGachaCamera && gachaCamera == null)
        {
            AutoFindGachaCamera();
        }
    }

    private void AutoFindGachaCamera()
    {
        if (!string.IsNullOrEmpty(gachaCameraName))
        {
            GameObject cameraObject = GameObject.Find(gachaCameraName);

            if (cameraObject != null)
            {
                Camera foundCamera = cameraObject.GetComponent<Camera>();

                if (foundCamera != null)
                {
                    gachaCamera = foundCamera;
                    return;
                }
            }
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];

            if (camera == null)
            {
                continue;
            }

            string lowerName = camera.name.ToLower();

            if (lowerName.Contains("gacha"))
            {
                gachaCamera = camera;
                return;
            }
        }
    }

    private void DebugReason(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        if (Time.time < nextDebugLogTime)
        {
            return;
        }

        nextDebugLogTime = Time.time + debugLogInterval;
        Debug.Log("EdgeScrollCameraController: " + message);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawBoundsGizmo)
        {
            return;
        }

        Vector2 min = hasBounds ? worldMinBounds : manualMinBounds;
        Vector2 max = hasBounds ? worldMaxBounds : manualMaxBounds;

        Vector3 center = new Vector3(
            (min.x + max.x) * 0.5f,
            (min.y + max.y) * 0.5f,
            0f
        );

        Vector3 size = new Vector3(
            Mathf.Abs(max.x - min.x),
            Mathf.Abs(max.y - min.y),
            0.05f
        );

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.95f);
        Gizmos.DrawWireCube(center, size);
    }
}