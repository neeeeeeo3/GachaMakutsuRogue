using UnityEngine;

public class HeroVisualBuilder : MonoBehaviour
{
    public enum HeroVisualType
    {
        BraveHero,
        Knight,
        Mage,
        Ranger,
        HeavyWarrior,
        Thief
    }

    [Header("Hero Type")]
    public HeroVisualType heroVisualType = HeroVisualType.BraveHero;

    [Header("Visual Root")]
    public string visualRootName = "HeroVisual";
    public bool hideOriginalSpriteRenderer = true;
    public bool rebuildOnStart = true;

    [Header("Size")]
    public float visualScale = 1f;
    public int baseSortingOrder = 120;

    private Sprite squareSprite;
    private Sprite circleSprite;

    private Color capeColor;
    private Color armorColor;
    private Color armorShadowColor;
    private Color skinColor;
    private Color hairColor;
    private Color bootColor;
    private Color swordColor;
    private Color swordHandleColor;
    private Color shieldColor;
    private Color shieldMarkColor;
    private Color eyeColor;
    private Color robeColor;
    private Color accentColor;

    private void Start()
    {
        if (rebuildOnStart)
        {
            RebuildVisual();
        }
    }

    [ContextMenu("Rebuild Hero Visual")]
    public void RebuildVisual()
    {
        squareSprite = CreateSquareSprite();
        circleSprite = CreateCircleSprite(32);

        ApplyPalette();

        HideOriginalSpriteRenderer();
        ClearOldVisual();

        GameObject root = new GameObject(visualRootName);
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        if (heroVisualType == HeroVisualType.BraveHero)
        {
            BuildBraveHero(root.transform);
        }
        else if (heroVisualType == HeroVisualType.Knight)
        {
            BuildKnight(root.transform);
        }
        else if (heroVisualType == HeroVisualType.Mage)
        {
            BuildMage(root.transform);
        }
        else if (heroVisualType == HeroVisualType.Ranger)
        {
            BuildRanger(root.transform);
        }
        else if (heroVisualType == HeroVisualType.HeavyWarrior)
        {
            BuildHeavyWarrior(root.transform);
        }
        else if (heroVisualType == HeroVisualType.Thief)
        {
            BuildThief(root.transform);
        }
    }

