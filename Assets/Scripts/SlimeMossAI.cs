using System.Collections.Generic;
using UnityEngine;

public class SlimeMossAI : MonoBehaviour
{
    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public WaveManager waveManager;
    public bool autoFindDungeonGridManager = true;
    public bool autoFindWaveManager = true;

    [Header("Movement Phase")]
    public bool moveDuringDungeonBuildPhase = true;
    public bool moveDuringHeroDefensePhase = true;
    public bool moveDuringOtherPhases = false;

    [Header("Movement")]
    public float moveSpeed = 0.75f;
    public float stepWaitMin = 0.00f;
    public float stepWaitMax = 0.06f;
    public bool snapToCellCenterOnStart = true;

    [Header("Smooth Movement")]
    public bool useSmoothCellMovement = true;
    public AnimationCurve moveEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float minimumMoveDuration = 0.08f;

    [Header("Combat Hold")]
    public bool stayStillWhileHeroInAttackRange = true;
    public bool finishCurrentStepBeforeCombatHold = true;
    public int combatHoldGridRange = 1;
    public bool allowSameCellCombatHold = true;
    public float combatHoldAfterHeroLeaves = 0.25f;

    [Header("Moss-Like Rule")]
    public bool continueForwardIfPossible = true;
    public bool avoidBacktrackingWhenPossible = true;
    public bool randomizeInitialDirection = true;

    [Header("Food")]
    public bool eatFood = true;

    [Tooltip("OFF推奨。ONでも、実際に重なっていない餌は食べません。")]
    public bool eatAdjacentFood = false;

    public bool eatOnlyWhenNotMoving = true;
    public float eatCooldown = 1.2f;

    [Header("Food Overlap")]
    public bool moveOntoAdjacentFoodBeforeEating = true;
    public float foodOverlapDistance = 0.08f;
    public bool snapToFoodPositionWhenEating = true;

    [Header("Food Rarity Effects")]
    public bool useFoodItemReproductionCount = true;

    [Tooltip("SlimeFoodItemが見つからない/無効な時の保険増殖数です。")]
    public int fallbackReproductionCount = 1;

    [Tooltip("ONにすると、FOODのレアリティ効果ログを出します。")]
    public bool debugLogFoodRarityEffect = true;

    [Header("Reproduce")]
    public bool reproduceWhenEat = true;
    public int maxSlimeCount = 24;

    [Header("Anti Wall Embed")]
    public bool preferSpawnAdjacentCell = true;
    public bool avoidSpawnOnOtherSlime = true;
    public bool snapSpawnedSlimeToCellCenter = true;
    public bool rescueIfInsideWall = true;
    public int safeSpawnSearchRadius = 2;
    public int rescueSearchRadius = 3;

    [Header("Motion Feel")]
    public bool wobbleWhileMoving = true;
    public float wobbleAmount = 0.035f;
    public float wobbleSpeed = 7f;

    public bool squashOnArrival = true;
    public float arrivalSquashAmount = 0.10f;
    public float arrivalSquashDuration = 0.10f;

    [Header("Debug")]
    public bool debugLogChoices = false;
    public bool debugLogPhaseStop = false;
    public bool debugLogRescue = false;
    public bool debugLogCombatHold = false;
    public bool debugLogFood = false;

    private readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private Vector2Int currentCell;
    private Vector2Int previousCell;
    private Vector2Int currentDirection;
    private Vector2Int targetCell;

    private bool hasCurrentCell;
    private bool hasPreviousCell;
    private bool hasMoveTarget;

    private Vector3 moveStartPosition;
    private Vector3 moveEndPosition;
    private float moveTimer;
    private float moveDuration;

    private float stepWaitTimer;
    private float eatCooldownTimer;
    private float arrivalSquashTimer;
    private float combatHoldTimer;

    private Vector3 baseScale;
    private bool isMoving;

    private SlimeFoodItem reservedFoodTarget;

    private void Start()
    {
        AutoFindReferences();

        baseScale = transform.localScale;

        if (!TryInitializeCurrentCell())
        {
            Debug.LogWarning("SlimeMossAI could not find a floor cell under slime.");
            return;
        }

        if (snapToCellCenterOnStart && dungeonGridManager != null)
        {
            SnapToCellCenter(currentCell);
        }

        PickInitialDirection();
        ResetStepWait();
    }

