using System.Collections.Generic;
using UnityEngine;

public class SlimeFoodSeeker : MonoBehaviour
{
    [Header("References")]
    public DungeonGridManager dungeonGridManager;
    public bool autoFindDungeonGridManager = true;

    [Header("Search")]
    public float searchRange = 5.0f;
    public float searchInterval = 0.25f;

    [Header("Move")]
    public float moveSpeed = 1.25f;
    public float waypointReachDistance = 0.05f;
    public bool onlyMoveDuringDungeonBuildPhase = true;

    [Header("Eat")]
    public float eatDistance = 0.22f;
    public float eatCooldown = 1.2f;

    [Header("Reproduce")]
    public bool reproduceWhenEat = true;
    public int maxSlimeCount = 24;

    [Header("Grid Search")]
    public int nearestWalkableSearchRadius = 2;

    [Header("Motion Feel")]
    public bool wobbleWhileMoving = true;
    public float wobbleAmount = 0.08f;
    public float wobbleSpeed = 10f;

    [Header("Debug")]
    public bool debugLogPathFailures = false;

    private SlimeFoodItem targetFood;
    private Vector2Int targetFoodGridPosition;

    private readonly List<Vector2Int> currentPath = new List<Vector2Int>();
    private int pathIndex;

    private Vector2Int currentLockedCell;
    private Vector2Int moveTargetCell;
    private bool hasMoveTarget;

    private readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private float searchTimer;
    private float cooldownTimer;

    private Vector3 baseScale;
    private bool isMovingToFood;

    private void Start()
    {
        AutoFindReferences();

        baseScale = transform.localScale;
        searchTimer = Random.Range(0f, searchInterval);
    }

    private void OnDisable()
    {
        ReleaseTargetFood();
        ClearPath();
        RestoreScale();
    }

    private void Update()
    {
        AutoFindReferences();

        cooldownTimer -= Time.deltaTime;

        if (!CanSeekFoodNow())
        {
            ReleaseTargetFood();
            ClearPath();
            isMovingToFood = false;
            RestoreScale();
            return;
        }

        ValidateTargetFood();

        if (targetFood == null)
        {
            TryFindFood();
        }

        if (targetFood != null)
        {
            FollowLockedPathToFood();
        }
        else
        {
            isMovingToFood = false;
            RestoreScale();
        }

        UpdateWobble();
    }

    private bool CanSeekFoodNow()
    {
        if (cooldownTimer > 0f)
        {
            return false;
        }

        if (onlyMoveDuringDungeonBuildPhase)
        {
            if (RunManager.Instance != null && !RunManager.Instance.IsDungeonBuildPhase())
            {
                return false;
            }
        }

        return true;
    }

    private void ValidateTargetFood()
    {
        if (targetFood == null)
        {
            return;
        }

        if (!targetFood.IsAvailableFor(this))
        {
            ReleaseTargetFood();
            ClearPath();
            return;
        }

        float directDistance = Vector2.Distance(transform.position, targetFood.transform.position);

        if (directDistance > searchRange * 1.75f)
        {
            ReleaseTargetFood();
            ClearPath();
        }
    }

    private void TryFindFood()
    {
        searchTimer -= Time.deltaTime;

        if (searchTimer > 0f)
        {
            return;
        }

        searchTimer = searchInterval;

        if (dungeonGridManager == null)
        {
            return;
        }

        if (!TryGetNearestWalkableGridPosition(transform.position, out Vector2Int startGridPosition))
        {
            if (debugLogPathFailures)
            {
                Debug.Log("Slime is not on or near a walkable floor tile.");
            }

            return;
        }

        SlimeFoodItem[] foods = FindObjectsByType<SlimeFoodItem>(FindObjectsSortMode.None);

        SlimeFoodItem bestFood = null;
        Vector2Int bestFoodGridPosition = Vector2Int.zero;
        List<Vector2Int> bestPath = null;
        float bestScore = float.MaxValue;

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

            float directDistance = Vector2.Distance(transform.position, food.transform.position);

            if (directDistance > searchRange)
            {
                continue;
            }

            if (!TryGetReachableGoalCellForFood(food, out Vector2Int foodGridPosition))
            {
                continue;
            }

            if (!TryFindGridPath(startGridPosition, foodGridPosition, out List<Vector2Int> candidatePath))
            {
                continue;
            }

            float score = candidatePath.Count + directDistance * 0.01f;

            if (score < bestScore)
            {
                bestScore = score;
                bestFood = food;
                bestFoodGridPosition = foodGridPosition;
                bestPath = candidatePath;
            }
        }

