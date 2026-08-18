using System.Collections.Generic;
using UnityEngine;

public class HeroMover : MonoBehaviour
{
    public float speed = 2f;
    public float coreXPosition = 6f;
    public int coreDamage = 1;

    [Header("Path Movement")]
    public float reachPointDistance = 0.05f;

    private bool hasReachedCore;
    private HeroAttack heroAttack;

    private List<Vector3> pathPoints = new List<Vector3>();
    private int currentPathIndex;
    private bool hasPath;

    private void Awake()
    {
        heroAttack = GetComponent<HeroAttack>();
    }

    private void Update()
    {
        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            return;
        }

        if (heroAttack == null)
        {
            heroAttack = GetComponent<HeroAttack>();
        }

        if (heroAttack != null && heroAttack.IsAttacking)
        {
            return;
        }

        if (hasPath)
        {
            MoveAlongPath();
        }
        else
        {
            MoveStraightFallback();
        }
    }

    public void ConfigureMovement(float newSpeed, int newCoreDamage)
    {
        speed = newSpeed;
        coreDamage = newCoreDamage;
        hasReachedCore = false;
    }

    public void SetPath(List<Vector3> newPathPoints)
    {
        pathPoints = new List<Vector3>();

        if (newPathPoints != null)
        {
            pathPoints.AddRange(newPathPoints);
        }

        if (pathPoints.Count <= 0)
        {
            hasPath = false;
            currentPathIndex = 0;
            Debug.LogWarning("Hero path is empty. Using straight fallback movement.");
            return;
        }

        hasPath = true;

        transform.position = pathPoints[0];

        if (pathPoints.Count >= 2)
        {
            currentPathIndex = 1;
        }
        else
        {
            currentPathIndex = 0;
        }

        Debug.Log("Hero path set. Points: " + pathPoints.Count);
    }

    private void MoveAlongPath()
    {
        if (pathPoints == null || pathPoints.Count <= 0)
        {
            hasPath = false;
            return;
        }

        if (currentPathIndex >= pathPoints.Count)
        {
            ReachCore();
            return;
        }

        Vector3 targetPosition = pathPoints[currentPathIndex];
        targetPosition.z = transform.position.z;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance <= reachPointDistance)
        {
            currentPathIndex++;

            if (currentPathIndex >= pathPoints.Count)
            {
                ReachCore();
            }
        }
    }

    private void MoveStraightFallback()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;

        if (!hasReachedCore && transform.position.x >= coreXPosition)
        {
            ReachCore();
        }
    }

    private void ReachCore()
    {
        if (hasReachedCore)
        {
            return;
        }

        hasReachedCore = true;

        Debug.Log("Hero reached the core!");

        if (RunManager.Instance != null)
        {
            RunManager.Instance.TakeCoreDamage(coreDamage);

            if (!RunManager.Instance.isGameOver)
            {
                RunManager.Instance.SetPhaseToDungeonBuild();
            }
        }

        Destroy(gameObject);
    }
}