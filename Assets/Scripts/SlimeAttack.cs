using UnityEngine;

public class SlimeAttack : MonoBehaviour
{
    [Header("Attack")]
    public int attackDamage = 1;
    public float attackInterval = 0.8f;

    [Header("Rarity Attack Bonus")]
    public bool applyRarityAttackIntervalBonus = true;

    [Tooltip("RARE SLIMEの攻撃間隔倍率です。0.9なら少し速くなります。")]
    public float rareAttackIntervalMultiplier = 0.9f;

    [Tooltip("EPIC SLIMEの攻撃間隔倍率です。0.72ならかなり速くなります。")]
    public float epicAttackIntervalMultiplier = 0.72f;

    [Tooltip("攻撃間隔の下限です。速くなりすぎ防止。")]
    public float minimumAttackInterval = 0.35f;

    [Tooltip("ONにすると、レアリティ攻撃速度補正のログを出します。")]
    public bool showRarityDebugLog = true;

    [Header("Grid Rule")]
    public bool allowSameCellAttack = true;

    [Tooltip("ON推奨。同じマスか上下左右1マスだけ攻撃します。斜め攻撃を防ぎます。")]
    public bool attackOnlySameOrOrthogonalAdjacent = true;

    [Header("World Distance Safety")]
    [Tooltip("ON推奨。グリッド上は隣でも、ワールド距離が遠すぎる時は攻撃しません。")]
    public bool useWorldDistanceGate = true;

    [Tooltip("同じマス扱いの時、この距離以内なら攻撃します。")]
    public float sameCellMaxWorldDistance = 0.85f;

    [Tooltip("上下左右1マスの時、隣マス中心間距離にこの倍率をかけた距離以内なら攻撃します。")]
    public float adjacentDistanceMultiplier = 1.35f;

    [Tooltip("隣マス中心距離が取れない時の保険距離です。")]
    public float fallbackAdjacentMaxWorldDistance = 1.35f;

    [Header("Optional Strict Checks")]
    [Tooltip("最初はOFF推奨。ONにするとヒーローがマス中心付近に来るまで攻撃しません。")]
    public bool useOptionalCenterCheck = false;

    public float slimeCenterTolerance = 0.35f;
    public float heroCenterTolerance = 0.35f;

    [Tooltip("最初はOFF推奨。ONにするとスライムとヒーローのいるマスがFloorの時だけ攻撃します。")]
    public bool requireFloorTileCheck = false;

    [Header("Phase")]
    public bool attackOnlyDuringHeroDefense = true;

    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Debug")]
    public bool showDebugLog = false;

    private float nextAttackTime;
    private bool rarityAttackBonusApplied;
    private GachaRarityHolder rarityHolder;

    private void Awake()
    {
        rarityHolder = GetComponent<GachaRarityHolder>();
    }

    private void Start()
    {
        AutoFindReferences();
        ApplyRarityAttackBonusIfNeeded();
    }

    private void Update()
    {
        ApplyRarityAttackBonusIfNeeded();

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (!CanAttackNow())
        {
            return;
        }

        HeroHealth targetHero = FindAttackableHero();

        if (targetHero == null)
        {
            return;
        }

        AttackHero(targetHero);
        nextAttackTime = Time.time + attackInterval;
    }

    private void ApplyRarityAttackBonusIfNeeded()
    {
        if (!applyRarityAttackIntervalBonus)
        {
            return;
        }

        if (rarityAttackBonusApplied)
        {
            return;
        }

        if (rarityHolder == null)
        {
            rarityHolder = GetComponent<GachaRarityHolder>();
        }

        if (rarityHolder == null)
        {
            return;
        }

        float originalInterval = attackInterval;

        switch (rarityHolder.rarity)
        {
            case GachaRarityType.Rare:
                attackInterval *= rareAttackIntervalMultiplier;
                break;

            case GachaRarityType.Epic:
                attackInterval *= epicAttackIntervalMultiplier;
                break;
        }

        if (attackInterval < minimumAttackInterval)
        {
            attackInterval = minimumAttackInterval;
        }

        rarityAttackBonusApplied = true;

        if (showRarityDebugLog)
        {
            Debug.Log(
                "SlimeAttack rarity bonus applied. "
                + rarityHolder.displayName
                + " interval "
                + originalInterval.ToString("0.00")
                + " -> "
                + attackInterval.ToString("0.00")
                + " damage="
                + attackDamage
            );
        }
    }

