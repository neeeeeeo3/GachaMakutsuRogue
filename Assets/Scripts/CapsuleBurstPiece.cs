using UnityEngine;

public class CapsuleBurstPiece : MonoBehaviour
{
    private Vector3 moveDirection;
    private float moveSpeed;
    private float lifeTime;
    private float timer;

    private SpriteRenderer spriteRenderer;
    private Color startColor;

    public void Initialize(Vector3 direction, float speed, float duration)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        lifeTime = duration;
        timer = 0f;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, 360f * Time.deltaTime);

        if (spriteRenderer != null && lifeTime > 0f)
        {
            float progress = Mathf.Clamp01(timer / lifeTime);
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, progress);
            spriteRenderer.color = color;
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}