    private void ApplyPalette()
    {
        skinColor = new Color(0.95f, 0.76f, 0.56f, 1f);
        eyeColor = Color.black;
        swordColor = new Color(0.86f, 0.9f, 0.96f, 1f);
        swordHandleColor = new Color(0.45f, 0.25f, 0.10f, 1f);
        bootColor = new Color(0.20f, 0.12f, 0.08f, 1f);

        if (heroVisualType == HeroVisualType.BraveHero)
        {
            capeColor = new Color(0.78f, 0.08f, 0.12f, 1f);
            armorColor = new Color(0.72f, 0.75f, 0.82f, 1f);
            armorShadowColor = new Color(0.42f, 0.45f, 0.52f, 1f);
            hairColor = new Color(0.95f, 0.78f, 0.18f, 1f);
            shieldColor = new Color(0.20f, 0.38f, 0.85f, 1f);
            shieldMarkColor = new Color(0.95f, 0.88f, 0.25f, 1f);
            robeColor = armorColor;
            accentColor = shieldMarkColor;
        }
        else if (heroVisualType == HeroVisualType.Knight)
        {
            capeColor = new Color(0.16f, 0.22f, 0.42f, 1f);
            armorColor = new Color(0.78f, 0.80f, 0.86f, 1f);
            armorShadowColor = new Color(0.38f, 0.42f, 0.50f, 1f);
            hairColor = new Color(0.50f, 0.36f, 0.18f, 1f);
            shieldColor = new Color(0.75f, 0.78f, 0.84f, 1f);
            shieldMarkColor = new Color(0.20f, 0.32f, 0.80f, 1f);
            robeColor = armorColor;
            accentColor = shieldMarkColor;
        }
        else if (heroVisualType == HeroVisualType.Mage)
        {
            capeColor = new Color(0.20f, 0.08f, 0.42f, 1f);
            armorColor = new Color(0.36f, 0.18f, 0.72f, 1f);
            armorShadowColor = new Color(0.18f, 0.08f, 0.36f, 1f);
            hairColor = new Color(0.88f, 0.88f, 0.95f, 1f);
            shieldColor = new Color(0.45f, 0.20f, 0.80f, 1f);
            shieldMarkColor = new Color(0.95f, 0.78f, 0.28f, 1f);
            robeColor = new Color(0.30f, 0.12f, 0.62f, 1f);
            accentColor = new Color(0.85f, 0.65f, 1f, 1f);
        }
        else if (heroVisualType == HeroVisualType.Ranger)
        {
            capeColor = new Color(0.12f, 0.38f, 0.18f, 1f);
            armorColor = new Color(0.30f, 0.48f, 0.24f, 1f);
            armorShadowColor = new Color(0.15f, 0.25f, 0.12f, 1f);
            hairColor = new Color(0.36f, 0.22f, 0.10f, 1f);
            shieldColor = new Color(0.25f, 0.35f, 0.18f, 1f);
            shieldMarkColor = new Color(0.70f, 0.90f, 0.32f, 1f);
            robeColor = armorColor;
            accentColor = shieldMarkColor;
        }
        else if (heroVisualType == HeroVisualType.HeavyWarrior)
        {
            capeColor = new Color(0.40f, 0.08f, 0.06f, 1f);
            armorColor = new Color(0.55f, 0.50f, 0.46f, 1f);
            armorShadowColor = new Color(0.22f, 0.20f, 0.19f, 1f);
            hairColor = new Color(0.16f, 0.12f, 0.10f, 1f);
            shieldColor = new Color(0.48f, 0.18f, 0.12f, 1f);
            shieldMarkColor = new Color(0.92f, 0.68f, 0.20f, 1f);
            robeColor = armorColor;
            accentColor = shieldMarkColor;
        }
        else if (heroVisualType == HeroVisualType.Thief)
        {
            capeColor = new Color(0.10f, 0.06f, 0.16f, 1f);
            armorColor = new Color(0.26f, 0.16f, 0.32f, 1f);
            armorShadowColor = new Color(0.06f, 0.04f, 0.10f, 1f);
            hairColor = new Color(0.12f, 0.08f, 0.06f, 1f);
            shieldColor = new Color(0.18f, 0.12f, 0.24f, 1f);
            shieldMarkColor = new Color(0.95f, 0.70f, 0.18f, 1f);
            robeColor = new Color(0.14f, 0.08f, 0.22f, 1f);
            accentColor = new Color(0.95f, 0.70f, 0.18f, 1f);
            swordColor = new Color(0.72f, 0.78f, 0.86f, 1f);
            swordHandleColor = new Color(0.20f, 0.12f, 0.08f, 1f);
            bootColor = new Color(0.06f, 0.04f, 0.05f, 1f);
        }
    }

    private void BuildBraveHero(Transform root)
    {
        BuildCommonBody(root, true, true);
        BuildSword(root, new Vector3(0.39f, 0.15f, 0f), -35f, 0.62f);
        BuildShield(root, new Vector3(-0.30f, -0.02f, 0f), 0.34f, 0.42f);
    }

    private void BuildKnight(Transform root)
    {
        BuildCape(root, 0.55f, 0.72f);
        BuildBody(root, 0.60f, 0.66f);
        BuildHelmet(root);
        BuildEyes(root, 0.30f);
        BuildBigShield(root);
        BuildSword(root, new Vector3(0.36f, 0.08f, 0f), -25f, 0.55f);
        BuildFeet(root);
    }

