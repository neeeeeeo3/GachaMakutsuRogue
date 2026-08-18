using UnityEngine;

public class DirtBreakEffectManager : MonoBehaviour
{
    [Header("Normal Effect Count")]
    public int chunkCount = 10;
    public int dustCount = 8;

    [Header("Hard Soil Effect Count")]
    public int hardChunkCount = 16;
    public int hardDustCount = 4;

    [Header("Timing")]
    public float chunkLifetime = 0.42f;
    public float dustLifetime = 0.32f;
    public float hardChunkLifetime = 0.52f;
    public float hardDustLifetime = 0.24f;

    [Header("Movement")]
    public float chunkSpeedMin = 1.2f;
    public float chunkSpeedMax = 2.8f;
    public float dustSpeedMin = 0.4f;
    public float dustSpeedMax = 1.2f;
    public float gravity = 3.5f;

    [Header("Hard Soil Movement")]
    public float hardChunkSpeedMin = 1.8f;
    public float hardChunkSpeedMax = 4.0f;
    public float hardDustSpeedMin = 0.3f;
    public float hardDustSpeedMax = 0.9f;
    public float hardGravity = 4.6f;

    [Header("Size")]
    public float chunkSizeMin = 0.06f;
    public float chunkSizeMax = 0.12f;
    public float dustSizeMin = 0.10f;
    public float dustSizeMax = 0.22f;

    [Header("Hard Soil Size")]
    public float hardChunkSizeMin = 0.04f;
    public float hardChunkSizeMax = 0.10f;
    public float hardDustSizeMin = 0.08f;
    public float hardDustSizeMax = 0.16f;

    [Header("Look")]
    public int sortingOrder = 300;
    public Color normalDirtColor = new Color(0.42f, 0.25f, 0.12f, 1f);
    public Color richDirtColor = new Color(0.75f, 0.55f, 0.18f, 1f);
    public Color hardStoneColor = new Color(0.10f, 0.13f, 0.18f, 1f);
    public Color hardStoneHighlightColor = new Color(0.30f, 0.34f, 0.42f, 1f);
    public Color dustColor = new Color(0.62f, 0.48f, 0.32f, 0.55f);
    public Color hardDustColor = new Color(0.36f, 0.39f, 0.44f, 0.45f);

    [Header("Spawn")]
    public Vector3 spawnOffset = new Vector3(0f, 0f, 0f);
    public float spawnRadius = 0.12f;

    private Sprite squareSprite;

    private void OnEnable()
    {
        DungeonTile.OnAnyTileDugWithSoilType += SpawnBreakEffect;
    }

    private void OnDisable()
    {
        DungeonTile.OnAnyTileDugWithSoilType -= SpawnBreakEffect;
    }

    private void Awake()
    {
        squareSprite = CreateSquareSprite();
    }

    private void SpawnBreakEffect(Vector3 worldPosition, DungeonTile.SoilType soilType)
    {
        if (squareSprite == null)
        {
            squareSprite = CreateSquareSprite();
        }

        Vector3 origin = worldPosition + spawnOffset;

        if (soilType == DungeonTile.SoilType.Hard)
        {
            SpawnHardSoilEffect(origin);
            return;
        }

        SpawnNormalSoilEffect(origin, soilType);
    }

    private void SpawnNormalSoilEffect(Vector3 origin, DungeonTile.SoilType soilType)
    {
        Color chunkColor = GetNormalChunkColor(soilType);

        for (int i = 0; i < chunkCount; i++)
        {
            SpawnChunk(
                origin,
                chunkColor,
                chunkSpeedMin,
                chunkSpeedMax,
                chunkSizeMin,
                chunkSizeMax,
                chunkLifetime,
                gravity,
                false
            );
        }

        for (int i = 0; i < dustCount; i++)
        {
            SpawnDust(
                origin,
                dustColor,
                dustSpeedMin,
                dustSpeedMax,
                dustSizeMin,
                dustSizeMax,
                dustLifetime,
                0.25f
            );
        }
    }

    private void SpawnHardSoilEffect(Vector3 origin)
    {
        for (int i = 0; i < hardChunkCount; i++)
        {
            Color stoneColor = hardStoneColor;

            if (Random.value < 0.28f)
            {
                stoneColor = hardStoneHighlightColor;
            }

            SpawnChunk(
                origin,
                stoneColor,
                hardChunkSpeedMin,
                hardChunkSpeedMax,
                hardChunkSizeMin,
                hardChunkSizeMax,
                hardChunkLifetime,
                hardGravity,
                true
            );
        }

        for (int i = 0; i < hardDustCount; i++)
        {
            SpawnDust(
                origin,
                hardDustColor,
                hardDustSpeedMin,
                hardDustSpeedMax,
                hardDustSizeMin,
                hardDustSizeMax,
                hardDustLifetime,
                0.15f
            );
        }
    }

