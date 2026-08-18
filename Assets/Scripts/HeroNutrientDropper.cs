using System.Collections.Generic;
using UnityEngine;

public class HeroNutrientDropper : MonoBehaviour
{
    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Drop Enabled")]
    public bool dropNutrients = true;

    [Header("Normal Hero Nutrients")]
    public int normalRadius = 1;
    public int normalMaxTiles = 4;

    [Header("Fast Hero Nutrients")]
    public int fastRadius = 1;
    public int fastMaxTiles = 3;

    [Header("Tank Hero Nutrients")]
    public int tankRadius = 2;
    public int tankMaxTiles = 8;

    [Header("Thief Hero Nutrients")]
    public int thiefRadius = 1;
    public int thiefMaxTiles = 3;

    [Header("Fallback Search")]
    public bool expandSearchIfNotEnoughSoil = true;

    [Tooltip("近くに未掘り土が少ない場合、ここまで外側を探します。")]
    public int fallbackExtraRadius = 4;

    [Tooltip("最低でもこの数のRich Soil化を狙います。0なら各HeroのMaxTilesだけを上限として使います。")]
    public int minimumTargetTiles = 1;

    [Header("Conversion Rule")]
    public bool useManhattanRadius = true;

    [Tooltip("ONにするとHard SoilもRich Soilに変えます。最初はOFF推奨です。")]
    public bool canConvertHardSoil = false;

    [Tooltip("ONにすると、すでにRich Soilのタイルも変換成功として数えます。通常はOFF推奨です。")]
    public bool countAlreadyRichTiles = false;

    [Tooltip("ONにすると近い土から順番にRich化します。OFFなら候補をシャッフルします。")]
    public bool preferNearestTiles = true;

    [Header("Visual Effect")]
    public bool createNutrientEffect = true;
    public Color nutrientEffectColor = new Color(0.72f, 1f, 0.32f, 0.82f);
    public float nutrientEffectSize = 0.55f;
    public float nutrientEffectDuration = 0.45f;
    public int nutrientEffectSortingOrder = 2300;

    [Header("Debug")]
    public bool showDebugLog = true;

    private Sprite squareSprite;

    private class TileCandidate
    {
        public DungeonTile tile;
        public int distance;

        public TileCandidate(DungeonTile newTile, int newDistance)
        {
            tile = newTile;
            distance = newDistance;
        }
    }

    private void Awake()
    {
        squareSprite = CreateSquareSprite();
    }

    public void DropNutrientsAtHeroPosition(string heroName)
    {
        DropNutrientsAtWorldPosition(transform.position, heroName);
    }

    public void DropNutrientsAtWorldPosition(Vector3 worldPosition, string heroName)
    {
        if (!dropNutrients)
        {
            DebugLog("Drop skipped. dropNutrients is OFF.");
            return;
        }

        AutoFindReferences();

        if (dungeonGridManager == null)
        {
            Debug.LogWarning("HeroNutrientDropper: DungeonGridManager not found.");
            return;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(worldPosition, out int centerX, out int centerY))
        {
            DebugLog("Hero died outside dungeon grid. WorldPosition=" + worldPosition);
            return;
        }

        Vector2Int center = new Vector2Int(centerX, centerY);

        int baseRadius = GetRadiusForHero(heroName);
        int maxTiles = GetMaxTilesForHero(heroName);
        int targetTiles = Mathf.Max(minimumTargetTiles, maxTiles);

        if (minimumTargetTiles <= 0)
        {
            targetTiles = maxTiles;
        }

        int searchRadius = Mathf.Max(0, baseRadius);

        if (expandSearchIfNotEnoughSoil)
        {
            searchRadius += Mathf.Max(0, fallbackExtraRadius);
        }

        List<TileCandidate> candidates = CollectConvertibleCandidateTiles(center, searchRadius);

        if (!preferNearestTiles)
        {
            Shuffle(candidates);
        }
        else
        {
            candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        }

        int convertedCount = 0;
        int safeMaxTiles = Mathf.Max(0, maxTiles);
        int safeTargetTiles = Mathf.Max(0, targetTiles);

        int finalTargetCount = safeMaxTiles;

        if (minimumTargetTiles > 0)
        {
            finalTargetCount = Mathf.Min(safeMaxTiles, safeTargetTiles);
        }

        if (finalTargetCount <= 0)
        {
            finalTargetCount = safeMaxTiles;
        }

        foreach (TileCandidate candidate in candidates)
        {
            if (candidate == null || candidate.tile == null)
            {
                continue;
            }

            if (convertedCount >= finalTargetCount)
            {
                break;
            }

            if (TryConvertTileToRich(candidate.tile))
            {
                convertedCount++;

                if (createNutrientEffect)
                {
                    CreateNutrientEffect(candidate.tile.transform.position);
                }
            }
        }

        DebugLog(
            "Hero nutrients dropped."
            + " Hero=" + heroName
            + " Center=" + center
            + " BaseRadius=" + baseRadius
            + " SearchRadius=" + searchRadius
            + " Candidates=" + candidates.Count
            + " Converted=" + convertedCount
        );

        if (convertedCount <= 0)
        {
            Debug.LogWarning(
                "HeroNutrientDropper: No soil was converted. "
                + "Reason is usually: nearby tiles are already Floor, already Rich, Hard Soil is blocked, or hero died outside valid soil range."
            );
        }
    }

