using UnityEngine;
using UnityEngine.EventSystems;

public class DragDigManager : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public GachaManager gachaManager;
    public bool autoFindReferences = true;

    [Header("Input")]
    public int digMouseButton = 0;

    private DungeonTile lastDugTile;
    private bool dragStartedOverUi;

    private void Start()
    {
        AutoFindReferences();
    }

    private void Update()
    {
        AutoFindReferences();

        if (Input.GetMouseButtonDown(digMouseButton))
        {
            dragStartedOverUi = IsPointerOverUI();
            lastDugTile = null;
        }

        if (Input.GetMouseButtonUp(digMouseButton))
        {
            dragStartedOverUi = false;
            lastDugTile = null;
        }

        if (!Input.GetMouseButton(digMouseButton))
        {
            return;
        }

        if (dragStartedOverUi)
        {
            return;
        }

        if (!CanDragDigNow())
        {
            return;
        }

        TryDigTileUnderMouse();
    }

    private bool CanDragDigNow()
    {
        if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
        {
            return false;
        }

        if (RemoveModeManager.Instance != null && RemoveModeManager.Instance.IsRemoveModeActive)
        {
            return false;
        }

        if (CorePlacementManager.Instance != null && CorePlacementManager.Instance.IsCorePlacementModeActive)
        {
            return false;
        }

        if (gachaManager != null && gachaManager.HasPendingCapsule())
        {
            return false;
        }

        if (IsPointerOverUI())
        {
            return false;
        }

        return true;
    }

    private void TryDigTileUnderMouse()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition = targetCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        DungeonTile tile = FindDungeonTileAtPosition(mouseWorldPosition);

        if (tile == null)
        {
            return;
        }

        if (tile == lastDugTile)
        {
            return;
        }

        lastDugTile = tile;
        tile.TryDig();
    }

    private DungeonTile FindDungeonTileAtPosition(Vector3 worldPosition)
    {
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);

        foreach (Collider2D collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            DungeonTile tile = collider.GetComponent<DungeonTile>();

            if (tile != null)
            {
                return tile;
            }
        }

        return null;
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