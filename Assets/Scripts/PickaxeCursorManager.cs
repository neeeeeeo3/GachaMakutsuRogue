using UnityEngine;
using UnityEngine.EventSystems;

public class PickaxeCursorManager : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public GachaManager gachaManager;
    public bool autoFindReferences = true;

    [Header("Visibility")]
    public bool showOnlyDuringDungeonBuildPhase = true;
    public bool hideOverUi = true;
    public bool hideWhilePlacingCapsule = true;
    public bool hideDuringCorePlacementMode = true;
    public bool hideDuringRemoveMode = true;
    public bool hideOutsideTargetCameraViewport = true;

    [Header("Cursor Position")]
    public Vector2 cursorOffset = Vector2.zero;
    public float cursorWorldZ = 0f;

    [Header("Cursor Hotspot")]
    public bool anchorTipToMousePosition = true;
    public Vector2 tipLocalPosition = new Vector2(0.43f, 0.11f);

    [Header("Look")]
    public float visualScale = 0.75f;
    public int sortingOrder = 1000;
    public float baseRotationZ = -35f;

    public Color handleColor = new Color(0.48f, 0.28f, 0.12f, 1f);
    public Color metalColor = new Color(0.75f, 0.78f, 0.82f, 1f);

    [Header("Normal Swing")]
    public float swingDuration = 0.18f;
    public float swingAngle = 65f;
    public float hitPunchScale = 1.12f;

    [Header("Hard Soil Swing")]
    public int hardSoilSwingCount = 2;
    public float hardSwingDuration = 0.14f;
    public float hardSwingAngle = 78f;
    public float hardHitPunchScale = 1.18f;

    private GameObject visualRoot;
    private Sprite squareSprite;

    private float swingTimer;
    private int pendingSwingCount;

    private DungeonTile.SoilType activeSwingSoilType = DungeonTile.SoilType.Normal;
    private DungeonTile.SoilType queuedSwingSoilType = DungeonTile.SoilType.Normal;

    private void OnEnable()
    {
        DungeonTile.OnAnyTileDigStartedWithSoilType += HandleTileDigStarted;
    }

    private void OnDisable()
    {
        DungeonTile.OnAnyTileDigStartedWithSoilType -= HandleTileDigStarted;
        Cursor.visible = true;
    }

    private void Start()
    {
        AutoFindReferences();
        CreateVisual();
        SetVisualVisible(false);
    }

    private void Update()
    {
        AutoFindReferences();

        bool shouldShow = ShouldShowPickaxeCursor();

        SetVisualVisible(shouldShow);
        Cursor.visible = !shouldShow;

        if (!shouldShow)
        {
            return;
        }

        TryStartNextSwing();
        UpdateSwingAnimation();
        UpdateCursorPosition();
    }

    private void HandleTileDigStarted(Vector3 dugWorldPosition, DungeonTile.SoilType dugSoilType)
    {
        if (!ShouldShowPickaxeCursor())
        {
            return;
        }

        int swingCount = 1;

        if (dugSoilType == DungeonTile.SoilType.Hard)
        {
            swingCount = Mathf.Max(1, hardSoilSwingCount);
        }

        pendingSwingCount += swingCount;

        if (dugSoilType == DungeonTile.SoilType.Hard)
        {
            queuedSwingSoilType = DungeonTile.SoilType.Hard;
        }
        else if (queuedSwingSoilType != DungeonTile.SoilType.Hard)
        {
            queuedSwingSoilType = dugSoilType;
        }

        TryStartNextSwing();
    }

    private void TryStartNextSwing()
    {
        if (swingTimer > 0f)
        {
            return;
        }

        if (pendingSwingCount <= 0)
        {
            return;
        }

        activeSwingSoilType = queuedSwingSoilType;
        pendingSwingCount--;

        if (pendingSwingCount <= 0)
        {
            queuedSwingSoilType = DungeonTile.SoilType.Normal;
        }

        swingTimer = GetCurrentSwingDuration();
    }

    private float GetCurrentSwingDuration()
    {
        if (activeSwingSoilType == DungeonTile.SoilType.Hard)
        {
            return hardSwingDuration;
        }

        return swingDuration;
    }

    private float GetCurrentSwingAngle()
    {
        if (activeSwingSoilType == DungeonTile.SoilType.Hard)
        {
            return hardSwingAngle;
        }

        return swingAngle;
    }

    private float GetCurrentPunchScale()
    {
        if (activeSwingSoilType == DungeonTile.SoilType.Hard)
        {
            return hardHitPunchScale;
        }

        return hitPunchScale;
    }

    private bool ShouldShowPickaxeCursor()
    {
        if (targetCamera == null)
        {
            return false;
        }

        if (hideOutsideTargetCameraViewport)
        {
            Vector2 mousePosition = Input.mousePosition;

            if (!targetCamera.pixelRect.Contains(mousePosition))
            {
                return false;
            }
        }

        if (hideOverUi && IsPointerOverUI())
        {
            return false;
        }

        if (showOnlyDuringDungeonBuildPhase)
        {
            if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
            {
                return false;
            }
        }

        if (hideDuringCorePlacementMode)
        {
            if (CorePlacementManager.Instance != null && CorePlacementManager.Instance.IsCorePlacementModeActive)
            {
                return false;
            }
        }

        if (hideDuringRemoveMode)
        {
            if (RemoveModeManager.Instance != null && RemoveModeManager.Instance.IsRemoveModeActive)
            {
                return false;
            }
        }

        if (hideWhilePlacingCapsule)
        {
            if (gachaManager != null && gachaManager.HasPendingCapsule())
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateCursorPosition()
    {
        if (visualRoot == null || targetCamera == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(targetCamera.transform.position.z - cursorWorldZ);

        Vector3 mouseWorldPosition = targetCamera.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = cursorWorldZ;

        Vector3 targetWorldPosition = new Vector3(
            mouseWorldPosition.x + cursorOffset.x,
            mouseWorldPosition.y + cursorOffset.y,
            cursorWorldZ
        );

        if (anchorTipToMousePosition)
        {
            Vector3 tipLocal = new Vector3(tipLocalPosition.x, tipLocalPosition.y, 0f);
            Vector3 tipWorldOffset = visualRoot.transform.TransformVector(tipLocal);

            visualRoot.transform.position = targetWorldPosition - tipWorldOffset;
        }
        else
        {
            visualRoot.transform.position = targetWorldPosition;
        }
    }

    private void UpdateSwingAnimation()
    {
        if (visualRoot == null)
        {
            return;
        }

        float rotationZ = baseRotationZ;
        float scale = visualScale;

        if (swingTimer > 0f)
        {
            float duration = Mathf.Max(0.01f, GetCurrentSwingDuration());

            swingTimer -= Time.deltaTime;

            float progress = 1f - Mathf.Clamp01(swingTimer / duration);
            float swingWave = Mathf.Sin(progress * Mathf.PI);

            rotationZ = baseRotationZ - swingWave * GetCurrentSwingAngle();
            scale = Mathf.Lerp(visualScale, visualScale * GetCurrentPunchScale(), swingWave);

            if (swingTimer <= 0f)
            {
                swingTimer = 0f;
                TryStartNextSwing();
            }
        }

        visualRoot.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        visualRoot.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void CreateVisual()
    {
        if (visualRoot != null)
        {
            return;
        }

        squareSprite = CreateSquareSprite();

        visualRoot = new GameObject("PickaxeCursorVisual");
        visualRoot.transform.SetParent(transform);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.Euler(0f, 0f, baseRotationZ);
        visualRoot.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        CreatePart(
            "Handle",
            new Vector3(0f, -0.22f, 0f),
            new Vector3(0.08f, 0.72f, 1f),
            0f,
            handleColor
        );

        CreatePart(
            "MetalHead",
            new Vector3(0f, 0.15f, 0f),
            new Vector3(0.62f, 0.09f, 1f),
            0f,
            metalColor
        );

        CreatePart(
            "MetalTipLeft",
            new Vector3(-0.30f, 0.11f, 0f),
            new Vector3(0.25f, 0.07f, 1f),
            -28f,
            metalColor
        );

        CreatePart(
            "MetalTipRight",
            new Vector3(0.30f, 0.11f, 0f),
            new Vector3(0.25f, 0.07f, 1f),
            28f,
            metalColor
        );
    }

    private void CreatePart(string partName, Vector3 localPosition, Vector3 localScale, float localRotationZ, Color color)
    {
        GameObject partObject = new GameObject(partName);
        partObject.transform.SetParent(visualRoot.transform);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
        partObject.transform.localScale = localScale;

        SpriteRenderer spriteRenderer = partObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "PickaxeCursorSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private void SetVisualVisible(bool visible)
    {
        if (visualRoot == null)
        {
            return;
        }

        if (visualRoot.activeSelf != visible)
        {
            visualRoot.SetActive(visible);
        }
    }

    private void AutoFindReferences()
    {
        if (!autoFindReferences)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (gachaManager == null)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }
}