    private Color GetNormalChunkColor(DungeonTile.SoilType soilType)
    {
        if (soilType == DungeonTile.SoilType.Rich)
        {
            return richDirtColor;
        }

        return normalDirtColor;
    }

    private void SpawnChunk(
        Vector3 origin,
        Color color,
        float speedMin,
        float speedMax,
        float sizeMin,
        float sizeMax,
        float lifetime,
        float particleGravity,
        bool isHardChunk
    )
    {
        GameObject chunkObject = new GameObject(isHardChunk ? "HardStoneChunk" : "DirtChunk");
        chunkObject.transform.SetParent(transform);

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        chunkObject.transform.position = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);

        SpriteRenderer spriteRenderer = chunkObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        float size = Random.Range(sizeMin, sizeMax);

        float xScale = size;
        float yScale = size;

        if (isHardChunk)
        {
            xScale *= Random.Range(0.75f, 1.45f);
            yScale *= Random.Range(0.45f, 1.05f);
        }

        chunkObject.transform.localScale = new Vector3(xScale, yScale, 1f);
        chunkObject.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float angle = Random.Range(15f, 165f);
        float speed = Random.Range(speedMin, speedMax);
        Vector3 velocity = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad),
            0f
        ) * speed;

        float angularVelocity = isHardChunk
            ? Random.Range(-720f, 720f)
            : Random.Range(-480f, 480f);

        DirtBreakEffectParticle particle = chunkObject.AddComponent<DirtBreakEffectParticle>();
        particle.Initialize(
            velocity,
            angularVelocity,
            lifetime,
            particleGravity,
            xScale,
            xScale * 0.25f,
            true
        );
    }

    private void SpawnDust(
        Vector3 origin,
        Color color,
        float speedMin,
        float speedMax,
        float sizeMin,
        float sizeMax,
        float lifetime,
        float particleGravity
    )
    {
        GameObject dustObject = new GameObject("DirtDust");
        dustObject.transform.SetParent(transform);

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        dustObject.transform.position = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);

        SpriteRenderer spriteRenderer = dustObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = squareSprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder - 1;

        float size = Random.Range(sizeMin, sizeMax);
        dustObject.transform.localScale = new Vector3(size, size, 1f);
        dustObject.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float angle = Random.Range(0f, 360f);
        float speed = Random.Range(speedMin, speedMax);
        Vector3 velocity = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad),
            0f
        ) * speed;

        float angularVelocity = Random.Range(-120f, 120f);

        DirtBreakEffectParticle particle = dustObject.AddComponent<DirtBreakEffectParticle>();
        particle.Initialize(
            velocity,
            angularVelocity,
            lifetime,
            particleGravity,
            size,
            size * 1.8f,
            true
        );
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "DirtBreakSquareTexture";
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

public class DirtBreakEffectParticle : MonoBehaviour
{
    private Vector3 velocity;
    private float angularVelocity;
    private float lifetime;
    private float gravity;
    private float startSize;
    private float endSize;
    private bool fadeOut;

    private float timer;
    private SpriteRenderer spriteRenderer;
    private Color startColor;

    public void Initialize(
        Vector3 initialVelocity,
        float initialAngularVelocity,
        float particleLifetime,
        float particleGravity,
        float initialSize,
        float finalSize,
        bool shouldFadeOut
    )
    {
        velocity = initialVelocity;
        angularVelocity = initialAngularVelocity;
        lifetime = Mathf.Max(0.01f, particleLifetime);
        gravity = particleGravity;
        startSize = initialSize;
        endSize = finalSize;
        fadeOut = shouldFadeOut;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / lifetime);

        velocity.y -= gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(0f, 0f, angularVelocity * Time.deltaTime);

        float size = Mathf.Lerp(startSize, endSize, progress);
        transform.localScale = new Vector3(size, size, 1f);

        if (fadeOut && spriteRenderer != null)
        {
            Color color = startColor;
            color.a = startColor.a * (1f - progress);
            spriteRenderer.color = color;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}