    private bool CanAttackNow()
    {
        if (!attackOnlyDuringHeroDefense)
        {
            return true;
        }

        if (RunManager.Instance == null)
        {
            DebugReason("blocked: RunManager not found");
            return false;
        }

        if (RunManager.Instance.isGameOver)
        {
            DebugReason("blocked: game over");
            return false;
        }

        if (RunManager.Instance.IsDungeonBuildPhase())
        {
            DebugReason("blocked: dungeon build phase");
            return false;
        }

        return true;
    }

    private HeroHealth FindAttackableHero()
    {
        AutoFindReferences();

        HeroHealth[] heroes = FindObjectsByType<HeroHealth>(FindObjectsSortMode.None);

        HeroHealth bestHero = null;
        float bestWorldDistance = float.MaxValue;

        for (int i = 0; i < heroes.Length; i++)
        {
            HeroHealth hero = heroes[i];

            if (hero == null)
            {
                continue;
            }

            if (!hero.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!IsHeroAttackable(hero, out float worldDistance))
            {
                continue;
            }

            if (worldDistance < bestWorldDistance)
            {
                bestWorldDistance = worldDistance;
                bestHero = hero;
            }
        }

        return bestHero;
    }

    private bool IsHeroAttackable(HeroHealth hero, out float worldDistance)
    {
        worldDistance = float.MaxValue;

        if (hero == null)
        {
            return false;
        }

        if (!TryGetGridCell(transform.position, out Vector2Int slimeCell))
        {
            DebugReason("blocked: slime grid cell not found");
            return false;
        }

        if (!TryGetGridCell(hero.transform.position, out Vector2Int heroCell))
        {
            DebugReason("blocked: hero grid cell not found");
            return false;
        }

        Vector2Int diff = heroCell - slimeCell;

        int absX = Mathf.Abs(diff.x);
        int absY = Mathf.Abs(diff.y);
        int manhattanDistance = absX + absY;

        bool isSameCell = manhattanDistance == 0;
        bool isOrthogonalAdjacent = manhattanDistance == 1 && (absX == 1 || absY == 1);
        bool isDiagonal = absX > 0 && absY > 0;

        if (isDiagonal)
        {
            DebugReason("blocked: diagonal target. slime=" + slimeCell + " hero=" + heroCell);
            return false;
        }

        if (attackOnlySameOrOrthogonalAdjacent)
        {
            if (!isSameCell && !isOrthogonalAdjacent)
            {
                DebugReason("blocked: not same or adjacent. slime=" + slimeCell + " hero=" + heroCell);
                return false;
            }
        }

        if (isSameCell && !allowSameCellAttack)
        {
            DebugReason("blocked: same cell attack disabled");
            return false;
        }

        if (requireFloorTileCheck)
        {
            if (!AreBothCellsFloor(slimeCell, heroCell))
            {
                DebugReason("blocked: floor tile check failed. slime=" + slimeCell + " hero=" + heroCell);
                return false;
            }
        }

        if (useOptionalCenterCheck)
        {
            if (!IsNearCellCenter(transform.position, slimeCell, slimeCenterTolerance, out float slimeCenterDistance))
            {
                DebugReason("blocked: slime not near cell center. distance=" + slimeCenterDistance);
                return false;
            }

            if (!IsNearCellCenter(hero.transform.position, heroCell, heroCenterTolerance, out float heroCenterDistance))
            {
                DebugReason("blocked: hero not near cell center. distance=" + heroCenterDistance);
                return false;
            }
        }

        worldDistance = GetWorldDistance2D(transform.position, hero.transform.position);

        if (useWorldDistanceGate)
        {
            if (isSameCell)
            {
                if (worldDistance > sameCellMaxWorldDistance)
                {
                    DebugReason("blocked: same cell but too far. worldDistance=" + worldDistance);
                    return false;
                }
            }
            else if (isOrthogonalAdjacent)
            {
                float allowedDistance = GetAllowedAdjacentWorldDistance(slimeCell, heroCell);

                if (worldDistance > allowedDistance)
                {
                    DebugReason(
                        "blocked: adjacent but too far. worldDistance="
                        + worldDistance
                        + " allowed="
                        + allowedDistance
                    );

                    return false;
                }
            }
        }

        return true;
    }

