using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class RemoveModeManager : MonoBehaviour
{
    public static RemoveModeManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text removeModeText;
    public TMP_Text removeModeButtonText;

    [Header("References")]
    public GachaManager gachaManager;
    public bool autoFindGachaManager = true;

    [Header("Buttons To Hide While Removing")]
    public GameObject[] buttonsToHideWhileRemoving;

    public bool IsRemoveModeActive { get; private set; }

    private bool[] storedButtonActiveStates;
    private bool hasStoredButtonStates;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (autoFindGachaManager && gachaManager == null)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }

        UpdateUi();
    }

    private void Update()
    {
        if (!IsRemoveModeActive)
        {
            return;
        }

        if (!CanStayInRemoveMode())
        {
            ExitRemoveMode();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryRemoveAtMousePosition();
        }
    }

    public void ToggleRemoveMode()
    {
        if (IsRemoveModeActive)
        {
            ExitRemoveMode();
        }
        else
        {
            EnterRemoveMode();
        }
    }

    public void EnterRemoveMode()
    {
        if (!CanEnterRemoveMode())
        {
            return;
        }

        IsRemoveModeActive = true;

        HideButtonsWhileRemoving();
        UpdateUi();

        Debug.Log("Remove mode started.");
    }

    public void ExitRemoveMode()
    {
        IsRemoveModeActive = false;

        RestoreButtonsAfterRemoving();
        UpdateUi();

        Debug.Log("Remove mode ended.");
    }

    private bool CanEnterRemoveMode()
    {
        if (RunManager.Instance == null)
        {
            Debug.LogWarning("RunManager not found.");
            return false;
        }

        if (!RunManager.Instance.IsDungeonBuildPhase())
        {
            Debug.Log("Remove mode is only available during Dungeon Build Phase.");
            return false;
        }

        if (gachaManager == null && autoFindGachaManager)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }

        if (gachaManager != null && gachaManager.HasPendingCapsule())
        {
            Debug.Log("Cannot enter remove mode while placing capsule.");
            return false;
        }

        return true;
    }

    private bool CanStayInRemoveMode()
    {
        if (RunManager.Instance == null)
        {
            return false;
        }

        if (!RunManager.Instance.IsDungeonBuildPhase())
        {
            return false;
        }

        if (gachaManager == null && autoFindGachaManager)
        {
            gachaManager = FindFirstObjectByType<GachaManager>();
        }

        if (gachaManager != null && gachaManager.HasPendingCapsule())
        {
            return false;
        }

        return true;
    }

    private void TryRemoveAtMousePosition()
    {
        if (IsPointerOverUI())
        {
            return;
        }

        DungeonGridManager grid = DungeonGridManager.Instance;

        if (grid == null)
        {
            grid = FindFirstObjectByType<DungeonGridManager>();
        }

        if (grid == null)
        {
            Debug.LogWarning("DungeonGridManager not found.");
            return;
        }

        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        Vector3 snappedPosition = grid.SnapWorldPositionToTileCenter(mouseWorldPosition);

        PlaceableObject placeableObject = FindPlaceableObjectOnTile(snappedPosition, grid);

        if (placeableObject == null)
        {
            Debug.Log("No removable object on this tile.");
            return;
        }

        Debug.Log("Removed: " + placeableObject.objectName);

        Destroy(placeableObject.gameObject);
    }

    private PlaceableObject FindPlaceableObjectOnTile(Vector3 tileWorldPosition, DungeonGridManager grid)
    {
        if (!grid.TryGetGridPositionFromWorldPosition(tileWorldPosition, out int targetX, out int targetY))
        {
            return null;
        }

        PlaceableObject[] placeableObjects = FindObjectsByType<PlaceableObject>(FindObjectsSortMode.None);

        foreach (PlaceableObject placeableObject in placeableObjects)
        {
            if (placeableObject == null)
            {
                continue;
            }

            if (!placeableObject.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!placeableObject.countsAsOccupied)
            {
                continue;
            }

            if (!grid.TryGetGridPositionFromWorldPosition(placeableObject.transform.position, out int objectX, out int objectY))
            {
                continue;
            }

            if (objectX == targetX && objectY == targetY)
            {
                return placeableObject;
            }
        }

        return null;
    }

    private void HideButtonsWhileRemoving()
    {
        if (hasStoredButtonStates)
        {
            return;
        }

        if (buttonsToHideWhileRemoving == null)
        {
            return;
        }

        storedButtonActiveStates = new bool[buttonsToHideWhileRemoving.Length];

        for (int i = 0; i < buttonsToHideWhileRemoving.Length; i++)
        {
            GameObject buttonObject = buttonsToHideWhileRemoving[i];

            if (buttonObject == null)
            {
                continue;
            }

            storedButtonActiveStates[i] = buttonObject.activeSelf;
            buttonObject.SetActive(false);
        }

        hasStoredButtonStates = true;
    }

    private void RestoreButtonsAfterRemoving()
    {
        if (!hasStoredButtonStates)
        {
            return;
        }

        if (buttonsToHideWhileRemoving == null || storedButtonActiveStates == null)
        {
            hasStoredButtonStates = false;
            return;
        }

        int count = Mathf.Min(buttonsToHideWhileRemoving.Length, storedButtonActiveStates.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject buttonObject = buttonsToHideWhileRemoving[i];

            if (buttonObject == null)
            {
                continue;
            }

            buttonObject.SetActive(storedButtonActiveStates[i]);
        }

        hasStoredButtonStates = false;
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject();
    }

    private void UpdateUi()
    {
        if (removeModeText != null)
        {
            if (IsRemoveModeActive)
            {
                removeModeText.text = "REMOVE MODE\nCLICK ITEM TO REMOVE";
            }
            else
            {
                removeModeText.text = "";
            }
        }

        if (removeModeButtonText != null)
        {
            if (IsRemoveModeActive)
            {
                removeModeButtonText.text = "EXIT REMOVE";
            }
            else
            {
                removeModeButtonText.text = "REMOVE";
            }
        }
    }
}