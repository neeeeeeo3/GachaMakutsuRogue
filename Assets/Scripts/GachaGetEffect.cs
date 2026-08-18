using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GachaGetEffect
{
    private class PixelTextGroup
    {
        public GameObject root;
        public List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    }

    public static IEnumerator PlayRoutine(
        string itemName,
        Color itemColor,
        Vector3 worldPosition,
        float scale,
        float duration,
        string sortingLayerName,
        int sortingOrder,
        int targetLayer
    )
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            itemName = "ITEM";
        }

        itemName = itemName.ToUpperInvariant();

        Sprite circleSprite = CreateCircleSprite(96);
        Sprite roundedRectSprite = CreateRoundedRectSprite(96, 18);
        Sprite squareSprite = CreateSquareSprite();

        int sortingLayerId = GetSortingLayerId(sortingLayerName);

        GameObject root = new GameObject("DungeonGachaGetEffect");
        root.transform.position = worldPosition;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * scale;
        root.layer = targetLayer;

        Color auraColor = itemColor;
        auraColor.a = 0.26f;

        SpriteRenderer auraBack = CreateSpritePart(
            root.transform,
            "AuraBack",
            circleSprite,
            Vector3.zero,
            new Vector3(1.45f, 1.45f, 1f),
            0f,
            auraColor,
            sortingLayerId,
            sortingOrder,
            targetLayer
        );

        SpriteRenderer auraRing = CreateSpritePart(
            root.transform,
            "AuraRing",
            circleSprite,
            Vector3.zero,
            new Vector3(1.12f, 1.12f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.20f),
            sortingLayerId,
            sortingOrder + 1,
            targetLayer
        );

        SpriteRenderer labelBack = CreateSpritePart(
            root.transform,
            "LabelBack",
            roundedRectSprite,
            new Vector3(0f, -0.43f, 0f),
            new Vector3(1.85f, 0.46f, 1f),
            0f,
            new Color(0.04f, 0.05f, 0.08f, 0.88f),
            sortingLayerId,
            sortingOrder + 8,
            targetLayer
        );

        SpriteRenderer labelInner = CreateSpritePart(
            root.transform,
            "LabelInner",
            roundedRectSprite,
            new Vector3(0f, -0.43f, 0f),
            new Vector3(1.65f, 0.32f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.14f),
            sortingLayerId,
            sortingOrder + 9,
            targetLayer
        );

        SpriteRenderer[] rays = CreateBurstRays(
            root.transform,
            roundedRectSprite,
            itemColor,
            sortingLayerId,
            sortingOrder + 2,
            targetLayer
        );

        SpriteRenderer[] dots = CreateBurstDots(
            root.transform,
            circleSprite,
            itemColor,
            sortingLayerId,
            sortingOrder + 3,
            targetLayer
        );

        GameObject capsuleRoot = new GameObject("BigGetCapsule");
        capsuleRoot.transform.SetParent(root.transform);
        capsuleRoot.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        capsuleRoot.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
        capsuleRoot.transform.localScale = Vector3.one;
        capsuleRoot.layer = targetLayer;

        SpriteRenderer capsuleShadow = CreateSpritePart(
            capsuleRoot.transform,
            "CapsuleShadow",
            circleSprite,
            new Vector3(0.04f, -0.19f, 0f),
            new Vector3(0.78f, 0.20f, 1f),
            0f,
            new Color(0f, 0f, 0f, 0.28f),
            sortingLayerId,
            sortingOrder + 4,
            targetLayer
        );

        SpriteRenderer capsuleBottom = CreateSpritePart(
            capsuleRoot.transform,
            "CapsuleBottom",
            circleSprite,
            new Vector3(0f, -0.05f, 0f),
            new Vector3(0.56f, 0.46f, 1f),
            0f,
            itemColor,
            sortingLayerId,
            sortingOrder + 5,
            targetLayer
        );

        SpriteRenderer capsuleTop = CreateSpritePart(
            capsuleRoot.transform,
            "CapsuleTopPlastic",
            circleSprite,
            new Vector3(0f, 0.08f, 0f),
            new Vector3(0.56f, 0.42f, 1f),
            0f,
            new Color(0.82f, 0.96f, 1f, 0.58f),
            sortingLayerId,
            sortingOrder + 6,
            targetLayer
        );

        SpriteRenderer capsuleRim = CreateSpritePart(
            capsuleRoot.transform,
            "CapsuleRim",
            circleSprite,
            new Vector3(0f, 0.01f, 0f),
            new Vector3(0.60f, 0.085f, 1f),
            0f,
            new Color(1f, 1f, 1f, 0.68f),
            sortingLayerId,
            sortingOrder + 7,
            targetLayer
        );

        SpriteRenderer capsuleHighlight = CreateSpritePart(
            capsuleRoot.transform,
            "CapsuleHighlight",
            circleSprite,
            new Vector3(-0.15f, 0.15f, 0f),
            new Vector3(0.075f, 0.22f, 1f),
            -30f,
            new Color(1f, 1f, 1f, 0.66f),
            sortingLayerId,
            sortingOrder + 8,
            targetLayer
        );

        PixelTextGroup getShadowText = CreatePixelText(
            root.transform,
            "GetTextShadow",
            "GET!",
            new Vector3(0.035f, 0.745f, 0f),
            0.075f,
            1,
            new Color(0.08f, 0.06f, 0.00f, 0.88f),
            squareSprite,
            sortingLayerId,
            sortingOrder + 180,
            targetLayer
        );

        PixelTextGroup getText = CreatePixelText(
            root.transform,
            "GetText",
            "GET!",
            new Vector3(0f, 0.78f, 0f),
            0.075f,
            1,
            new Color(1f, 0.96f, 0.45f, 1f),
            squareSprite,
            sortingLayerId,
            sortingOrder + 181,
            targetLayer
        );

        PixelTextGroup itemShadowText = CreatePixelText(
            root.transform,
            "ItemTextShadow",
            itemName,
            new Vector3(0.025f, -0.465f, 0f),
            0.052f,
            1,
            new Color(0f, 0f, 0f, 0.88f),
            squareSprite,
            sortingLayerId,
            sortingOrder + 190,
            targetLayer
        );

        PixelTextGroup itemText = CreatePixelText(
            root.transform,
            "ItemText",
            itemName,
            new Vector3(0f, -0.44f, 0f),
            0.052f,
            1,
            new Color(1f, 1f, 1f, 1f),
            squareSprite,
            sortingLayerId,
            sortingOrder + 191,
            targetLayer
        );

        Vector3 startPosition = worldPosition;
        Vector3 baseScale = Vector3.one * scale;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
            float fade = 1f;

            if (t > 0.72f)
            {
                fade = 1f - Mathf.InverseLerp(0.72f, 1f, t);
            }

            float pop;

            if (t < 0.32f)
            {
                pop = Mathf.LerpUnclamped(0.20f, 1.18f, EaseOutBack(t / 0.32f));
            }
            else
            {
                pop = Mathf.Lerp(1.18f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.32f) / 0.68f));
            }

            float floatUp = Mathf.Sin(t * Mathf.PI) * 0.10f + t * 0.14f;

            root.transform.position = startPosition + new Vector3(0f, floatUp, 0f);
            root.transform.localScale = baseScale * pop;

            float capsuleRotate = Mathf.Lerp(-22f, 12f, t) + Mathf.Sin(t * Mathf.PI * 5f) * 4f * (1f - t);
            capsuleRoot.transform.localRotation = Quaternion.Euler(0f, 0f, capsuleRotate);

            float capsuleBounce = Mathf.Sin(t * Mathf.PI * 3f) * 0.035f * (1f - t);
            capsuleRoot.transform.localPosition = new Vector3(0f, 0.20f + capsuleBounce, 0f);

            float auraPulse = 1f + Mathf.Sin(Time.time * 9f) * 0.05f;
            auraBack.transform.localScale = new Vector3(1.45f, 1.45f, 1f) * auraPulse;
            auraRing.transform.localScale = new Vector3(1.12f, 1.12f, 1f) * (1f + t * 0.20f);

            SetAlpha(auraBack, 0.26f * fade);
            SetAlpha(auraRing, 0.20f * fade);
            SetAlpha(labelBack, 0.88f * fade);
            SetAlpha(labelInner, 0.14f * fade);
            SetAlpha(capsuleShadow, 0.28f * fade);
            SetAlpha(capsuleBottom, itemColor.a * fade);
            SetAlpha(capsuleTop, 0.58f * fade);
            SetAlpha(capsuleRim, 0.68f * fade);
            SetAlpha(capsuleHighlight, 0.66f * fade);

            SetPixelTextAlpha(getShadowText, 0.88f * fade);
            SetPixelTextAlpha(getText, fade);
            SetPixelTextAlpha(itemShadowText, 0.88f * fade);
            SetPixelTextAlpha(itemText, fade);

            for (int i = 0; i < rays.Length; i++)
            {
                if (rays[i] == null)
                {
                    continue;
                }

                float rayFade = Mathf.Clamp01(1f - t * 1.25f) * fade;
                SetAlpha(rays[i], 0.55f * rayFade);

                Vector3 rayDirection = rays[i].transform.localPosition.normalized;

                if (rayDirection == Vector3.zero)
                {
                    rayDirection = Vector3.up;
                }

                rays[i].transform.localPosition = rayDirection * Mathf.Lerp(0.32f, 0.82f, t);
            }

            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null)
                {
                    continue;
                }

                float dotFade = Mathf.Clamp01(1f - t * 1.1f) * fade;
                SetAlpha(dots[i], 0.72f * dotFade);

                Vector3 dotDirection = dots[i].transform.localPosition.normalized;

                if (dotDirection == Vector3.zero)
                {
                    dotDirection = Vector3.up;
                }

                dots[i].transform.localPosition = dotDirection * Mathf.Lerp(0.26f, 0.72f, t);
            }

            yield return null;
        }

        Object.Destroy(root);
    }

    private static PixelTextGroup CreatePixelText(
        Transform parent,
        string name,
        string text,
        Vector3 localPosition,
        float pixelSize,
        int characterSpacingColumns,
        Color color,
        Sprite squareSprite,
        int sortingLayerId,
        int sortingOrder,
        int targetLayer
    )
    {
        PixelTextGroup group = new PixelTextGroup();

        GameObject textRoot = new GameObject(name);
        textRoot.transform.SetParent(parent);
        textRoot.transform.localPosition = localPosition;
        textRoot.transform.localRotation = Quaternion.identity;
        textRoot.transform.localScale = Vector3.one;
        textRoot.layer = targetLayer;

        group.root = textRoot;

        int totalColumns = GetTextTotalColumns(text, characterSpacingColumns);
        int totalRows = 7;

        float totalWidth = totalColumns * pixelSize;
        float totalHeight = totalRows * pixelSize;

        float originX = -totalWidth * 0.5f + pixelSize * 0.5f;
        float originY = totalHeight * 0.5f - pixelSize * 0.5f;

        int cursorX = 0;

        for (int charIndex = 0; charIndex < text.Length; charIndex++)
        {
            char c = text[charIndex];
            string[] pattern = GetCharacterPattern(c);

            int patternWidth = pattern[0].Length;

            for (int y = 0; y < pattern.Length; y++)
            {
                for (int x = 0; x < pattern[y].Length; x++)
                {
                    if (pattern[y][x] == '0')
                    {
                        continue;
                    }

                    GameObject pixelObject = new GameObject("Pixel_" + c + "_" + x + "_" + y);
                    pixelObject.transform.SetParent(textRoot.transform);
                    pixelObject.transform.localPosition = new Vector3(
                        originX + (cursorX + x) * pixelSize,
                        originY - y * pixelSize,
                        0f
                    );
                    pixelObject.transform.localRotation = Quaternion.identity;
                    pixelObject.transform.localScale = new Vector3(pixelSize * 0.92f, pixelSize * 0.92f, 1f);
                    pixelObject.layer = targetLayer;

                    SpriteRenderer renderer = pixelObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = squareSprite;
                    renderer.color = color;
                    renderer.sortingLayerID = sortingLayerId;
                    renderer.sortingOrder = sortingOrder;

                    group.renderers.Add(renderer);
                }
            }

            cursorX += patternWidth;

            if (charIndex < text.Length - 1)
            {
                cursorX += characterSpacingColumns;
            }
        }

        return group;
    }

    private static int GetTextTotalColumns(string text, int characterSpacingColumns)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int totalColumns = 0;

        for (int i = 0; i < text.Length; i++)
        {
            string[] pattern = GetCharacterPattern(text[i]);
            totalColumns += pattern[0].Length;

            if (i < text.Length - 1)
            {
                totalColumns += characterSpacingColumns;
            }
        }

        return totalColumns;
    }

    private static string[] GetCharacterPattern(char c)
    {
        switch (char.ToUpperInvariant(c))
        {
            case 'A':
                return new string[]
                {
                    "01110",
                    "10001",
                    "10001",
                    "11111",
                    "10001",
                    "10001",
                    "10001"
                };

            case 'D':
                return new string[]
                {
                    "11110",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "11110"
                };

            case 'E':
                return new string[]
                {
                    "11111",
                    "10000",
                    "10000",
                    "11110",
                    "10000",
                    "10000",
                    "11111"
                };

            case 'F':
                return new string[]
                {
                    "11111",
                    "10000",
                    "10000",
                    "11110",
                    "10000",
                    "10000",
                    "10000"
                };

            case 'G':
                return new string[]
                {
                    "01110",
                    "10000",
                    "10000",
                    "10111",
                    "10001",
                    "10001",
                    "01110"
                };

            case 'I':
                return new string[]
                {
                    "111",
                    "010",
                    "010",
                    "010",
                    "010",
                    "010",
                    "111"
                };

            case 'L':
                return new string[]
                {
                    "10000",
                    "10000",
                    "10000",
                    "10000",
                    "10000",
                    "10000",
                    "11111"
                };

            case 'M':
                return new string[]
                {
                    "10001",
                    "11011",
                    "10101",
                    "10101",
                    "10001",
                    "10001",
                    "10001"
                };

            case 'O':
                return new string[]
                {
                    "01110",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "10001",
                    "01110"
                };

            case 'P':
                return new string[]
                {
                    "11110",
                    "10001",
                    "10001",
                    "11110",
                    "10000",
                    "10000",
                    "10000"
                };

            case 'R':
                return new string[]
                {
                    "11110",
                    "10001",
                    "10001",
                    "11110",
                    "10100",
                    "10010",
                    "10001"
                };

            case 'S':
                return new string[]
                {
                    "01111",
                    "10000",
                    "10000",
                    "01110",
                    "00001",
                    "00001",
                    "11110"
                };

            case 'T':
                return new string[]
                {
                    "11111",
                    "00100",
                    "00100",
                    "00100",
                    "00100",
                    "00100",
                    "00100"
                };

            case '!':
                return new string[]
                {
                    "1",
                    "1",
                    "1",
                    "1",
                    "1",
                    "0",
                    "1"
                };

            case ' ':
                return new string[]
                {
                    "000",
                    "000",
                    "000",
                    "000",
                    "000",
                    "000",
                    "000"
                };

            default:
                return new string[]
                {
                    "111",
                    "001",
                    "010",
                    "010",
                    "000",
                    "010",
                    "010"
                };
        }
    }

    private static void SetPixelTextAlpha(PixelTextGroup group, float alpha)
    {
        if (group == null || group.renderers == null)
        {
            return;
        }

        for (int i = 0; i < group.renderers.Count; i++)
        {
            SetAlpha(group.renderers[i], alpha);
        }
    }

    private static SpriteRenderer[] CreateBurstRays(
        Transform root,
        Sprite sprite,
        Color itemColor,
        int sortingLayerId,
        int sortingOrder,
        int targetLayer
    )
    {
        SpriteRenderer[] rays = new SpriteRenderer[12];

        for (int i = 0; i < rays.Length; i++)
        {
            float angle = i * (360f / rays.Length);
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            Vector3 position = direction * 0.38f;

            Color rayColor = itemColor;
            rayColor.a = 0.55f;

            rays[i] = CreateSpritePart(
                root,
                "BurstRay_" + i,
                sprite,
                position,
                new Vector3(0.045f, 0.24f, 1f),
                angle,
                rayColor,
                sortingLayerId,
                sortingOrder,
                targetLayer
            );
        }

        return rays;
    }

    private static SpriteRenderer[] CreateBurstDots(
        Transform root,
        Sprite sprite,
        Color itemColor,
        int sortingLayerId,
        int sortingOrder,
        int targetLayer
    )
    {
        SpriteRenderer[] dots = new SpriteRenderer[10];

        for (int i = 0; i < dots.Length; i++)
        {
            float angle = i * (360f / dots.Length) + 18f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            Vector3 position = direction * 0.34f;

            Color dotColor = i % 2 == 0 ? itemColor : Color.white;
            dotColor.a = 0.72f;

            float size = i % 2 == 0 ? 0.06f : 0.04f;

            dots[i] = CreateSpritePart(
                root,
                "BurstDot_" + i,
                sprite,
                position,
                new Vector3(size, size, 1f),
                0f,
                dotColor,
                sortingLayerId,
                sortingOrder,
                targetLayer
            );
        }

        return dots;
    }

    private static SpriteRenderer CreateSpritePart(
        Transform parent,
        string name,
        Sprite sprite,
        Vector3 localPosition,
        Vector3 localScale,
        float localRotationZ,
        Color color,
        int sortingLayerId,
        int sortingOrder,
        int targetLayer
    )
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = localScale;
        obj.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);
        obj.layer = targetLayer;

        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = sortingLayerId;
        renderer.sortingOrder = sortingOrder;

        return renderer;
    }

    private static void SetAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null)
        {
            return;
        }

        Color color = renderer.color;
        color.a = Mathf.Clamp01(alpha);
        renderer.color = color;
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);

        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static int GetSortingLayerId(string sortingLayerName)
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

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = CreateTransparentTexture(4, 4, "GachaGetEffectSquare");

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

    private static Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "GachaGetEffectCircle");

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

    private static Sprite CreateRoundedRectSprite(int size, int radius)
    {
        Texture2D texture = CreateTransparentTexture(size, size, "GachaGetEffectRoundedRect");

        float half = size * 0.5f;
        float rectHalf = half - 1f;
        float cornerRadius = Mathf.Max(1f, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float dx = Mathf.Abs(px - half);
                float dy = Mathf.Abs(py - half);

                bool insideCoreX = dx <= rectHalf - cornerRadius;
                bool insideCoreY = dy <= rectHalf - cornerRadius;

                bool inside = false;

                if (insideCoreX && dy <= rectHalf)
                {
                    inside = true;
                }
                else if (insideCoreY && dx <= rectHalf)
                {
                    inside = true;
                }
                else
                {
                    float cornerX = rectHalf - cornerRadius;
                    float cornerY = rectHalf - cornerRadius;

                    float distanceX = dx - cornerX;
                    float distanceY = dy - cornerY;

                    if (distanceX * distanceX + distanceY * distanceY <= cornerRadius * cornerRadius)
                    {
                        inside = true;
                    }
                }

                if (inside)
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

    private static Texture2D CreateTransparentTexture(int width, int height, string name)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.name = name;
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