using System.Reflection;
using UnityEngine;

public class GachaMachineHandleAutoBinder : MonoBehaviour
{
    [Header("References")]
    public GachaMachineVisualBuilder visualBuilder;
    public GachaMachineAnimator gachaMachineAnimator;

    [Header("Auto Find")]
    public bool autoFindVisualBuilder = true;
    public bool autoFindAnimator = true;

    [Header("Binding")]
    public bool bindOnStart = true;
    public bool keepTryingUntilBound = true;

    [Tooltip("Animator内のTransform型フィールド名に Handle が含まれていたら、新しいHandlePivotを自動代入します。")]
    public bool useReflectionBinding = true;

    [Header("Debug")]
    public bool showDebugLog = true;

    private bool hasBound;

    private void Start()
    {
        if (bindOnStart)
        {
            TryBindHandle();
        }
    }

    private void LateUpdate()
    {
        if (!keepTryingUntilBound)
        {
            return;
        }

        if (hasBound)
        {
            return;
        }

        TryBindHandle();
    }

    [ContextMenu("Try Bind Handle")]
    public void TryBindHandle()
    {
        AutoFindReferences();

        if (visualBuilder == null)
        {
            return;
        }

        if (gachaMachineAnimator == null)
        {
            return;
        }

        Transform handlePivot = visualBuilder.generatedHandlePivot;

        if (handlePivot == null)
        {
            return;
        }

        bool boundSomething = false;

        if (useReflectionBinding)
        {
            boundSomething = BindHandleByReflection(handlePivot);
        }

        hasBound = boundSomething;

        if (hasBound && showDebugLog)
        {
            Debug.Log("GachaMachineHandleAutoBinder bound generated HandlePivot to GachaMachineAnimator.");
        }
    }

    private void AutoFindReferences()
    {
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

        if (autoFindAnimator && gachaMachineAnimator == null)
        {
            gachaMachineAnimator = GetComponent<GachaMachineAnimator>();

            if (gachaMachineAnimator == null)
            {
                gachaMachineAnimator = GetComponentInChildren<GachaMachineAnimator>();
            }

            if (gachaMachineAnimator == null)
            {
                gachaMachineAnimator = FindFirstObjectByType<GachaMachineAnimator>();
            }
        }
    }

    private bool BindHandleByReflection(Transform newHandlePivot)
    {
        bool boundSomething = false;

        FieldInfo[] fields = typeof(GachaMachineAnimator).GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType != typeof(Transform))
            {
                continue;
            }

            string fieldName = field.Name.ToLower();

            if (!fieldName.Contains("handle"))
            {
                continue;
            }

            field.SetValue(gachaMachineAnimator, newHandlePivot);
            boundSomething = true;

            if (showDebugLog)
            {
                Debug.Log("Bound animator field: " + field.Name + " -> " + newHandlePivot.name);
            }
        }

        PropertyInfo[] properties = typeof(GachaMachineAnimator).GetProperties(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (PropertyInfo property in properties)
        {
            if (property.PropertyType != typeof(Transform))
            {
                continue;
            }

            if (!property.CanWrite)
            {
                continue;
            }

            string propertyName = property.Name.ToLower();

            if (!propertyName.Contains("handle"))
            {
                continue;
            }

            property.SetValue(gachaMachineAnimator, newHandlePivot);
            boundSomething = true;

            if (showDebugLog)
            {
                Debug.Log("Bound animator property: " + property.Name + " -> " + newHandlePivot.name);
            }
        }

        return boundSomething;
    }
}