    private void OnDisable()
    {
        ReleaseReservedFood();
        RestoreScale();
    }

    private void Update()
    {
        AutoFindReferences();
        ValidateReservedFood();

        eatCooldownTimer -= Time.deltaTime;

        if (!CanMoveNow())
        {
            StopCurrentMoveWithoutChangingCell();
            return;
        }

        if (!hasCurrentCell)
        {
            if (!TryInitializeCurrentCell())
            {
                return;
            }
        }

        if (rescueIfInsideWall && !hasMoveTarget)
        {
            RescueIfStandingOnInvalidCell();
        }

        UpdateCombatHold();

        if (IsCombatHoldActive())
        {
            HandleCombatHold();
            UpdateMotionFeel();
            return;
        }

        if (eatFood && CanEatAtThisMoment())
        {
            bool ateFood = TryEatFoodAtCurrentCell();

            if (!ateFood && !hasMoveTarget && moveOntoAdjacentFoodBeforeEating)
            {
                TryMoveOntoAdjacentFood();
            }
        }

        if (hasMoveTarget)
        {
            MoveToTargetCellSmooth();
        }
        else
        {
            stepWaitTimer -= Time.deltaTime;

            if (stepWaitTimer <= 0f)
            {
                DecideNextMove();
            }
        }

        UpdateMotionFeel();
    }

    private bool CanMoveNow()
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        if (RunManager.Instance == null)
        {
            return true;
        }

        if (RunManager.Instance.isGameOver)
        {
            return false;
        }

        bool isDungeonBuildPhase = RunManager.Instance.IsDungeonBuildPhase();
        bool isHeroDefensePhase = IsHeroDefensePhase();

        if (isDungeonBuildPhase && moveDuringDungeonBuildPhase)
        {
            return true;
        }

        if (isHeroDefensePhase && moveDuringHeroDefensePhase)
        {
            return true;
        }

        if (!isDungeonBuildPhase && !isHeroDefensePhase && moveDuringOtherPhases)
        {
            return true;
        }

        if (debugLogPhaseStop)
        {
            Debug.Log("Slime stopped by phase. Build: " + isDungeonBuildPhase + " Defense: " + isHeroDefensePhase);
        }

