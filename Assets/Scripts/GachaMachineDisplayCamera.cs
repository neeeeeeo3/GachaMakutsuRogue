using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class GachaMachineDisplayCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Viewport")]
    public Rect viewportRect = new Rect(0.80f, 0.03f, 0.18f, 0.27f);

    [Header("Camera")]
    public float orthographicSize = 1.45f;
    public Vector2 targetOffset = Vector2.zero;
    public float cameraZ = -10f;
    public int cameraDepth = 20;

    [Header("Layer")]
    public string renderLayerName = "GachaMachine";

    [Header("Background")]
    public Color backgroundColor = new Color(0.03f, 0.03f, 0.04f, 1f);

    [Header("Update")]
    public bool applyEveryFrame = true;

    private Camera targetCamera;

    private void Awake()
    {
        Apply();
    }

    private void Start()
    {
        Apply();
    }

    private void Update()
    {
        if (applyEveryFrame)
        {
            Apply();
        }
    }

    private void OnValidate()
    {
        Apply();
    }

    public void Apply()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = orthographicSize;
        targetCamera.rect = viewportRect;
        targetCamera.depth = cameraDepth;
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = backgroundColor;

        int layerIndex = LayerMask.NameToLayer(renderLayerName);

        if (layerIndex >= 0)
        {
            targetCamera.cullingMask = 1 << layerIndex;
        }

        if (target != null)
        {
            Vector3 targetPosition = target.position;

            transform.position = new Vector3(
                targetPosition.x + targetOffset.x,
                targetPosition.y + targetOffset.y,
                cameraZ
            );
        }
    }
}