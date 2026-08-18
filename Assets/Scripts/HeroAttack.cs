using UnityEngine;

public class HeroAttack : MonoBehaviour
{
    public float attackRange = 1.15f;
    public int damage = 1;
    public float attackInterval = 0.8f;

    public bool IsAttacking { get; private set; }

    private float attackTimer;
    private SlimeHealth targetSlime;

    private void Awake()
    {
        attackTimer = attackInterval;
    }

    private void Update()
    {
        if (RunManager.Instance != null && RunManager.Instance.isGameOver)
        {
            IsAttacking = false;
            targetSlime = null;
            return;
        }

        UpdateTarget();

        if (targetSlime == null)
        {
            IsAttacking = false;
            return;
        }

        IsAttacking = true;

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            AttackTarget();
        }
    }

    public void ConfigureAttack(float newAttackRange, int newDamage, float newAttackInterval)
    {
        attackRange = newAttackRange;
        damage = newDamage;
        attackInterval = newAttackInterval;

        if (attackInterval <= 0.05f)
        {
            attackInterval = 0.05f;
        }

        attackTimer = attackInterval;
    }

    private void UpdateTarget()
    {
        if (targetSlime != null)
        {
            if (targetSlime.IsDead)
            {
                targetSlime = null;
                return;
            }

            float currentDistance = Vector3.Distance(transform.position, targetSlime.transform.position);

            if (currentDistance <= attackRange)
            {
                return;
            }

            targetSlime = null;
        }

        targetSlime = FindNearestSlimeInRange();
    }

    private SlimeHealth FindNearestSlimeInRange()
    {
        SlimeHealth[] slimes = FindObjectsByType<SlimeHealth>(FindObjectsSortMode.None);

        SlimeHealth nearestSlime = null;
        float nearestDistance = float.MaxValue;

        foreach (SlimeHealth slime in slimes)
        {
            if (slime == null || slime.IsDead)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, slime.transform.position);

            if (distance <= attackRange && distance < nearestDistance)
            {
                nearestSlime = slime;
                nearestDistance = distance;
            }
        }

        return nearestSlime;
    }

    private void AttackTarget()
    {
        if (targetSlime == null || targetSlime.IsDead)
        {
            targetSlime = null;
            return;
        }

        Debug.Log("Hero attacked slime! Damage: " + damage);

        targetSlime.TakeDamage(damage);

        if (targetSlime == null || targetSlime.IsDead)
        {
            targetSlime = null;
        }
    }
}