        return false;
    }

    private bool IsHeroDefensePhase()
    {
        if (waveManager == null)
        {
            return false;
        }

        return waveManager.HasActiveHero();
    }

    private void UpdateCombatHold()
    {
        if (!stayStillWhileHeroInAttackRange)
        {
            combatHoldTimer = 0f;
            return;
        }

        if (!IsHeroDefensePhase())
        {
            combatHoldTimer = 0f;
            return;
        }

        if (IsHeroInCombatHoldRange())
        {
            combatHoldTimer = combatHoldAfterHeroLeaves;

            if (debugLogCombatHold)
            {
                Debug.Log("Slime combat hold active.");
            }

            return;
        }

        combatHoldTimer -= Time.deltaTime;
    }

    private bool IsCombatHoldActive()
    {
        if (!stayStillWhileHeroInAttackRange)
        {
            return false;
        }

        return combatHoldTimer > 0f;
    }

    private void HandleCombatHold()
    {
        if (hasMoveTarget && finishCurrentStepBeforeCombatHold)
        {
            MoveToTargetCellSmooth();
            return;
        }

        if (hasMoveTarget)
        {
            ReleaseReservedFood();
        }

        hasMoveTarget = false;
        isMoving = false;
        moveTimer = 0f;

        if (rescueIfInsideWall)
        {
            RescueIfStandingOnInvalidCell();
        }

        RestoreScale();
    }

    private bool IsHeroInCombatHoldRange()
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(transform.position, out int slimeX, out int slimeY))
        {
            return false;
        }

        Vector2Int slimeCell = new Vector2Int(slimeX, slimeY);

        if (!IsWalkable(slimeCell))
        {
            return false;
        }

        HeroHealth[] heroes = FindObjectsByType<HeroHealth>(FindObjectsSortMode.None);

        foreach (HeroHealth hero in heroes)
        {
            if (hero == null)
            {
                continue;
            }

            if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(hero.transform.position, out int heroX, out int heroY))
            {
                continue;
            }

            Vector2Int heroCell = new Vector2Int(heroX, heroY);

            if (!IsWalkable(heroCell))
            {
                continue;
            }

            if (heroCell == slimeCell)
            {
                if (allowSameCellCombatHold)
                {
                    currentCell = slimeCell;
                    return true;
                }

                continue;
            }

            int manhattanDistance = Mathf.Abs(slimeCell.x - heroCell.x)
                + Mathf.Abs(slimeCell.y - heroCell.y);

            if (combatHoldGridRange <= 1)
            {
                if (manhattanDistance == 1)
                {
                    currentCell = slimeCell;
                    return true;
                }

                continue;
            }

            if (TryGetFloorPathDistance(slimeCell, heroCell, combatHoldGridRange, out int pathDistance))
            {
                if (pathDistance <= combatHoldGridRange)
                {
                    currentCell = slimeCell;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetFloorPathDistance(
        Vector2Int startCell,
        Vector2Int goalCell,
        int maxDistance,
        out int pathDistance
    )
    {
        pathDistance = int.MaxValue;

        if (!IsWalkable(startCell))
        {
            return false;
        }

        if (!IsWalkable(goalCell))
        {
            return false;
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();

        queue.Enqueue(startCell);
        distances[startCell] = 0;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDistance = distances[current];

            if (current == goalCell)
            {
                pathDistance = currentDistance;
                return currentDistance <= maxDistance;
            }

            if (currentDistance >= maxDistance)
            {
                continue;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (distances.ContainsKey(next))
                {
                    continue;
                }

                if (!IsWalkable(next))
                {
                    continue;
                }

                distances[next] = currentDistance + 1;
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private bool CanEatAtThisMoment()
    {
        if (!eatOnlyWhenNotMoving)
        {
            return true;
        }

        return !hasMoveTarget && !isMoving;
    }

    private void StopCurrentMoveWithoutChangingCell()
    {
        if (hasMoveTarget)
        {
            ReleaseReservedFood();
        }

        isMoving = false;
        hasMoveTarget = false;
        moveTimer = 0f;
        RestoreScale();

        if (rescueIfInsideWall)
        {
            RescueIfStandingOnInvalidCell();
        }
    }

    private bool TryInitializeCurrentCell()
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(transform.position, out int x, out int y))
        {
            return false;
        }

        Vector2Int cell = new Vector2Int(x, y);

        if (!IsWalkable(cell))
        {
            if (TryFindNearestWalkableCell(cell, rescueSearchRadius, out Vector2Int rescueCell))
            {
                cell = rescueCell;
            }
            else
            {
                return false;
            }
        }

        currentCell = cell;
        previousCell = cell;
        hasCurrentCell = true;
        hasPreviousCell = false;

        return true;
    }

    private void PickInitialDirection()
    {
        List<Vector2Int> openDirections = GetOpenDirections(currentCell);

        if (openDirections.Count <= 0)
        {
            currentDirection = Vector2Int.zero;
            return;
        }

        if (randomizeInitialDirection)
        {
            currentDirection = openDirections[Random.Range(0, openDirections.Count)];
        }
        else
        {
            currentDirection = openDirections[0];
        }
    }

    private void DecideNextMove()
    {
        if (!hasCurrentCell)
        {
            return;
        }

        List<Vector2Int> openDirections = GetOpenDirections(currentCell);

        if (openDirections.Count <= 0)
        {
            ResetStepWait();
            return;
        }

        Vector2Int chosenDirection = ChooseDirectionByMossRule(openDirections);

        if (chosenDirection == Vector2Int.zero)
        {
            ResetStepWait();
            return;
        }

        Vector2Int nextCell = currentCell + chosenDirection;

        if (!IsWalkable(nextCell))
        {
            ResetStepWait();
            return;
        }

        BeginMoveToCell(nextCell);

        if (debugLogChoices)
        {
            Debug.Log("Slime next direction: " + chosenDirection + " target: " + nextCell);
        }
    }

    private Vector2Int ChooseDirectionByMossRule(List<Vector2Int> openDirections)
    {
        if (openDirections == null || openDirections.Count <= 0)
        {
            return Vector2Int.zero;
        }

        if (continueForwardIfPossible && currentDirection != Vector2Int.zero)
        {
            Vector2Int forwardCell = currentCell + currentDirection;

            if (IsWalkable(forwardCell))
            {
                return currentDirection;
            }
        }

        Vector2Int cameFromDirection = Vector2Int.zero;
        bool hasCameFromDirection = false;

        if (hasPreviousCell)
        {
            cameFromDirection = previousCell - currentCell;
            hasCameFromDirection = openDirections.Contains(cameFromDirection);
        }

        List<Vector2Int> nonBacktrackingDirections = new List<Vector2Int>();

        foreach (Vector2Int direction in openDirections)
        {
            if (hasCameFromDirection && direction == cameFromDirection)
            {
                continue;
            }

            nonBacktrackingDirections.Add(direction);
        }

        if (avoidBacktrackingWhenPossible && nonBacktrackingDirections.Count > 0)
        {
            return nonBacktrackingDirections[Random.Range(0, nonBacktrackingDirections.Count)];
        }

        return openDirections[Random.Range(0, openDirections.Count)];
    }

    private List<Vector2Int> GetOpenDirections(Vector2Int fromCell)
    {
        List<Vector2Int> openDirections = new List<Vector2Int>();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int nextCell = fromCell + direction;

            if (IsWalkable(nextCell))
            {
                openDirections.Add(direction);
            }
        }

        return openDirections;
    }

    private void BeginMoveToCell(Vector2Int nextCell)
    {
        targetCell = nextCell;
        hasMoveTarget = true;
        isMoving = true;

        moveStartPosition = transform.position;

        moveEndPosition = dungeonGridManager.GetWorldPositionFromGridPosition(targetCell);
        moveEndPosition.z = transform.position.z;

        float distance = Vector3.Distance(moveStartPosition, moveEndPosition);
        moveDuration = distance / Mathf.Max(0.01f, moveSpeed);
        moveDuration = Mathf.Max(minimumMoveDuration, moveDuration);

        moveTimer = 0f;
    }

    private void MoveToTargetCellSmooth()
    {
        if (dungeonGridManager == null)
        {
            return;
        }

        if (!IsWalkable(targetCell))
        {
            ReleaseReservedFood();

            hasMoveTarget = false;
            isMoving = false;
            ResetStepWait();
            return;
        }

        moveTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(moveTimer / Mathf.Max(0.01f, moveDuration));
        float easedProgress = progress;

        if (useSmoothCellMovement && moveEaseCurve != null)
        {
            easedProgress = moveEaseCurve.Evaluate(progress);
        }

        transform.position = Vector3.Lerp(moveStartPosition, moveEndPosition, easedProgress);

        if (progress < 1f)
        {
            return;
        }

        transform.position = moveEndPosition;

        previousCell = currentCell;
        hasPreviousCell = true;

        currentCell = targetCell;
        currentDirection = currentCell - previousCell;

        hasMoveTarget = false;
        isMoving = false;

        if (squashOnArrival)
        {
            arrivalSquashTimer = arrivalSquashDuration;
        }

        if (eatFood)
        {
            TryEatFoodAtCurrentCell();
        }

        ResetStepWait();
    }

    private bool IsWalkable(Vector2Int cell)
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(cell);

        if (tile == null)
        {
            return false;
        }

        return tile.IsFloor;
    }

    private bool TryEatFoodAtCurrentCell()
    {
        if (eatCooldownTimer > 0f)
        {
            return false;
        }

        if (!IsWalkable(currentCell))
        {
            RescueIfStandingOnInvalidCell();
            return false;
        }

        SlimeFoodItem targetFood = FindOverlappingFoodAtCurrentCell();

        if (targetFood == null)
        {
            return false;
        }

        if (!targetFood.TryReserve(this))
        {
            return false;
        }

        if (snapToFoodPositionWhenEating)
        {
            Vector3 foodPosition = targetFood.transform.position;
            foodPosition.z = transform.position.z;
            transform.position = foodPosition;

            if (dungeonGridManager.TryGetGridPositionFromWorldPosition(transform.position, out int x, out int y))
            {
                currentCell = new Vector2Int(x, y);
            }
        }

        int reproductionCount = GetFoodReproductionCount(targetFood);
        string foodEffectLabel = targetFood.GetFoodEffectLabel();

        targetFood.ApplyFoodEffectToSlime(gameObject);
        targetFood.Consume();

        if (reservedFoodTarget == targetFood)
        {
            reservedFoodTarget = null;
        }

        eatCooldownTimer = eatCooldown;

        if (debugLogFood || debugLogFoodRarityEffect)
        {
            Debug.Log(
                "Slime ate "
                + foodEffectLabel
                + ". ReproductionCount="
                + reproductionCount
            );
        }

        if (reproduceWhenEat && reproductionCount > 0)
        {
            TryReproduce(reproductionCount);
        }

        return true;
    }

    private int GetFoodReproductionCount(SlimeFoodItem food)
    {
        if (!useFoodItemReproductionCount)
        {
            return Mathf.Max(0, fallbackReproductionCount);
        }

        if (food == null)
        {
            return Mathf.Max(0, fallbackReproductionCount);
        }

        return Mathf.Max(0, food.GetReproductionCount());
    }

    private SlimeFoodItem FindOverlappingFoodAtCurrentCell()
    {
        SlimeFoodItem bestFood = null;
        float bestDistance = float.MaxValue;

        if (reservedFoodTarget != null)
        {
            if (IsFoodOverlappingCurrentCell(reservedFoodTarget, out float reservedDistance))
            {
                return reservedFoodTarget;
            }
        }

        SlimeFoodItem[] foods = FindObjectsByType<SlimeFoodItem>(FindObjectsSortMode.None);

        foreach (SlimeFoodItem food in foods)
        {
            if (food == null)
            {
                continue;
            }

            if (!food.IsAvailableFor(this))
            {
                continue;
            }

            if (!IsFoodOverlappingCurrentCell(food, out float distance))
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestFood = food;
            }
        }

        return bestFood;
    }

    private bool IsFoodOverlappingCurrentCell(SlimeFoodItem food, out float distance)
    {
        distance = float.MaxValue;

        if (food == null)
        {
            return false;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(food.transform.position, out int foodX, out int foodY))
        {
            return false;
        }

        Vector2Int foodCell = new Vector2Int(foodX, foodY);

        bool canCheckThisFood = foodCell == currentCell;

        if (eatAdjacentFood)
        {
            int manhattanDistance = Mathf.Abs(foodCell.x - currentCell.x)
                + Mathf.Abs(foodCell.y - currentCell.y);

            if (manhattanDistance <= 1)
            {
                canCheckThisFood = true;
            }
        }

        if (!canCheckThisFood)
        {
            return false;
        }

        distance = Vector2.Distance(transform.position, food.transform.position);

        return distance <= foodOverlapDistance;
    }

    private bool TryMoveOntoAdjacentFood()
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        if (hasMoveTarget || isMoving)
        {
            return false;
        }

        if (eatCooldownTimer > 0f)
        {
            return false;
        }

        SlimeFoodItem[] foods = FindObjectsByType<SlimeFoodItem>(FindObjectsSortMode.None);

        SlimeFoodItem bestFood = null;
        Vector2Int bestFoodCell = currentCell;
        float bestDistance = float.MaxValue;

        foreach (SlimeFoodItem food in foods)
        {
            if (food == null)
            {
                continue;
            }

            if (!food.IsAvailableFor(this))
            {
                continue;
            }

            if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(food.transform.position, out int foodX, out int foodY))
            {
                continue;
            }

            Vector2Int foodCell = new Vector2Int(foodX, foodY);

            if (!IsWalkable(foodCell))
            {
                continue;
            }

            int manhattanDistance = Mathf.Abs(foodCell.x - currentCell.x)
                + Mathf.Abs(foodCell.y - currentCell.y);

            if (manhattanDistance != 1)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, food.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestFood = food;
                bestFoodCell = foodCell;
            }
        }

        if (bestFood == null)
        {
            return false;
        }

        if (!bestFood.TryReserve(this))
        {
            return false;
        }

        reservedFoodTarget = bestFood;
        BeginMoveToCell(bestFoodCell);

        if (debugLogFood)
        {
            Debug.Log("Slime moves onto adjacent food before eating.");
        }

        return true;
    }

    private void ValidateReservedFood()
    {
        if (reservedFoodTarget == null)
        {
            return;
        }

        if (!reservedFoodTarget.IsAvailableFor(this))
        {
            reservedFoodTarget = null;
        }
    }

    private void ReleaseReservedFood()
    {
        if (reservedFoodTarget == null)
        {
            return;
        }

        reservedFoodTarget.ReleaseReservation(this);
        reservedFoodTarget = null;
    }

    private void TryReproduce()
    {
        TryReproduce(1);
    }

    private void TryReproduce(int requestedSpawnCount)
    {
        int safeRequestedSpawnCount = Mathf.Max(0, requestedSpawnCount);

        if (safeRequestedSpawnCount <= 0)
        {
            return;
        }

        SlimeMossAI[] slimes = FindObjectsByType<SlimeMossAI>(FindObjectsSortMode.None);
        int availableSlots = maxSlimeCount - slimes.Length;

        if (availableSlots <= 0)
        {
            Debug.Log("Slime max count reached.");
            return;
        }

        int actualSpawnCount = Mathf.Min(safeRequestedSpawnCount, availableSlots);

        RestoreScale();

        int spawnedCount = 0;

        for (int i = 0; i < actualSpawnCount; i++)
        {
            if (!TryGetSafeReproductionSpawnCell(out Vector2Int spawnCell))
            {
                if (debugLogRescue)
                {
                    Debug.LogWarning("No safe spawn cell found for slime reproduction.");
                }

                break;
            }

            Vector3 spawnPosition = dungeonGridManager.GetWorldPositionFromGridPosition(spawnCell);
            spawnPosition.z = transform.position.z;

            GameObject clone = Instantiate(gameObject, spawnPosition, transform.rotation);
            clone.name = gameObject.name + "_Split";
            clone.transform.localScale = baseScale;

            SlimeMossAI[] cloneAis = clone.GetComponents<SlimeMossAI>();

            foreach (SlimeMossAI ai in cloneAis)
            {
                ai.ResetAfterSpawnAtCell(spawnCell);
            }

            spawnedCount++;
        }

        Debug.Log("Slime reproduced safely. Spawned: " + spawnedCount);
    }

    private bool TryGetSafeReproductionSpawnCell(out Vector2Int spawnCell)
    {
        spawnCell = currentCell;

        List<Vector2Int> adjacentCandidates = new List<Vector2Int>();
        List<Vector2Int> fallbackCandidates = new List<Vector2Int>();

        foreach (Vector2Int direction in directions)
        {
            Vector2Int nextCell = currentCell + direction;

            if (IsSafeSpawnCell(nextCell))
            {
                adjacentCandidates.Add(nextCell);
            }
        }

        if (IsSafeSpawnCell(currentCell))
        {
            fallbackCandidates.Add(currentCell);
        }

        if (preferSpawnAdjacentCell && adjacentCandidates.Count > 0)
        {
            spawnCell = adjacentCandidates[Random.Range(0, adjacentCandidates.Count)];
            return true;
        }

        if (fallbackCandidates.Count > 0)
        {
            spawnCell = fallbackCandidates[Random.Range(0, fallbackCandidates.Count)];
            return true;
        }

        if (TryFindNearestSafeSpawnCell(currentCell, safeSpawnSearchRadius, out Vector2Int nearestSafeCell))
        {
            spawnCell = nearestSafeCell;
            return true;
        }

        return false;
    }

    private bool IsSafeSpawnCell(Vector2Int cell)
    {
        if (!IsWalkable(cell))
        {
            return false;
        }

        if (avoidSpawnOnOtherSlime && IsOccupiedByOtherSlime(cell))
        {
            return false;
        }

        return true;
    }

    private bool IsOccupiedByOtherSlime(Vector2Int cell)
    {
        SlimeMossAI[] slimes = FindObjectsByType<SlimeMossAI>(FindObjectsSortMode.None);

        foreach (SlimeMossAI slime in slimes)
        {
            if (slime == null)
            {
                continue;
            }

            if (slime == this)
            {
                continue;
            }

            if (dungeonGridManager == null)
            {
                continue;
            }

            if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(slime.transform.position, out int x, out int y))
            {
                continue;
            }

            Vector2Int slimeCell = new Vector2Int(x, y);

            if (slimeCell == cell)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindNearestSafeSpawnCell(Vector2Int centerCell, int maxRadius, out Vector2Int safeCell)
    {
        safeCell = centerCell;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();

            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    if (Mathf.Abs(offsetX) + Mathf.Abs(offsetY) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = centerCell + new Vector2Int(offsetX, offsetY);

                    if (IsSafeSpawnCell(candidate))
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                safeCell = candidates[Random.Range(0, candidates.Count)];
                return true;
            }
        }

        return false;
    }

    private bool TryFindNearestWalkableCell(Vector2Int centerCell, int maxRadius, out Vector2Int walkableCell)
    {
        walkableCell = centerCell;

        if (IsWalkable(centerCell))
        {
            walkableCell = centerCell;
            return true;
        }

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    if (Mathf.Abs(offsetX) + Mathf.Abs(offsetY) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = centerCell + new Vector2Int(offsetX, offsetY);

                    if (IsWalkable(candidate))
                    {
                        walkableCell = candidate;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void RescueIfStandingOnInvalidCell()
    {
        if (dungeonGridManager == null)
        {
            return;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(transform.position, out int x, out int y))
        {
            return;
        }

        Vector2Int standingCell = new Vector2Int(x, y);

        if (IsWalkable(standingCell))
        {
            currentCell = standingCell;
            return;
        }

        if (!TryFindNearestWalkableCell(standingCell, rescueSearchRadius, out Vector2Int rescueCell))
        {
            return;
        }

        currentCell = rescueCell;
        previousCell = rescueCell;
        hasPreviousCell = false;
        hasMoveTarget = false;
        isMoving = false;

        ReleaseReservedFood();
        SnapToCellCenter(rescueCell);

        if (debugLogRescue)
        {
            Debug.Log("Slime rescued from wall to cell: " + rescueCell);
        }
    }

    private void SnapToCellCenter(Vector2Int cell)
    {
        if (dungeonGridManager == null)
        {
            return;
        }

        Vector3 center = dungeonGridManager.GetWorldPositionFromGridPosition(cell);
        center.z = transform.position.z;
        transform.position = center;
    }

    public void ResetAfterSpawn()
    {
        AutoFindReferences();

        if (!TryInitializeCurrentCell())
        {
            return;
        }

        ResetAfterSpawnAtCell(currentCell);
    }

    public void ResetAfterSpawnAtCell(Vector2Int spawnCell)
    {
        AutoFindReferences();

        baseScale = transform.localScale;

        currentCell = spawnCell;
        previousCell = spawnCell;
        hasCurrentCell = true;
        hasPreviousCell = false;

        hasMoveTarget = false;
        isMoving = false;
        moveTimer = 0f;
        arrivalSquashTimer = 0f;
        combatHoldTimer = 0f;

        eatCooldownTimer = eatCooldown;
        ReleaseReservedFood();

        if (snapSpawnedSlimeToCellCenter)
        {
            SnapToCellCenter(spawnCell);
        }

        PickInitialDirection();
        ResetStepWait();
        RestoreScale();
    }

    private void ResetStepWait()
    {
        stepWaitTimer = Random.Range(stepWaitMin, stepWaitMax);
    }

    private void UpdateMotionFeel()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        if (isMoving && wobbleWhileMoving)
        {
            float wave = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;

            transform.localScale = new Vector3(
                baseScale.x * (1f + wave),
                baseScale.y * (1f - wave),
                baseScale.z
            );

            return;
        }

        if (arrivalSquashTimer > 0f)
        {
            arrivalSquashTimer -= Time.deltaTime;

            float progress = 1f - Mathf.Clamp01(arrivalSquashTimer / Mathf.Max(0.01f, arrivalSquashDuration));
            float wave = Mathf.Sin(progress * Mathf.PI);

            transform.localScale = new Vector3(
                baseScale.x * (1f + arrivalSquashAmount * wave),
                baseScale.y * (1f - arrivalSquashAmount * wave),
                baseScale.z
            );

            return;
        }

        RestoreScale();
    }

    private void RestoreScale()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        transform.localScale = baseScale;
    }

    private void AutoFindReferences()
    {
        if (autoFindDungeonGridManager && dungeonGridManager == null)
        {
            dungeonGridManager = FindFirstObjectByType<DungeonGridManager>();
        }

        if (autoFindWaveManager && waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
    }
}