        if (bestFood == null || bestPath == null)
        {
            return;
        }

        if (!bestFood.TryReserve(this))
        {
            return;
        }

        targetFood = bestFood;
        targetFoodGridPosition = bestFoodGridPosition;

        SetPath(bestPath);
    }

    private void FollowLockedPathToFood()
    {
        if (targetFood == null || dungeonGridManager == null)
        {
            return;
        }

        if (Vector2.Distance(transform.position, targetFood.transform.position) <= eatDistance)
        {
            EatTargetFood();
            return;
        }

        if (currentPath.Count <= 0)
        {
            if (!TryRebuildPathToTarget())
            {
                ReleaseTargetFood();
                ClearPath();
                return;
            }
        }

        if (!hasMoveTarget)
        {
            if (!PrepareNextMoveTarget())
            {
                ReleaseTargetFood();
                ClearPath();
                return;
            }
        }

        MoveToLockedTargetCell();
    }

    private bool PrepareNextMoveTarget()
    {
        if (currentPath.Count <= 0)
        {
            return false;
        }

        Vector3 currentCellCenter = dungeonGridManager.GetWorldPositionFromGridPosition(currentLockedCell);
        currentCellCenter.z = transform.position.z;

        if (!IsCloseToPosition(currentCellCenter))
        {
            moveTargetCell = currentLockedCell;
            hasMoveTarget = true;
            return true;
        }

        if (currentLockedCell == targetFoodGridPosition)
        {
            EatTargetFood();
            return true;
        }

        while (pathIndex < currentPath.Count && currentPath[pathIndex] == currentLockedCell)
        {
            pathIndex++;
        }

        if (pathIndex >= currentPath.Count)
        {
            EatTargetFood();
            return true;
        }

        Vector2Int nextCell = currentPath[pathIndex];

        if (!IsCardinalNeighbor(currentLockedCell, nextCell))
        {
            if (debugLogPathFailures)
            {
                Debug.Log("Next slime path cell is not cardinal neighbor.");
            }

            return TryRebuildPathToTarget();
        }

        if (!IsWalkableGridPosition(nextCell))
        {
            if (debugLogPathFailures)
            {
                Debug.Log("Next slime path cell is not walkable.");
            }

            return TryRebuildPathToTarget();
        }

        moveTargetCell = nextCell;
        hasMoveTarget = true;

        return true;
    }

    private void MoveToLockedTargetCell()
    {
        Vector3 targetWorldPosition = dungeonGridManager.GetWorldPositionFromGridPosition(moveTargetCell);
        targetWorldPosition.z = transform.position.z;

        MoveAxisAlignedTo(targetWorldPosition);
        isMovingToFood = true;

        if (!IsCloseToPosition(targetWorldPosition))
        {
            return;
        }

        transform.position = targetWorldPosition;
        currentLockedCell = moveTargetCell;
        hasMoveTarget = false;

        if (currentLockedCell == targetFoodGridPosition)
        {
            EatTargetFood();
        }
    }

    private bool TryRebuildPathToTarget()
    {
        if (targetFood == null || dungeonGridManager == null)
        {
            return false;
        }

        if (!TryGetNearestWalkableGridPosition(transform.position, out Vector2Int currentGridPosition))
        {
            return false;
        }

        if (!TryGetReachableGoalCellForFood(targetFood, out Vector2Int foodGridPosition))
        {
            return false;
        }

        if (!TryFindGridPath(currentGridPosition, foodGridPosition, out List<Vector2Int> newPath))
        {
            return false;
        }

        targetFoodGridPosition = foodGridPosition;
        SetPath(newPath);

        return true;
    }

    private bool TryGetReachableGoalCellForFood(SlimeFoodItem food, out Vector2Int goalGridPosition)
    {
        goalGridPosition = Vector2Int.zero;

        if (food == null || dungeonGridManager == null)
        {
            return false;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(food.transform.position, out int foodX, out int foodY))
        {
            return false;
        }

        Vector2Int baseFoodPosition = new Vector2Int(foodX, foodY);

        List<Vector2Int> candidates = new List<Vector2Int>();
        candidates.Add(baseFoodPosition);

        foreach (Vector2Int direction in directions)
        {
            candidates.Add(baseFoodPosition + direction);
        }

        bool found = false;
        float bestDistance = float.MaxValue;

        foreach (Vector2Int candidate in candidates)
        {
            if (!IsWalkableGridPosition(candidate))
            {
                continue;
            }

            Vector3 candidateWorldPosition = dungeonGridManager.GetWorldPositionFromGridPosition(candidate);
            float distance = Vector2.Distance(candidateWorldPosition, food.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                goalGridPosition = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool TryFindGridPath(Vector2Int start, Vector2Int goal, out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();

        if (dungeonGridManager == null)
        {
            return false;
        }

        if (!IsWalkableGridPosition(start))
        {
            return false;
        }

        if (!IsWalkableGridPosition(goal))
        {
            return false;
        }

        Queue<Vector2Int> openQueue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        openQueue.Enqueue(start);
        visited.Add(start);

        bool found = false;

        while (openQueue.Count > 0)
        {
            Vector2Int current = openQueue.Dequeue();

            if (current == goal)
            {
                found = true;
                break;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = current + direction;

                if (visited.Contains(next))
                {
                    continue;
                }

                if (!IsWalkableGridPosition(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                openQueue.Enqueue(next);
            }
        }

        if (!found)
        {
            return false;
        }

        Vector2Int pathPosition = goal;
        path.Add(pathPosition);

        while (pathPosition != start)
        {
            pathPosition = cameFrom[pathPosition];
            path.Add(pathPosition);
        }

        path.Reverse();

        return true;
    }

    private bool TryGetNearestWalkableGridPosition(Vector3 worldPosition, out Vector2Int gridPosition)
    {
        gridPosition = Vector2Int.zero;

        if (dungeonGridManager == null)
        {
            return false;
        }

        if (!dungeonGridManager.TryGetGridPositionFromWorldPosition(worldPosition, out int x, out int y))
        {
            return false;
        }

        Vector2Int center = new Vector2Int(x, y);

        if (IsWalkableGridPosition(center))
        {
            gridPosition = center;
            return true;
        }

        for (int radius = 1; radius <= nearestWalkableSearchRadius; radius++)
        {
            for (int offsetX = -radius; offsetX <= radius; offsetX++)
            {
                for (int offsetY = -radius; offsetY <= radius; offsetY++)
                {
                    if (Mathf.Abs(offsetX) + Mathf.Abs(offsetY) > radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = center + new Vector2Int(offsetX, offsetY);

                    if (!IsWalkableGridPosition(candidate))
                    {
                        continue;
                    }

                    gridPosition = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsWalkableGridPosition(Vector2Int gridPosition)
    {
        if (dungeonGridManager == null)
        {
            return false;
        }

        DungeonTile tile = dungeonGridManager.GetTileAtGridPosition(gridPosition);

        if (tile == null)
        {
            return false;
        }

        return tile.IsFloor;
    }

    private void SetPath(List<Vector2Int> newPath)
    {
        currentPath.Clear();

        if (newPath != null)
        {
            currentPath.AddRange(newPath);
        }

        pathIndex = 0;
        hasMoveTarget = false;

        if (currentPath.Count > 0)
        {
            currentLockedCell = currentPath[0];
        }
    }

    private void ClearPath()
    {
        currentPath.Clear();
        pathIndex = 0;
        hasMoveTarget = false;
    }

    private bool IsCardinalNeighbor(Vector2Int a, Vector2Int b)
    {
        int distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        return distance == 1;
    }

    private void MoveAxisAlignedTo(Vector3 targetPosition)
    {
        Vector3 currentPosition = transform.position;
        Vector3 nextPosition = currentPosition;

        float deltaX = targetPosition.x - currentPosition.x;
        float deltaY = targetPosition.y - currentPosition.y;

        if (Mathf.Abs(deltaX) > waypointReachDistance)
        {
            nextPosition.x = Mathf.MoveTowards(
                currentPosition.x,
                targetPosition.x,
                moveSpeed * Time.deltaTime
            );
        }
        else if (Mathf.Abs(deltaY) > waypointReachDistance)
        {
            nextPosition.y = Mathf.MoveTowards(
                currentPosition.y,
                targetPosition.y,
                moveSpeed * Time.deltaTime
            );
        }

        nextPosition.z = currentPosition.z;
        transform.position = nextPosition;
    }

    private bool IsCloseToPosition(Vector3 targetPosition)
    {
        return Mathf.Abs(transform.position.x - targetPosition.x) <= waypointReachDistance
            && Mathf.Abs(transform.position.y - targetPosition.y) <= waypointReachDistance;
    }

    private void EatTargetFood()
    {
        if (targetFood == null)
        {
            return;
        }

        targetFood.Consume();
        targetFood = null;

        ClearPath();

        cooldownTimer = eatCooldown;
        isMovingToFood = false;
        RestoreScale();

        if (reproduceWhenEat)
        {
            TryReproduce();
        }
    }

    private void TryReproduce()
    {
        SlimeFoodSeeker[] slimes = FindObjectsByType<SlimeFoodSeeker>(FindObjectsSortMode.None);

        if (slimes.Length >= maxSlimeCount)
        {
            Debug.Log("Slime max count reached.");
            return;
        }

        RestoreScale();

        Vector3 spawnPosition = GetReproductionSpawnPosition();

        GameObject clone = Instantiate(gameObject, spawnPosition, transform.rotation);
        clone.name = gameObject.name + "_Split";
        clone.transform.localScale = baseScale;

        SlimeFoodSeeker[] cloneSeekers = clone.GetComponents<SlimeFoodSeeker>();

        foreach (SlimeFoodSeeker seeker in cloneSeekers)
        {
            seeker.ResetAfterSpawn();
        }

        Debug.Log("Slime reproduced.");
    }

    private Vector3 GetReproductionSpawnPosition()
    {
        if (dungeonGridManager == null)
        {
            return transform.position;
        }

        if (!TryGetNearestWalkableGridPosition(transform.position, out Vector2Int currentGridPosition))
        {
            return transform.position;
        }

        List<Vector2Int> candidatePositions = new List<Vector2Int>();
        candidatePositions.Add(currentGridPosition);

        foreach (Vector2Int direction in directions)
        {
            Vector2Int next = currentGridPosition + direction;

            if (IsWalkableGridPosition(next))
            {
                candidatePositions.Add(next);
            }
        }

        Vector2Int selectedGridPosition = candidatePositions[Random.Range(0, candidatePositions.Count)];
        Vector3 spawnPosition = dungeonGridManager.GetWorldPositionFromGridPosition(selectedGridPosition);
        spawnPosition.z = transform.position.z;

        return spawnPosition;
    }

    public void ResetAfterSpawn()
    {
        ReleaseTargetFood();
        ClearPath();

        searchTimer = Random.Range(0f, searchInterval);
        cooldownTimer = eatCooldown;

        isMovingToFood = false;
        baseScale = transform.localScale;
        RestoreScale();

        if (TryGetNearestWalkableGridPosition(transform.position, out Vector2Int currentGridPosition))
        {
            currentLockedCell = currentGridPosition;
        }
    }

    private void ReleaseTargetFood()
    {
        if (targetFood != null)
        {
            targetFood.ReleaseReservation(this);
            targetFood = null;
        }
    }

    private void UpdateWobble()
    {
        if (!wobbleWhileMoving)
        {
            return;
        }

        if (!isMovingToFood)
        {
            return;
        }

        float wave = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;

        transform.localScale = new Vector3(
            baseScale.x * (1f + wave),
            baseScale.y * (1f - wave),
            baseScale.z
        );
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