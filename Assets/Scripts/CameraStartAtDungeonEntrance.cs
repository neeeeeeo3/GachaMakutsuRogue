using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(9500)]
public class CameraStartAtDungeonEntrance : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public bool autoFindCamera = true;

    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Start Position")]
    public bool moveOnStart = true;

    [Tooltip("入口を画面中央から少しずらしたい時に使います。Yをマイナスにすると、入口が画面上寄りになります。")]
    public Vector2 cameraOffsetFromEntrance = Vector2.zero;

    [Tooltip("DungeonGridManagerの生成を待つためのフレーム数です。通常は1でOK。")]
    public int waitFramesBeforeMove = 1;

    [Header("Bounds")]
    [Tooltip("ON推奨。移動後に背景範囲制限スクリプトでカメラ位置を補正します。")]
    public bool clampAfterMove = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private void Start()
    {
        if (!moveOnStart)
        {
            return;
        }

        StartCoroutine(MoveToEntranceAtStartRoutine());
    }

    [ContextMenu("Move Camera To Entrance Now")]
    public void MoveCameraToEntranceNow()
    {
        AutoFindReferences();

        if (targetCamera == null)
        {
            Debug.LogWarning("CameraStartAtDungeonEntrance: Target Camera not found.");
            return;
        }

        if (dungeonGridManager == null)
        {
            Debug.LogWarning("CameraStartAtDungeonEntrance: DungeonGridManager not found.");
            return;
        }

        Vector3 entrancePosition = dungeonGridManager.GetEntranceWorldPosition();

        Vector3 cameraPosition = targetCamera.transform.position;
        cameraPosition.x = entrancePosition.x + cameraOffsetFromEntrance.x;
        cameraPosition.y = entrancePosition.y + cameraOffsetFromEntrance.y;

        targetCamera.transform.position = cameraPosition;

        if (clampAfterMove)
        {
            CameraBackgroundBoundsLimiter limiter =
                targetCamera.GetComponent<CameraBackgroundBoundsLimiter>();

            if (limiter != null)
            {
                limiter.ClampCameraNow();
            }
        }

        DebugLog("Moved camera to entrance: " + entrancePosition);
    }

    private IEnumerator MoveToEntranceAtStartRoutine()
    {
        int safeWaitFrames = Mathf.Max(0, waitFramesBeforeMove);

        for (int i = 0; i < safeWaitFrames; i++)
        {
            yield return null;
        }

        MoveCameraToEntranceNow();
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
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("CameraStartAtDungeonEntrance: " + message);
    }
}