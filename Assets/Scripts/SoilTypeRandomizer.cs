using UnityEngine;

public class SoilTypeRandomizer : MonoBehaviour
{
    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Rates")]
    [Range(0f, 1f)]
    public float richSoilRate = 0.18f;

    [Range(0f, 1f)]
    public float hardSoilRate = 0.15f;

    [Header("Timing")]
    public bool randomizeOnStart = true;
    public float startDelay = 0.1f;

    private void Start()
    {
        if (randomizeOnStart)
        {
            Invoke(nameof(RandomizeSoilTypes), startDelay);
        }
    }

    [ContextMenu("Randomize Soil Types")]
    public void RandomizeSoilTypes()
    {
        AutoFindReferences();

        if (dungeonGridManager == null)
        {
            Debug.LogWarning("DungeonGridManager not found.");
            return;
        }

        DungeonTile[] tiles = FindObjectsByType<DungeonTile>(FindObjectsSortMode.None);

        foreach (DungeonTile tile in tiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (tile.IsFloor)
            {
                continue;
            }

            if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(tile.transform.position, out int x, out int y))
            {
                continue;
            }

            Vector2Int gridPosition = new Vector2Int(x, y);

            if (dungeonGridManager.IsEntranceGridPosition(gridPosition))
            {
                continue;
            }

            if (dungeonGridManager.hasCorePlaced && dungeonGridManager.IsCoreGridPosition(gridPosition))
            {
                continue;
            }

            float roll = Random.value;

            if (roll < richSoilRate)
            {
                tile.SetSoilType(DungeonTile.SoilType.Rich);
            }
            else if (roll < richSoilRate + hardSoilRate)
            {
                tile.SetSoilType(DungeonTile.SoilType.Hard);
            }
            else
            {
                tile.SetSoilType(DungeonTile.SoilType.Normal);
            }
        }

        Debug.Log("Soil types randomized.");
    }

    private void AutoFindReferences()
    {
        if (!autoFindDungeonGridManager)
        {
            return;
        }

        if (dungeonGridManager == null)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }
    }
}