    private List<TileCandidate> CollectConvertibleCandidateTiles(Vector2Int center, int radius)
    {
        List<TileCandidate> candidates = new List<TileCandidate>();

        int safeRadius = Mathf.Max(0, radius);

        for (int offsetX = -safeRadius; offsetX <= safeRadius; offsetX++)
        {
            for (int offsetY = -safeRadius; offsetY <= safeRadius; offsetY++)
            {
                int manhattanDistance = Mathf.Abs(offsetX) + Mathf.Abs(offsetY);

                if (useManhattanRadius)
                {
                    if (manhattanDistance > safeRadius)
                    {
                        continue;
                    }
                }
                else
                {
                    float distance = Mathf.Sqrt(offsetX * offsetX + offsetY * offsetY);

                    if (distance > safeRadius + 0.01f)
                    {
                        continue;
                    }
                }

                Vector2Int gridPosition = center + new Vector2Int(offsetX, offsetY);
                DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(gridPosition);

                if (!IsConvertibleTile(tile))
                {
                    continue;
                }

                candidates.Add(new TileCandidate(tile, manhattanDistance));
            }
        }

        return candidates;
    }

    private bool IsConvertibleTile(DungeonTile tile)
    {
        if (tile == null)
        {
            return false;
        }

        if (!tile.IsSoil)
        {
            return false;
        }

        if (tile.soilType == DungeonTile.SoilType.Rich)
        {
            return countAlreadyRichTiles;
        }

        if (tile.soilType == DungeonTile.SoilType.Hard && !canConvertHardSoil)
        {
            return false;
        }

        return true;
    }

    private bool TryConvertTileToRich(DungeonTile tile)
    {
        if (!IsConvertibleTile(tile))
        {
            return false;
        }

        if (tile.soilType == DungeonTile.SoilType.Rich)
        {
            return countAlreadyRichTiles;
        }

        tile.SetSoilType(DungeonTile.SoilType.Rich);
        return true;
    }

    private int GetRadiusForHero(string heroName)
    {
        string normalizedName = NormalizeHeroName(heroName);

        if (normalizedName.Contains("TANK"))
        {
            return tankRadius;
        }

        if (normalizedName.Contains("FAST"))
        {
            return fastRadius;
        }

        if (normalizedName.Contains("THIEF"))
        {
            return thiefRadius;
        }

        return normalRadius;
    }

    private int GetMaxTilesForHero(string heroName)
    {
        string normalizedName = NormalizeHeroName(heroName);

        if (normalizedName.Contains("TANK"))
        {
            return tankMaxTiles;
        }

        if (normalizedName.Contains("FAST"))
        {
            return fastMaxTiles;
        }

        if (normalizedName.Contains("THIEF"))
        {
            return thiefMaxTiles;
        }

        return normalMaxTiles;
    }

    private string NormalizeHeroName(string heroName)
    {
        if (string.IsNullOrWhiteSpace(heroName))
        {
            return "NORMAL";
        }

        return heroName.Trim().ToUpperInvariant();
    }

    private void CreateNutrientEffect(Vector3 position)
    {
        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        GameObject effectObject = new GameObject("NutrientRichSoilEffect");
        effectObject.transform.position = position;
        effectObject.transform.localScale = Vector3.one * nutrientEffectSize;

        SpriteRenderer spriteRenderer = effectObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = nutrientEffectColor;
        spriteRenderer.sortingOrder = nutrientEffectSortingOrder;

        NutrientTileEffectAnimation animation = effectObject.AddComponent<NutrientTileEffectAnimation>();
        animation.Initialize(nutrientEffectDuration);
    }

    private void Shuffle(List<TileCandidate> tiles)
    {
        if (tiles == null)
        {
            return;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            int randomIndex = Random.Range(i, tiles.Count);

            TileCandidate temp = tiles[i];
            tiles[i] = tiles[randomIndex];
            tiles[randomIndex] = temp;
        }
    }

    private void AutoFindReferences()
    {
        if (!autoFindDungeonGridManager)
        {
            return;
        }

        if (dungeonGridManager != null)
        {
            return;
        }

        if (DungeonGridManager.Instance != null)
        {
            dungeonGridManager = DungeonGridManager.Instance;
        }
        else
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "HeroNutrientSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private void DebugLog(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("HeroNutrientDropper: " + message);
    }
}

public class NutrientTileEffectAnimation : MonoBehaviour
{
    private float duration = 0.45f;
    private float timer;
    private Vector3 startScale;
    private SpriteRenderer spriteRenderer;

    public void Initialize(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
        startScale = transform.localScale;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / duration);
        float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

        transform.localScale = Vector3.Lerp(
            startScale,
            startScale * 1.45f,
            easedProgress
        );

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(color.a, 0f, easedProgress);
            spriteRenderer.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}