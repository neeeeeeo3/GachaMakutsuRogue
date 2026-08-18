using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class CapsuleOpener : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject objectPrefab;
    public float openDelay = 0.6f;
    public bool openAutomatically = true;
    public bool destroyCapsuleAfterOpen = true;

    [Header("Rarity Transfer")]
    public bool transferRarityToSpawnedObject = true;
    public bool addRarityHolderIfMissing = true;
    public bool applyRarityStatMultipliers = true;

    [Header("Rarity Visual")]
    public bool addRarityVisualToSpawnedObject = true;
    public bool rebuildRarityVisualAfterOpen = true;
    public bool hideCommonRarityVisual = true;
    public float rarityVisualScale = 1f;
    public int rarityVisualSortingOrderOffset = 30;

    [Header("Placeable Object")]
    public bool addPlaceableObjectToSpawnedObject = true;
    public bool spawnedObjectCountsAsOccupied = true;

    [Header("Rarity Visual Scale")]
    public bool applyVisualScaleByRarity = false;
    public float commonVisualScale = 1f;
    public float rareVisualScale = 1.08f;
    public float epicVisualScale = 1.16f;

    [Header("Open Motion")]
    public bool shakeBeforeOpen = true;
    public float shakeDuration = 0.18f;
    public float shakePower = 0.045f;
    public int shakeCount = 5;

    [Header("Open Effect")]
    public bool createOpenEffect = true;
    public float openEffectDuration = 0.45f;
    public float openEffectScale = 1f;
    public string openEffectSortingLayerName = "Default";
    public int openEffectSortingOrder = 2600;

    [Header("Debug")]
    public bool showDebugLog = false;

    private bool hasInitialized;
    private bool hasOpened;
    private Coroutine openCoroutine;

    public void Initialize(GameObject newObjectPrefab, float newOpenDelay)
    {
        objectPrefab = newObjectPrefab;
        openDelay = newOpenDelay;
        hasInitialized = true;

        if (openAutomatically)
        {
            StartOpenTimer();
        }
    }

    private void Start()
    {
        if (!openAutomatically)
        {
            return;
        }

        if (hasInitialized)
        {
            return;
        }

        if (objectPrefab == null)
        {
            return;
        }

        StartOpenTimer();
    }

    public void StartOpenTimer()
    {
        if (hasOpened)
        {
            return;
        }

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
        }

        openCoroutine = StartCoroutine(OpenRoutine());
    }

    public void OpenImmediately()
    {
        if (hasOpened)
        {
            return;
        }

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }

        StartCoroutine(OpenNowRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        if (openDelay > 0f)
        {
            yield return new WaitForSeconds(openDelay);
        }

        yield return OpenNowRoutine();
    }

    private IEnumerator OpenNowRoutine()
    {
        if (hasOpened)
        {
            yield break;
        }

        hasOpened = true;

        if (shakeBeforeOpen)
        {
            yield return ShakeRoutine();
        }

        GameObject spawnedObject = SpawnContent();

        if (createOpenEffect)
        {
            Color effectColor = GetEffectColor();
            CreateOpenEffect(transform.position, effectColor);
        }

        if (showDebugLog)
        {
            string spawnedName = spawnedObject != null ? spawnedObject.name : "NULL";
            Debug.Log("Capsule opened. Spawned: " + spawnedName);
        }

        if (destroyCapsuleAfterOpen)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 baseLocalPosition = transform.localPosition;

        float timer = 0f;
        int safeShakeCount = Mathf.Max(1, shakeCount);

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, shakeDuration));
            float wave = Mathf.Sin(t * Mathf.PI * safeShakeCount);
            float fade = 1f - t;

            transform.localPosition = baseLocalPosition + new Vector3(
                wave * shakePower * fade,
                Mathf.Abs(wave) * shakePower * 0.35f * fade,
                0f
            );

            yield return null;
        }

        transform.localPosition = baseLocalPosition;
    }

    private GameObject SpawnContent()
    {
        if (objectPrefab == null)
        {
            Debug.LogWarning("CapsuleOpener has no objectPrefab.");
            return null;
        }

        Vector3 spawnPosition = transform.position;
        spawnPosition.z = 0f;

        GameObject spawnedObject = Instantiate(objectPrefab, spawnPosition, Quaternion.identity);

        GachaRarityHolder capsuleRarityHolder = GetComponent<GachaRarityHolder>();

        if (transferRarityToSpawnedObject)
        {
            TransferRarityToSpawnedObject(spawnedObject, capsuleRarityHolder);
        }

        GachaRarityHolder spawnedRarityHolder = spawnedObject != null
            ? spawnedObject.GetComponent<GachaRarityHolder>()
            : null;

        if (applyRarityStatMultipliers)
        {
            ApplyRarityMultipliersToSpawnedObject(spawnedObject, spawnedRarityHolder);
        }

        if (applyVisualScaleByRarity)
        {
            ApplyRarityVisualScale(spawnedObject, spawnedRarityHolder);
        }

        if (addRarityVisualToSpawnedObject)
        {
            EnsureRarityVisual(spawnedObject, spawnedRarityHolder);
        }

        if (addPlaceableObjectToSpawnedObject)
        {
            EnsureSpawnedPlaceableObject(spawnedObject, spawnedRarityHolder);
        }

        return spawnedObject;
    }

    private void TransferRarityToSpawnedObject(GameObject spawnedObject, GachaRarityHolder sourceRarityHolder)
    {
        if (spawnedObject == null)
        {
            return;
        }

        if (sourceRarityHolder == null)
        {
            if (!addRarityHolderIfMissing)
            {
                return;
            }

            GachaRarityHolder defaultHolder = spawnedObject.GetComponent<GachaRarityHolder>();

            if (defaultHolder == null)
            {
                defaultHolder = spawnedObject.AddComponent<GachaRarityHolder>();
            }

            defaultHolder.Initialize(
                spawnedObject.name,
                GachaRarityType.Common,
                spawnedObject.name.ToUpperInvariant(),
                Color.white
            );

            return;
        }

        GachaRarityHolder spawnedRarityHolder = spawnedObject.GetComponent<GachaRarityHolder>();

        if (spawnedRarityHolder == null)
        {
            if (!addRarityHolderIfMissing)
            {
                return;
            }

            spawnedRarityHolder = spawnedObject.AddComponent<GachaRarityHolder>();
        }

        spawnedRarityHolder.Initialize(
            sourceRarityHolder.itemName,
            sourceRarityHolder.rarity,
            sourceRarityHolder.displayName,
            sourceRarityHolder.capsuleColor
        );

        spawnedRarityHolder.hpMultiplier = sourceRarityHolder.hpMultiplier;
        spawnedRarityHolder.attackMultiplier = sourceRarityHolder.attackMultiplier;
        spawnedRarityHolder.rewardMultiplier = sourceRarityHolder.rewardMultiplier;
        spawnedRarityHolder.specialMultiplier = sourceRarityHolder.specialMultiplier;

        spawnedObject.name = sourceRarityHolder.displayName;

        if (showDebugLog)
        {
            Debug.Log(
                "Transferred rarity to spawned object: "
                + sourceRarityHolder.displayName
                + " / "
                + sourceRarityHolder.rarity
            );
        }
    }

    private void EnsureRarityVisual(GameObject spawnedObject, GachaRarityHolder rarityHolder)
    {
        if (spawnedObject == null)
        {
            return;
        }

        if (rarityHolder == null)
        {
            rarityHolder = spawnedObject.GetComponent<GachaRarityHolder>();
        }

        if (rarityHolder == null)
        {
            return;
        }

        GachaRarityVisual rarityVisual = spawnedObject.GetComponent<GachaRarityVisual>();

        if (rarityVisual == null)
        {
            rarityVisual = spawnedObject.AddComponent<GachaRarityVisual>();
        }

        rarityVisual.rarityHolder = rarityHolder;
        rarityVisual.autoFindRarityHolder = true;
        rarityVisual.rebuildOnStart = false;
        rarityVisual.hideVisualForCommon = hideCommonRarityVisual;
        rarityVisual.visualScale = rarityVisualScale;
        rarityVisual.sortingOrderOffset = rarityVisualSortingOrderOffset;

        if (rebuildRarityVisualAfterOpen)
        {
            rarityVisual.RebuildVisual();
        }

        if (showDebugLog)
        {
            Debug.Log("Rarity visual applied to spawned object: " + rarityHolder.displayName);
        }
    }

    private void ApplyRarityMultipliersToSpawnedObject(GameObject spawnedObject, GachaRarityHolder rarityHolder)
    {
        if (spawnedObject == null)
        {
            return;
        }

        if (rarityHolder == null)
        {
            return;
        }

        float hpMultiplier = Mathf.Max(0.01f, rarityHolder.hpMultiplier);
        float attackMultiplier = Mathf.Max(0.01f, rarityHolder.attackMultiplier);
        float rewardMultiplier = Mathf.Max(0.01f, rarityHolder.rewardMultiplier);
        float specialMultiplier = Mathf.Max(0.01f, rarityHolder.specialMultiplier);

        MonoBehaviour[] behaviours = spawnedObject.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            ApplyNumberMemberMultiplier(behaviour, "maxHp", hpMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "currentHp", hpMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "hp", hpMultiplier);

            ApplyNumberMemberMultiplier(behaviour, "maxHealth", hpMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "currentHealth", hpMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "health", hpMultiplier);

            ApplyNumberMemberMultiplier(behaviour, "attackDamage", attackMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "damage", attackMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "trapDamage", attackMultiplier);

            ApplyNumberMemberMultiplier(behaviour, "rewardMana", rewardMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "manaReward", rewardMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "rewardAmount", rewardMultiplier);

            ApplyNumberMemberMultiplier(behaviour, "foodValue", specialMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "reproduceBonus", specialMultiplier);
            ApplyNumberMemberMultiplier(behaviour, "effectPower", specialMultiplier);
        }

        if (showDebugLog)
        {
            Debug.Log(
                "Applied rarity multipliers. HP="
                + hpMultiplier
                + " ATK="
                + attackMultiplier
                + " REWARD="
                + rewardMultiplier
                + " SPECIAL="
                + specialMultiplier
            );
        }
    }

    private void ApplyNumberMemberMultiplier(MonoBehaviour targetBehaviour, string memberName, float multiplier)
    {
        if (targetBehaviour == null)
        {
            return;
        }

        Type type = targetBehaviour.GetType();

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        FieldInfo fieldInfo = type.GetField(memberName, flags);

        if (fieldInfo != null)
        {
            object currentValue = fieldInfo.GetValue(targetBehaviour);

            if (TryMultiplyValue(currentValue, fieldInfo.FieldType, multiplier, out object newValue))
            {
                fieldInfo.SetValue(targetBehaviour, newValue);

                if (showDebugLog)
                {
                    Debug.Log(
                        "Rarity stat applied: "
                        + type.Name
                        + "."
                        + memberName
                        + " -> "
                        + newValue
                    );
                }
            }

            return;
        }

        PropertyInfo propertyInfo = type.GetProperty(memberName, flags);

        if (propertyInfo != null && propertyInfo.CanRead && propertyInfo.CanWrite)
        {
            object currentValue = propertyInfo.GetValue(targetBehaviour);

            if (TryMultiplyValue(currentValue, propertyInfo.PropertyType, multiplier, out object newValue))
            {
                propertyInfo.SetValue(targetBehaviour, newValue);

                if (showDebugLog)
                {
                    Debug.Log(
                        "Rarity stat applied: "
                        + type.Name
                        + "."
                        + memberName
                        + " -> "
                        + newValue
                    );
                }
            }
        }
    }

    private bool TryMultiplyValue(object currentValue, Type valueType, float multiplier, out object newValue)
    {
        newValue = null;

        if (currentValue == null)
        {
            return false;
        }

        if (valueType == typeof(int))
        {
            int currentInt = (int)currentValue;
            int multiplied = Mathf.Max(1, Mathf.RoundToInt(currentInt * multiplier));
            newValue = multiplied;
            return true;
        }

        if (valueType == typeof(float))
        {
            float currentFloat = (float)currentValue;
            float multiplied = currentFloat * multiplier;
            newValue = multiplied;
            return true;
        }

        if (valueType == typeof(double))
        {
            double currentDouble = (double)currentValue;
            double multiplied = currentDouble * multiplier;
            newValue = multiplied;
            return true;
        }

        return false;
    }

    private void ApplyRarityVisualScale(GameObject spawnedObject, GachaRarityHolder rarityHolder)
    {
        if (spawnedObject == null)
        {
            return;
        }

        if (rarityHolder == null)
        {
            return;
        }

        float scaleMultiplier = GetVisualScaleMultiplier(rarityHolder.rarity);

        spawnedObject.transform.localScale = new Vector3(
            spawnedObject.transform.localScale.x * scaleMultiplier,
            spawnedObject.transform.localScale.y * scaleMultiplier,
            spawnedObject.transform.localScale.z
        );
    }

    private float GetVisualScaleMultiplier(GachaRarityType rarity)
    {
        switch (rarity)
        {
            case GachaRarityType.Common:
                return commonVisualScale;

            case GachaRarityType.Rare:
                return rareVisualScale;

            case GachaRarityType.Epic:
                return epicVisualScale;
        }

        return 1f;
    }

    private void EnsureSpawnedPlaceableObject(GameObject spawnedObject, GachaRarityHolder rarityHolder)
    {
        if (spawnedObject == null)
        {
            return;
        }

        PlaceableObject placeableObject = spawnedObject.GetComponent<PlaceableObject>();

        if (placeableObject == null)
        {
            placeableObject = spawnedObject.AddComponent<PlaceableObject>();
        }

        if (rarityHolder != null && !string.IsNullOrWhiteSpace(rarityHolder.displayName))
        {
            placeableObject.objectName = rarityHolder.displayName;
        }
        else
        {
            placeableObject.objectName = spawnedObject.name;
        }

        placeableObject.countsAsOccupied = spawnedObjectCountsAsOccupied;
    }

    private Color GetEffectColor()
    {
        GachaRarityHolder rarityHolder = GetComponent<GachaRarityHolder>();

        if (rarityHolder != null)
        {
            Color color = rarityHolder.capsuleColor;

            if (color.a <= 0f)
            {
                color.a = 1f;
            }

            return color;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            return spriteRenderer.color;
        }

        return Color.white;
    }

    private void CreateOpenEffect(Vector3 worldPosition, Color effectColor)
    {
        Sprite circleSprite = CreateCircleSprite(48);
        Sprite squareSprite = CreateSquareSprite();

        int sortingLayerId = GetSortingLayerId(openEffectSortingLayerName);

        GameObject effectRoot = new GameObject("CapsuleOpenEffect");
        effectRoot.transform.position = worldPosition;
        effectRoot.transform.rotation = Quaternion.identity;
        effectRoot.transform.localScale = Vector3.one * openEffectScale;
        effectRoot.layer = gameObject.layer;

        Color auraColor = effectColor;
        auraColor.a = 0.30f;

        CreateSpritePart(
            effectRoot.transform,
            "OpenAura",
            circleSprite,
            Vector3.zero,
            new Vector3(0.72f, 0.72f, 1f),
            0f,
            auraColor,
            sortingLayerId,
            openEffectSortingOrder
        );

        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            float distance = i % 2 == 0 ? 0.32f : 0.45f;

            Color pieceColor = i % 2 == 0 ? effectColor : Color.white;
            pieceColor.a = 0.75f;

            CreateSpritePart(
                effectRoot.transform,
                "OpenSpark_" + i,
                squareSprite,
                direction * distance,
                new Vector3(0.06f, 0.16f, 1f),
                angle,
                pieceColor,
                sortingLayerId,
                openEffectSortingOrder + 1
            );
        }

        Destroy(effectRoot, openEffectDuration);
    }

    private SpriteRenderer CreateSpritePart(
        Transform parent,
        string objectName,
        Sprite sprite,
        Vector3 localPosition,
        Vector3 localScale,
        float localRotationZ,
        Color color,
        int sortingLayerId,
        int sortingOrder
    )
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = localScale;
        obj.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
        obj.layer = gameObject.layer;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;

        return renderer;
    }

    private int GetSortingLayerId(string sortingLayerName)
    {
        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            SortingLayer[] sortingLayers = SortingLayer.layers;

            foreach (SortingLayer sortingLayer in sortingLayers)
            {
                if (sortingLayer.name == sortingLayerName)
                {
                    return SortingLayer.NameToID(sortingLayerName);
                }
            }
        }

        return SortingLayer.NameToID("Default");
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = CreateTransparentTexture(4, 4, "CapsuleOpenEffectSquare");

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width
        );
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "CapsuleOpenEffectCircle");

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );
    }

    private Texture2D CreateTransparentTexture(int width, int height, string textureName)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.name = textureName;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        return texture;
    }
}