    private float GetAllowedAdjacentWorldDistance(Vector2Int slimeCell, Vector2Int heroCell)
    {
        if (TryGetCellCenterWorldPosition(slimeCell, out Vector3 slimeCenter)
            && TryGetCellCenterWorldPosition(heroCell, out Vector3 heroCenter))
        {
            float cellCenterDistance = GetWorldDistance2D(slimeCenter, heroCenter);
            return Mathf.Max(0.01f, cellCenterDistance * adjacentDistanceMultiplier);
        }

        return fallbackAdjacentMaxWorldDistance;
    }

    private bool AreBothCellsFloor(Vector2Int slimeCell, Vector2Int heroCell)
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        DungeonTile slimeTile = dungeonGridManager.GetTileAtGridPosition(slimeCell);
        DungeonTile heroTile = dungeonGridManager.GetTileAtGridPosition(heroCell);

        if (slimeTile == null || heroTile == null)
        {
            return false;
        }

        if (!slimeTile.IsFloor)
        {
            return false;
        }

        if (!heroTile.IsFloor)
        {
            return false;
        }

        return true;
    }

    private bool IsNearCellCenter(
        Vector3 objectWorldPosition,
        Vector2Int cell,
        float tolerance,
        out float distance
    )
    {
        distance = float.MaxValue;

        if (!TryGetCellCenterWorldPosition(cell, out Vector3 cellCenter))
        {
            return false;
        }

        distance = GetWorldDistance2D(objectWorldPosition, cellCenter);
        return distance <= tolerance;
    }

    private bool TryGetCellCenterWorldPosition(Vector2Int cell, out Vector3 centerWorldPosition)
    {
        centerWorldPosition = Vector3.zero;

        if (dungeonGridManager == null)
        {
            return false;
        }

        DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(cell);

        if (tile == null)
        {
            return false;
        }

        centerWorldPosition = tile.transform.position;
        centerWorldPosition.z = 0f;

        return true;
    }

    private bool TryGetGridCell(Vector3 worldPosition, out Vector2Int cell)
    {
        cell = Vector2Int.zero;

        AutoFindReferences();

        if (dungeonGridManager == null)
        {
            return false;
        }

        bool success = dungeonGridManager.TryGetGridPositionFromWorldPosition(
            worldPosition,
            out int x,
            out int y
        );

        if (!success)
        {
            return false;
        }

        cell = new Vector2Int(x, y);
        return true;
    }

    private float GetWorldDistance2D(Vector3 a, Vector3 b)
    {
        a.z = 0f;
        b.z = 0f;

        return Vector3.Distance(a, b);
    }

    private void AttackHero(HeroHealth hero)
    {
        if (hero == null)
        {
            return;
        }

        hero.SendMessage(
            "TakeDamage",
            attackDamage,
            SendMessageOptions.DontRequireReceiver
        );

        if (showDebugLog)
        {
            TryGetGridCell(transform.position, out Vector2Int slimeCell);
            TryGetGridCell(hero.transform.position, out Vector2Int heroCell);

            Debug.Log(
                "Slime attacked hero. "
                + GetRarityLogPrefix()
                + " Damage="
                + attackDamage
                + " Interval="
                + attackInterval.ToString("0.00")
                + " SlimeCell="
                + slimeCell
                + " HeroCell="
                + heroCell
                + " WorldDistance="
                + GetWorldDistance2D(transform.position, hero.transform.position)
            );
        }
    }

    private string GetRarityLogPrefix()
    {
        if (rarityHolder == null)
        {
            rarityHolder = GetComponent<GachaRarityHolder>();
        }

        if (rarityHolder == null)
        {
            return "[COMMON SLIME]";
        }

        switch (rarityHolder.rarity)
        {
            case GachaRarityType.Rare:
                return "[RARE SLIME]";

            case GachaRarityType.Epic:
                return "[EPIC SLIME]";
        }

        return "[COMMON SLIME]";
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
            return;
        }

        dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
    }

    private void DebugReason(string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.Log("SlimeAttack " + message);
    }
}