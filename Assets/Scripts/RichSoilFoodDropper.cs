using UnityEngine;

public class RichSoilFoodDropper : MonoBehaviour
{
    [Header("Food Prefab")]
    public GameObject foodPrefab;

    [Header("Drop Rule")]
    [Range(0f, 1f)]
    public float richSoilFoodDropChance = 1.0f;

    public bool dropOnlyFromRichSoil = true;

    [Header("Spawn Position")]
    public Vector3 spawnOffset = new Vector3(0f, 0f, 0f);
    public float randomSpawnRadius = 0.08f;

    [Header("Fallback Visual")]
    public bool createFallbackFoodWhenPrefabMissing = true;
    public Color fallbackFoodColor = new Color(1f, 0.55f, 0.18f, 1f);
    public float fallbackFoodSize = 0.22f;
    public int fallbackSortingOrder = 260;

    [Header("Pop Animation")]
    public bool addPopAnimation = true;
    public float popHeight = 0.18f;
    public float popDuration = 0.22f;
    public float popScale = 1.25f;
    public float idleWobbleAmount = 0.035f;
    public float idleWobbleSpeed = 5.5f;

    private Sprite squareSprite;

    private void OnEnable()
    {
        DungeonTile.OnAnyTileDugWithSoilType += HandleTileDug;
    }

    private void OnDisable()
    {
        DungeonTile.OnAnyTileDugWithSoilType -= HandleTileDug;
    }

    private void Awake()
    {
        squareSprite = CreateSquareSprite();
    }

    private void HandleTileDug(Vector3 dugWorldPosition, DungeonTile.SoilType soilType)
    {
        if (dropOnlyFromRichSoil && soilType != DungeonTile.SoilType.Rich)
        {
            return;
        }

        if (Random.value > richSoilFoodDropChance)
        {
            return;
        }

        SpawnFood(dugWorldPosition, soilType);
    }

    private void SpawnFood(Vector3 dugWorldPosition, DungeonTile.SoilType soilType)
    {
        Vector2 randomOffset = Random.insideUnitCircle * randomSpawnRadius;

        Vector3 spawnPosition = dugWorldPosition
            + spawnOffset
            + new Vector3(randomOffset.x, randomOffset.y, 0f);

        GameObject spawnedFood = null;

        if (foodPrefab != null)
        {
            spawnedFood = Instantiate(foodPrefab, spawnPosition, Quaternion.identity);
            spawnedFood.name = foodPrefab.name + "_FromRichSoil";
        }
        else if (createFallbackFoodWhenPrefabMissing)
        {
            spawnedFood = CreateFallbackFood(spawnPosition);
        }

        if (spawnedFood == null)
        {
            return;
        }

        if (spawnedFood.GetComponent<SlimeFoodItem>() == null)
        {
            spawnedFood.AddComponent<SlimeFoodItem>();
        }

        if (addPopAnimation)
        {
            RichSoilFoodPopAnimation popAnimation = spawnedFood.AddComponent<RichSoilFoodPopAnimation>();
            popAnimation.Initialize(
                popHeight,
                popDuration,
                popScale,
                idleWobbleAmount,
                idleWobbleSpeed
            );
        }

        Debug.Log("Food dropped from rich soil: " + spawnPosition);
    }

    private GameObject CreateFallbackFood(Vector3 spawnPosition)
    {
        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        GameObject foodObject = new GameObject("FallbackFood_FromRichSoil");
        foodObject.transform.position = spawnPosition;
        foodObject.transform.localScale = new Vector3(fallbackFoodSize, fallbackFoodSize, 1f);

        SpriteRenderer spriteRenderer = foodObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = fallbackFoodColor;
        spriteRenderer.sortingOrder = fallbackSortingOrder;

        CircleCollider2D circleCollider = foodObject.AddComponent<CircleCollider2D>();
        circleCollider.radius = 0.5f;
        circleCollider.isTrigger = true;

        return foodObject;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "RichSoilFoodSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }
}

public class RichSoilFoodPopAnimation : MonoBehaviour
{
    private float popHeight;
    private float popDuration;
    private float popScale;
    private float idleWobbleAmount;
    private float idleWobbleSpeed;

    private float timer;
    private Vector3 startPosition;
    private Vector3 baseScale;
    private bool initialized;

    public void Initialize(
        float newPopHeight,
        float newPopDuration,
        float newPopScale,
        float newIdleWobbleAmount,
        float newIdleWobbleSpeed
    )
    {
        popHeight = newPopHeight;
        popDuration = Mathf.Max(0.01f, newPopDuration);
        popScale = Mathf.Max(1f, newPopScale);
        idleWobbleAmount = newIdleWobbleAmount;
        idleWobbleSpeed = newIdleWobbleSpeed;

        startPosition = transform.position;
        baseScale = transform.localScale;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer <= popDuration)
        {
            UpdatePop();
        }
        else
        {
            UpdateIdleWobble();
        }
    }

    private void UpdatePop()
    {
        float progress = Mathf.Clamp01(timer / popDuration);

        float jump = Mathf.Sin(progress * Mathf.PI) * popHeight;
        transform.position = startPosition + new Vector3(0f, jump, 0f);

        float scaleWave = Mathf.Sin(progress * Mathf.PI);
        float currentScale = Mathf.Lerp(1f, popScale, scaleWave);

        transform.localScale = baseScale * currentScale;
    }

    private void UpdateIdleWobble()
    {
        float wobble = Mathf.Sin(Time.time * idleWobbleSpeed) * idleWobbleAmount;

        transform.position = startPosition + new Vector3(0f, wobble, 0f);
        transform.localScale = baseScale;
    }
}