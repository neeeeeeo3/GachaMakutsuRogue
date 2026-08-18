using UnityEngine;
using UnityEngine.UI;

public class UIRaycastCleaner : MonoBehaviour
{
    [Header("Settings")]
    public bool cleanOnAwake = true;
    public bool cleanOnStart = true;
    public bool keepButtonImagesClickable = true;
    public bool showLog = true;

    private void Awake()
    {
        if (cleanOnAwake)
        {
            CleanRaycastTargets();
        }
    }

    private void Start()
    {
        if (cleanOnStart)
        {
            CleanRaycastTargets();
        }
    }

    [ContextMenu("Clean UI Raycast Targets")]
    public void CleanRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        int disabledCount = 0;
        int keptCount = 0;

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
            {
                continue;
            }

            bool shouldKeepClickable = false;

            if (keepButtonImagesClickable)
            {
                Selectable selectable = graphic.GetComponent<Selectable>();

                if (selectable != null)
                {
                    shouldKeepClickable = true;
                }
            }

            if (shouldKeepClickable)
            {
                graphic.raycastTarget = true;
                keptCount++;
            }
            else
            {
                if (graphic.raycastTarget)
                {
                    disabledCount++;
                }

                graphic.raycastTarget = false;
            }
        }

        if (showLog)
        {
            Debug.Log("UIRaycastCleaner finished. Disabled: " + disabledCount + " / Kept clickable: " + keptCount);
        }
    }
}