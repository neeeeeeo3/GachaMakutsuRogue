using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class HeroVisualTypeAssigner : MonoBehaviour
{
    public enum SelectionMode
    {
        Cycle,
        Random,
        Fixed
    }

    [Header("Selection")]
    public SelectionMode selectionMode = SelectionMode.Cycle;

    public HeroVisualBuilder.HeroVisualType fixedType = HeroVisualBuilder.HeroVisualType.BraveHero;

    public HeroVisualBuilder.HeroVisualType[] cycleOrder =
    {
        HeroVisualBuilder.HeroVisualType.BraveHero,
        HeroVisualBuilder.HeroVisualType.Knight,
        HeroVisualBuilder.HeroVisualType.Mage,
        HeroVisualBuilder.HeroVisualType.Ranger,
        HeroVisualBuilder.HeroVisualType.HeavyWarrior
    };

    [Header("Debug")]
    public bool showDebugLog = true;

    private static int spawnCounter;

    private void Awake()
    {
        AssignVisualType();
    }

    [ContextMenu("Assign Visual Type Now")]
    public void AssignVisualType()
    {
        HeroVisualBuilder visualBuilder = GetComponent<HeroVisualBuilder>();

        if (visualBuilder == null)
        {
            Debug.LogWarning("HeroVisualBuilder not found on this hero.");
            return;
        }

        HeroVisualBuilder.HeroVisualType selectedType = PickVisualType();

        visualBuilder.heroVisualType = selectedType;

        if (showDebugLog)
        {
            Debug.Log("Hero visual type assigned: " + selectedType);
        }
    }

    private HeroVisualBuilder.HeroVisualType PickVisualType()
    {
        if (selectionMode == SelectionMode.Fixed)
        {
            return fixedType;
        }

        if (selectionMode == SelectionMode.Random)
        {
            return PickRandomType();
        }

        return PickCycleType();
    }

    private HeroVisualBuilder.HeroVisualType PickCycleType()
    {
        if (cycleOrder == null || cycleOrder.Length <= 0)
        {
            return HeroVisualBuilder.HeroVisualType.BraveHero;
        }

        HeroVisualBuilder.HeroVisualType selectedType =
            cycleOrder[spawnCounter % cycleOrder.Length];

        spawnCounter++;

        return selectedType;
    }

    private HeroVisualBuilder.HeroVisualType PickRandomType()
    {
        if (cycleOrder != null && cycleOrder.Length > 0)
        {
            return cycleOrder[Random.Range(0, cycleOrder.Length)];
        }

        int typeCount = System.Enum.GetValues(typeof(HeroVisualBuilder.HeroVisualType)).Length;
        int randomIndex = Random.Range(0, typeCount);

        return (HeroVisualBuilder.HeroVisualType)randomIndex;
    }

    [ContextMenu("Reset Spawn Counter")]
    public void ResetSpawnCounter()
    {
        spawnCounter = 0;
        Debug.Log("Hero visual spawn counter reset.");
    }
}