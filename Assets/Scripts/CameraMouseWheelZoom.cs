using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(9000)]
public class CameraMouseWheelZoom : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public bool autoFindCamera = true;

    [Header("Zoom")]
    public bool enableZoom = true;

    [Tooltip("小さいほどズームイン。")]
    public float minOrthographicSize = 3f;

    [Tooltip("大きいほどズームアウト。")]
    public float maxOrthographicSize = 9f;

    [Tooltip("ホイール1段あたりのズーム量です。")]
    public float zoomSpeed = 1.2f;

    [Tooltip("ONにするとズームがなめらかになります。")]
    public bool useSmoothZoom = true;

    [Tooltip("小さいほどキビキビ、大きいほどぬるっとします。")]
    public float smoothTime = 0.12f;

    [Header("Mouse Focus")]
    [Tooltip("ON推奨。マウス位置を中心にズームするので操作感が良くなります。")]
    public bool zoomTowardMousePosition = true;

    [Tooltip("UIの上ではズームしないようにします。")]
    public bool blockZoomWhenPointerOverUI = true;

    [Header("Direction")]
    [Tooltip("ONにするとホイール方向を反転します。")]
    public bool invertScrollDirection = false;

    [Header("Debug")]
    public bool showDebugLog = false;

    private float targetOrthographicSize;
    private float zoomVelocity;

    private void Start()
    {
        AutoFindReferences();

        if (targetCamera != null)
        {
            targetOrthographicSize = targetCamera.orthographicSize;
        }
    }

    private void Update()
    {
        if (!enableZoom)
        {
            return;
        }

        AutoFindReferences();

        if (targetCamera == null)
        {
            return;
        }

        if (!targetCamera.orthographic)
        {
            return;
        }

        if (blockZoomWhenPointerOverUI
            && EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) <= 0.001f)
        {
            return;
        }

        if (invertScrollDirection)
        {
            scroll *= -1f;
        }

        Vector3 mouseWorldBeforeZoom = GetMouseWorldPosition();

        targetOrthographicSize -= scroll * zoomSpeed;
        targetOrthographicSize = Mathf.Clamp(
            targetOrthographicSize,
            minOrthographicSize,
            maxOrthographicSize
        );

        if (!useSmoothZoom)
        {
            ApplyZoomInstant(mouseWorldBeforeZoom);
        }

        DebugLog("Target Zoom Size: " + targetOrthographicSize);
    }

    private void LateUpdate()
    {
        if (!enableZoom)
        {
            return;
        }

        AutoFindReferences();

        if (targetCamera == null || !targetCamera.orthographic)
        {
            return;
        }

        if (!useSmoothZoom)
        {
            return;
        }

        if (Mathf.Abs(targetCamera.orthographicSize - targetOrthographicSize) <= 0.001f)
        {
            targetCamera.orthographicSize = targetOrthographicSize;
            return;
        }

        Vector3 mouseWorldBeforeZoom = GetMouseWorldPosition();

        targetCamera.orthographicSize = Mathf.SmoothDamp(
            targetCamera.orthographicSize,
            targetOrthographicSize,
            ref zoomVelocity,
            Mathf.Max(0.01f, smoothTime)
        );

        if (zoomTowardMousePosition)
        {
            KeepMouseWorldPositionStable(mouseWorldBeforeZoom);
        }
    }

    public void SetZoomSize(float newSize)
    {
        AutoFindReferences();

        targetOrthographicSize = Mathf.Clamp(
            newSize,
            minOrthographicSize,
            maxOrthographicSize
        );

        if (targetCamera != null)
        {
            targetCamera.orthographicSize = targetOrthographicSize;
        }
    }

    public void ResetZoomToMiddle()
    {
        float middleSize = (minOrthographicSize + maxOrthographicSize) * 0.5f;
        SetZoomSize(middleSize);
    }

    private void ApplyZoomInstant(Vector3 mouseWorldBeforeZoom)
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographicSize = targetOrthographicSize;

        if (zoomTowardMousePosition)
        {
            KeepMouseWorldPositionStable(mouseWorldBeforeZoom);
        }
    }

    private void KeepMouseWorldPositionStable(Vector3 mouseWorldBeforeZoom)
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 mouseWorldAfterZoom = GetMouseWorldPosition();
        Vector3 difference = mouseWorldBeforeZoom - mouseWorldAfterZoom;

        targetCamera.transform.position += new Vector3(
            difference.x,
            difference.y,
            0f
        );
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (targetCamera == null)
        {
            return Vector3.zero;
        }

        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(targetCamera.transform.position.z);

        Vector3 worldPosition = targetCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;

        return worldPosition;
    }

    private void AutoFindReferences()
    {
        if (!autoFindCamera || targetCamera != null)
        {
            return;
        }

        targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null && targetOrthographicSize <= 0f)
        {
            targetOrthographicSize = targetCamera.orthographicSize;
        }
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("CameraMouseWheelZoom: " + message);
    }
}