    private void BuildMage(Transform root)
    {
        BuildCape(root, 0.58f, 0.82f);

        CreatePart(root, "Robe", circleSprite,
            new Vector3(0f, -0.10f, 0f),
            new Vector3(0.56f, 0.76f, 1f),
            0f,
            robeColor,
            baseSortingOrder
        );

        CreatePart(root, "RobeStripe", squareSprite,
            new Vector3(0f, -0.12f, 0f),
            new Vector3(0.10f, 0.58f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 1
        );

        BuildHead(root);
        BuildWizardHat(root);
        BuildEyes(root, 0.34f);
        BuildStaff(root);
        BuildFeet(root);
    }

    private void BuildRanger(Transform root)
    {
        BuildCape(root, 0.50f, 0.70f);
        BuildBody(root, 0.46f, 0.58f);
        BuildHood(root);
        BuildEyes(root, 0.34f);
        BuildBow(root);
        BuildFeet(root);
    }

    private void BuildHeavyWarrior(Transform root)
    {
        BuildCape(root, 0.68f, 0.78f);
        BuildBody(root, 0.70f, 0.72f);
        BuildHead(root);
        BuildHair(root);
        BuildEyes(root, 0.34f);
        BuildHugeAxe(root);
        BuildHeavyShoulders(root);
        BuildFeet(root);
    }

    private void BuildThief(Transform root)
    {
        BuildThiefCape(root);
        BuildThiefBody(root);
        BuildThiefHood(root);
        BuildThiefEyes(root);
        BuildTwinDaggers(root);
        BuildGoldPouch(root);
        BuildThiefFeet(root);
    }

    private void BuildCommonBody(Transform root, bool withCape, bool withHair)
    {
        if (withCape)
        {
            BuildCape(root, 0.70f, 0.82f);
        }

        BuildBody(root, 0.52f, 0.62f);
        BuildHead(root);

        if (withHair)
        {
            BuildHair(root);
        }

        BuildEyes(root, 0.34f);
        BuildFeet(root);
    }

    private void BuildCape(Transform root, float width, float height)
    {
        CreatePart(root, "Cape", circleSprite,
            new Vector3(-0.05f, -0.05f, 0f),
            new Vector3(width, height, 1f),
            0f,
            capeColor,
            baseSortingOrder - 3
        );

        CreatePart(root, "CapeTail", squareSprite,
            new Vector3(-0.03f, -0.35f, 0f),
            new Vector3(width * 0.65f, 0.38f, 1f),
            0f,
            capeColor,
            baseSortingOrder - 4
        );
    }

    private void BuildBody(Transform root, float width, float height)
    {
        CreatePart(root, "BodyArmor", circleSprite,
            new Vector3(0f, -0.05f, 0f),
            new Vector3(width, height, 1f),
            0f,
            armorColor,
            baseSortingOrder
        );

        CreatePart(root, "ArmorBelt", squareSprite,
            new Vector3(0f, -0.22f, 0f),
            new Vector3(width * 0.90f, 0.08f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 1
        );
    }

    private void BuildHead(Transform root)
    {
        CreatePart(root, "Head", circleSprite,
            new Vector3(0f, 0.34f, 0f),
            new Vector3(0.42f, 0.42f, 1f),
            0f,
            skinColor,
            baseSortingOrder + 3
        );
    }

    private void BuildHair(Transform root)
    {
        CreatePart(root, "Hair", circleSprite,
            new Vector3(-0.02f, 0.47f, 0f),
            new Vector3(0.46f, 0.25f, 1f),
            0f,
            hairColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "HairBangLeft", squareSprite,
            new Vector3(-0.12f, 0.39f, 0f),
            new Vector3(0.12f, 0.18f, 1f),
            -25f,
            hairColor,
            baseSortingOrder + 5
        );

        CreatePart(root, "HairBangRight", squareSprite,
            new Vector3(0.10f, 0.39f, 0f),
            new Vector3(0.10f, 0.16f, 1f),
            20f,
            hairColor,
            baseSortingOrder + 5
        );
    }

    private void BuildEyes(Transform root, float y)
    {
        CreatePart(root, "EyeLeft", squareSprite,
            new Vector3(-0.08f, y, 0f),
            new Vector3(0.045f, 0.055f, 1f),
            0f,
            eyeColor,
            baseSortingOrder + 6
        );

        CreatePart(root, "EyeRight", squareSprite,
            new Vector3(0.08f, y, 0f),
            new Vector3(0.045f, 0.055f, 1f),
            0f,
            eyeColor,
            baseSortingOrder + 6
        );
    }

    private void BuildFeet(Transform root)
    {
        CreatePart(root, "FootLeft", squareSprite,
            new Vector3(-0.13f, -0.43f, 0f),
            new Vector3(0.16f, 0.12f, 1f),
            0f,
            bootColor,
            baseSortingOrder - 1
        );

        CreatePart(root, "FootRight", squareSprite,
            new Vector3(0.13f, -0.43f, 0f),
            new Vector3(0.16f, 0.12f, 1f),
            0f,
            bootColor,
            baseSortingOrder - 1
        );
    }

    private void BuildSword(Transform root, Vector3 position, float rotation, float bladeLength)
    {
        CreatePart(root, "SwordBlade", squareSprite,
            position,
            new Vector3(0.09f, bladeLength, 1f),
            rotation,
            swordColor,
            baseSortingOrder + 2
        );

        CreatePart(root, "SwordHandle", squareSprite,
            position + new Vector3(-0.14f, -0.20f, 0f),
            new Vector3(0.08f, 0.24f, 1f),
            rotation,
            swordHandleColor,
            baseSortingOrder + 3
        );
    }

    private void BuildShield(Transform root, Vector3 position, float width, float height)
    {
        CreatePart(root, "Shield", circleSprite,
            position,
            new Vector3(width, height, 1f),
            0f,
            shieldColor,
            baseSortingOrder + 3
        );

        CreatePart(root, "ShieldMark", squareSprite,
            position + new Vector3(0f, 0.02f, 0f),
            new Vector3(0.08f, 0.25f, 1f),
            0f,
            shieldMarkColor,
            baseSortingOrder + 4
        );
    }

    private void BuildBigShield(Transform root)
    {
        BuildShield(root, new Vector3(-0.34f, -0.08f, 0f), 0.42f, 0.58f);
    }

    private void BuildHelmet(Transform root)
    {
        BuildHead(root);

        CreatePart(root, "Helmet", circleSprite,
            new Vector3(0f, 0.43f, 0f),
            new Vector3(0.48f, 0.30f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 5
        );

        CreatePart(root, "HelmetCrest", squareSprite,
            new Vector3(0f, 0.60f, 0f),
            new Vector3(0.10f, 0.22f, 1f),
            0f,
            capeColor,
            baseSortingOrder + 6
        );
    }

    private void BuildWizardHat(Transform root)
    {
        CreatePart(root, "HatBrim", squareSprite,
            new Vector3(0f, 0.53f, 0f),
            new Vector3(0.56f, 0.09f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 6
        );

        CreatePart(root, "HatTop", squareSprite,
            new Vector3(0.02f, 0.70f, 0f),
            new Vector3(0.24f, 0.34f, 1f),
            -12f,
            robeColor,
            baseSortingOrder + 7
        );

        CreatePart(root, "HatGem", circleSprite,
            new Vector3(0.09f, 0.73f, 0f),
            new Vector3(0.08f, 0.08f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 8
        );
    }

    private void BuildHood(Transform root)
    {
        CreatePart(root, "Hood", circleSprite,
            new Vector3(0f, 0.39f, 0f),
            new Vector3(0.50f, 0.46f, 1f),
            0f,
            capeColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "FaceOpening", circleSprite,
            new Vector3(0f, 0.34f, 0f),
            new Vector3(0.34f, 0.32f, 1f),
            0f,
            skinColor,
            baseSortingOrder + 5
        );
    }

    private void BuildBow(Transform root)
    {
        CreatePart(root, "BowUpper", squareSprite,
            new Vector3(0.34f, 0.12f, 0f),
            new Vector3(0.07f, 0.38f, 1f),
            18f,
            swordHandleColor,
            baseSortingOrder + 3
        );

        CreatePart(root, "BowLower", squareSprite,
            new Vector3(0.34f, -0.17f, 0f),
            new Vector3(0.07f, 0.38f, 1f),
            -18f,
            swordHandleColor,
            baseSortingOrder + 3
        );

        CreatePart(root, "BowString", squareSprite,
            new Vector3(0.43f, -0.02f, 0f),
            new Vector3(0.025f, 0.60f, 1f),
            0f,
            swordColor,
            baseSortingOrder + 4
        );
    }

    private void BuildStaff(Transform root)
    {
        CreatePart(root, "Staff", squareSprite,
            new Vector3(0.36f, 0.00f, 0f),
            new Vector3(0.06f, 0.82f, 1f),
            -12f,
            swordHandleColor,
            baseSortingOrder + 2
        );

        CreatePart(root, "StaffOrb", circleSprite,
            new Vector3(0.45f, 0.39f, 0f),
            new Vector3(0.18f, 0.18f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 4
        );
    }

    private void BuildHugeAxe(Transform root)
    {
        CreatePart(root, "AxeHandle", squareSprite,
            new Vector3(0.36f, 0.03f, 0f),
            new Vector3(0.07f, 0.78f, 1f),
            -28f,
            swordHandleColor,
            baseSortingOrder + 2
        );

        CreatePart(root, "AxeHead", circleSprite,
            new Vector3(0.52f, 0.34f, 0f),
            new Vector3(0.34f, 0.28f, 1f),
            0f,
            swordColor,
            baseSortingOrder + 3
        );
    }

    private void BuildHeavyShoulders(Transform root)
    {
        CreatePart(root, "ShoulderLeft", circleSprite,
            new Vector3(-0.28f, 0.09f, 0f),
            new Vector3(0.24f, 0.24f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 3
        );

        CreatePart(root, "ShoulderRight", circleSprite,
            new Vector3(0.28f, 0.09f, 0f),
            new Vector3(0.24f, 0.24f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 3
        );
    }

    private void BuildThiefCape(Transform root)
    {
        CreatePart(root, "ThiefCape", circleSprite,
            new Vector3(0f, -0.06f, 0f),
            new Vector3(0.54f, 0.76f, 1f),
            0f,
            capeColor,
            baseSortingOrder - 4
        );

        CreatePart(root, "ThiefCapeTailLeft", squareSprite,
            new Vector3(-0.10f, -0.40f, 0f),
            new Vector3(0.18f, 0.30f, 1f),
            12f,
            capeColor,
            baseSortingOrder - 5
        );

        CreatePart(root, "ThiefCapeTailRight", squareSprite,
            new Vector3(0.10f, -0.40f, 0f),
            new Vector3(0.18f, 0.30f, 1f),
            -12f,
            capeColor,
            baseSortingOrder - 5
        );
    }

    private void BuildThiefBody(Transform root)
    {
        CreatePart(root, "ThiefBody", circleSprite,
            new Vector3(0f, -0.07f, 0f),
            new Vector3(0.44f, 0.58f, 1f),
            0f,
            armorColor,
            baseSortingOrder
        );

        CreatePart(root, "ThiefBelt", squareSprite,
            new Vector3(0f, -0.22f, 0f),
            new Vector3(0.48f, 0.08f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 2
        );

        CreatePart(root, "ThiefChestStrap", squareSprite,
            new Vector3(0.02f, -0.05f, 0f),
            new Vector3(0.08f, 0.56f, 1f),
            -38f,
            swordHandleColor,
            baseSortingOrder + 2
        );
    }

    private void BuildThiefHood(Transform root)
    {
        CreatePart(root, "ThiefHood", circleSprite,
            new Vector3(0f, 0.38f, 0f),
            new Vector3(0.48f, 0.44f, 1f),
            0f,
            robeColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "ThiefFace", circleSprite,
            new Vector3(0f, 0.32f, 0f),
            new Vector3(0.32f, 0.28f, 1f),
            0f,
            skinColor,
            baseSortingOrder + 5
        );

        CreatePart(root, "ThiefMask", squareSprite,
            new Vector3(0f, 0.34f, 0f),
            new Vector3(0.32f, 0.09f, 1f),
            0f,
            armorShadowColor,
            baseSortingOrder + 6
        );

        CreatePart(root, "ThiefHoodTip", squareSprite,
            new Vector3(0.05f, 0.58f, 0f),
            new Vector3(0.18f, 0.22f, 1f),
            -22f,
            robeColor,
            baseSortingOrder + 5
        );
    }

    private void BuildThiefEyes(Transform root)
    {
        CreatePart(root, "ThiefEyeLeft", squareSprite,
            new Vector3(-0.08f, 0.35f, 0f),
            new Vector3(0.045f, 0.045f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 7
        );

        CreatePart(root, "ThiefEyeRight", squareSprite,
            new Vector3(0.08f, 0.35f, 0f),
            new Vector3(0.045f, 0.045f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 7
        );
    }

    private void BuildTwinDaggers(Transform root)
    {
        CreatePart(root, "DaggerRightBlade", squareSprite,
            new Vector3(0.32f, 0.03f, 0f),
            new Vector3(0.06f, 0.34f, 1f),
            -38f,
            swordColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "DaggerRightHandle", squareSprite,
            new Vector3(0.22f, -0.11f, 0f),
            new Vector3(0.06f, 0.16f, 1f),
            -38f,
            swordHandleColor,
            baseSortingOrder + 5
        );

        CreatePart(root, "DaggerLeftBlade", squareSprite,
            new Vector3(-0.31f, 0.00f, 0f),
            new Vector3(0.06f, 0.30f, 1f),
            38f,
            swordColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "DaggerLeftHandle", squareSprite,
            new Vector3(-0.21f, -0.12f, 0f),
            new Vector3(0.06f, 0.15f, 1f),
            38f,
            swordHandleColor,
            baseSortingOrder + 5
        );
    }

    private void BuildGoldPouch(Transform root)
    {
        CreatePart(root, "GoldPouch", circleSprite,
            new Vector3(0.18f, -0.26f, 0f),
            new Vector3(0.13f, 0.13f, 1f),
            0f,
            accentColor,
            baseSortingOrder + 4
        );

        CreatePart(root, "PouchString", squareSprite,
            new Vector3(0.18f, -0.19f, 0f),
            new Vector3(0.10f, 0.025f, 1f),
            0f,
            swordHandleColor,
            baseSortingOrder + 5
        );
    }

    private void BuildThiefFeet(Transform root)
    {
        CreatePart(root, "ThiefFootLeft", squareSprite,
            new Vector3(-0.12f, -0.43f, 0f),
            new Vector3(0.14f, 0.10f, 1f),
            -8f,
            bootColor,
            baseSortingOrder - 1
        );

        CreatePart(root, "ThiefFootRight", squareSprite,
            new Vector3(0.12f, -0.43f, 0f),
            new Vector3(0.14f, 0.10f, 1f),
            8f,
            bootColor,
            baseSortingOrder - 1
        );
    }

    private void HideOriginalSpriteRenderer()
    {
        if (!hideOriginalSpriteRenderer)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    private void ClearOldVisual()
    {
        Transform oldRoot = transform.Find(visualRootName);

        if (oldRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(oldRoot.gameObject);
        }
        else
        {
            DestroyImmediate(oldRoot.gameObject);
        }
    }

    private void CreatePart(
        Transform parent,
        string partName,
        Sprite sprite,
        Vector3 localPosition,
        Vector3 localScale,
        float localRotationZ,
        Color color,
        int sortingOrder
    )
    {
        GameObject partObject = new GameObject(partName);
        partObject.transform.SetParent(parent);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localScale = localScale;
        partObject.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

        SpriteRenderer spriteRenderer = partObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.name = "HeroVisualSquareTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        texture.name = "HeroVisualCircleTexture";

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
                else
                {
                    texture.SetPixel(x, y, Color.clear);
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
}