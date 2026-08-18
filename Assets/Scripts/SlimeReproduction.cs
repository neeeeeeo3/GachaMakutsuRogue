using UnityEngine;

public class SlimeReproduction : MonoBehaviour
{
    public float searchRange = 1.5f;
    public float reproductionInterval = 3f;
    public int maxSlimeCount = 30;

    private float reproductionTimer;

    private void Start()
    {
        reproductionTimer = Random.Range(0f, reproductionInterval);
    }

    private void Update()
    {
        reproductionTimer += Time.deltaTime;

        if (reproductionTimer < reproductionInterval)
        {
            return;
        }

        reproductionTimer = 0f;

        int currentSlimeCount = FindObjectsByType<SlimeReproduction>(FindObjectsSortMode.None).Length;
        int finalMaxSlimeCount = maxSlimeCount;

        if (RunManager.Instance != null)
        {
            finalMaxSlimeCount += RunManager.Instance.maxSlimeBonus;
        }

        if (currentSlimeCount >= finalMaxSlimeCount)
        {
            return;
        }

        FoodMarker nearestFood = FindNearestFood();

        if (nearestFood == null)
        {
            return;
        }

        Destroy(nearestFood.gameObject);
        Reproduce();

        Debug.Log("Slime reproduced! Current Slime Count: " + (currentSlimeCount + 1));
    }

    private FoodMarker FindNearestFood()
    {
        FoodMarker[] foods = FindObjectsByType<FoodMarker>(FindObjectsSortMode.None);

        FoodMarker nearestFood = null;
        float nearestDistance = float.MaxValue;

        foreach (FoodMarker food in foods)
        {
            float distance = Vector3.Distance(transform.position, food.transform.position);

            if (distance <= searchRange && distance < nearestDistance)
            {
                nearestFood = food;
                nearestDistance = distance;
            }
        }

        return nearestFood;
    }

    private void Reproduce()
    {
        Vector2 randomOffset = Random.insideUnitCircle * 0.6f;

        Vector3 spawnPosition = transform.position + new Vector3(
            randomOffset.x,
            randomOffset.y,
            0f
        );

        Instantiate(gameObject, spawnPosition, Quaternion.identity);
    }
}