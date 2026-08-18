using UnityEngine;
using UnityEngine.EventSystems;

public class GachaLeverInput : MonoBehaviour
{
    [Header("References")]
    public GachaManager gachaManager;
    public GachaMachineVisualBuilder visualBuilder;
    public Camera gachaCamera;

    [Header("Auto Find")]
    public bool autoFindGachaManager = true;
    public bool autoFindVisualBuilder = true;
    public bool autoFindGachaCamera = true;
    public string gachaCameraName = "GachaMachineCamera";

    [Header("Click Area")]
    public bool createClickAreaIfMissing = true;
    public string clickAreaName = "GachaLeverClickArea";
    public Collider2D clickAreaCollider;

    [Tooltip("HandlePivotが見つからない時のレバー判定位置です。GachaMachineRoot基準のローカル位置。")]
    public Vector3 fallbackClickAreaLocalPosition = new Vector3(0.28f, -0.34f, 0f);

    public Vector2 clickAreaSize = new Vector2(0.90f, 0.80f);

    [Tooltip("ON推奨。生成されたHandlePivotにクリック判定を追従させます。")]
    public bool followGeneratedHandlePivot = true;

    [Header("Input")]
    public bool onlyAcceptClicksInsideGachaCameraView = true;

    [Tooltip("右下のガチャ表示がUI上にある場合、ONだとクリックがブロックされることがあります。まずはOFF推奨。")]
    public bool blockWhenPointerOverUI = false;

    public int mouseButton = 0;

    [Header("Old UI Button")]
    public GameObject gachaButtonObjectToHide;
    public bool hideOldGachaButtonOnStart = true;

    [Header("Safety")]
    public bool ignoreClickWhilePendingCapsule = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Transform clickAreaTransform;
    private bool clickAreaCreated;

    private void Start()
    {
        AutoFindReferences();

        if (hideOldGachaButtonOnStart && gachaButtonObjectToHide != null)
        {
            gachaButtonObjectToHide.SetActive(false);
        }

        EnsureClickArea();
        UpdateClickAreaPositionFromHandle();
    }

    private void Update()
    {
        AutoFindReferences();
        EnsureClickArea();

        if (followGeneratedHandlePivot)
        {
            UpdateClickAreaPositionFromHandle();
        }

        if (!Input.GetMouseButtonDown(mouseButton))
        {
            return;
        }

        TryClickLever();
    }

    private void TryClickLever()
    {
        if (gachaManager == null)
        {
            DebugLog("GachaManager not found.");
            return;
        }

        if (ignoreClickWhilePendingCapsule && gachaManager.HasPendingCapsule())
        {
            DebugLog("Click ignored. Pending capsule or ejecting.");
            return;
        }

        if (blockWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            DebugLog("Click ignored. Pointer over UI.");
            return;
        }

        Camera inputCamera = GetInputCamera();

        if (inputCamera == null)
        {
            DebugLog("Input camera not found.");
            return;
        }

        Vector2 mousePosition = Input.mousePosition;

        if (onlyAcceptClicksInsideGachaCameraView && !inputCamera.pixelRect.Contains(mousePosition))
        {
            DebugLog("Click ignored. Outside gacha camera view.");
            return;
        }

        if (clickAreaCollider == null)
        {
            DebugLog("Click area collider not found.");
            return;
        }

        Vector3 worldPosition = inputCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPosition.z = 0f;

        if (!clickAreaCollider.OverlapPoint(worldPosition))
        {
            DebugLog("Click ignored. Not on lever area. MouseWorld=" + worldPosition);
            return;
        }

        DebugLog("Lever clicked. Roll start.");

        gachaManager.Roll();
    }

    private void EnsureClickArea()
    {
        if (clickAreaCollider != null)
        {
            clickAreaTransform = clickAreaCollider.transform;
            return;
        }

        Transform existingClickArea = transform.Find(clickAreaName);

        if (existingClickArea != null)
        {
            clickAreaTransform = existingClickArea;

            clickAreaCollider = existingClickArea.GetComponent<Collider2D>();

            if (clickAreaCollider == null)
            {
                BoxCollider2D boxCollider = existingClickArea.gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = clickAreaSize;
                clickAreaCollider = boxCollider;
            }

            return;
        }

        if (!createClickAreaIfMissing)
        {
            return;
        }

        GameObject clickAreaObject = new GameObject(clickAreaName);
        clickAreaObject.transform.SetParent(transform);
        clickAreaObject.transform.localPosition = fallbackClickAreaLocalPosition;
        clickAreaObject.transform.localRotation = Quaternion.identity;
        clickAreaObject.transform.localScale = Vector3.one;
        clickAreaObject.layer = gameObject.layer;

        BoxCollider2D createdCollider = clickAreaObject.AddComponent<BoxCollider2D>();
        createdCollider.isTrigger = true;
        createdCollider.size = clickAreaSize;

        clickAreaTransform = clickAreaObject.transform;
        clickAreaCollider = createdCollider;
        clickAreaCreated = true;

        DebugLog("Created lever click area.");
    }

    private void UpdateClickAreaPositionFromHandle()
    {
        if (clickAreaTransform == null)
        {
            return;
        }

        if (visualBuilder == null)
        {
            return;
        }

        if (visualBuilder.generatedHandlePivot == null)
        {
            return;
        }

        clickAreaTransform.position = visualBuilder.generatedHandlePivot.position;
        clickAreaTransform.rotation = Quaternion.identity;
    }

    private Camera GetInputCamera()
    {
        if (gachaCamera != null)
        {
            return gachaCamera;
        }

        if (autoFindGachaCamera)
        {
            AutoFindGachaCamera();
        }

        if (gachaCamera != null)
        {
            return gachaCamera;
        }

        return Camera.main;
    }

    private void AutoFindReferences()
    {
        if (autoFindGachaManager && gachaManager == null)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }

        if (autoFindVisualBuilder && visualBuilder == null)
        {
            visualBuilder = GetComponent<GachaMachineVisualBuilder>();

            if (visualBuilder == null)
            {
                visualBuilder = GetComponentInChildren<GachaMachineVisualBuilder>();
            }

            if (visualBuilder == null)
            {
                visualBuilder = FindFirstObjectByType<GachaMachineVisualBuilder>();
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

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("GachaLeverInput: " + message);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.35f);

        Vector3 centerPosition = transform.TransformPoint(fallbackClickAreaLocalPosition);
        Vector3 size = new Vector3(clickAreaSize.x, clickAreaSize.y, 0.05f);

        if (clickAreaCollider != null)
        {
            centerPosition = clickAreaCollider.bounds.center;
            size = clickAreaCollider.bounds.size;
        }

        Gizmos.DrawCube(centerPosition, size);
    }
}