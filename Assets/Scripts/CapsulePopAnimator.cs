using UnityEngine;

public class CapsulePopAnimator : MonoBehaviour
{
    [Header("Timing")]
    public bool playOnStart = true;
    public float duration = 0.65f;

    [Header("Bounce")]
    public float bounceHeight = 0.28f;
    public float settleBounceHeight = 0.08f;
    public float rollDistance = 0.18f;

    [Header("Spin")]
    public float spinDegrees = 380f;

    [Header("Squash")]
    public bool useSquash = true;
    public float squashAmount = 0.12f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private float timer;
    private bool isPlaying;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;

        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;

        timer = 0f;
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        timer += Time.deltaTime;

        float progress = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
        float easeOut = 1f - Mathf.Pow(1f - progress, 3f);

        float bounce = Mathf.Sin(progress * Mathf.PI) * bounceHeight;
        float settleBounce = Mathf.Sin(progress * Mathf.PI * 4f) * settleBounceHeight * (1f - progress);

        Vector3 nextPosition = startPosition;
        nextPosition.x += rollDistance * easeOut;
        nextPosition.y += bounce + settleBounce;

        transform.position = nextPosition;

        float rotationZ = spinDegrees * easeOut;
        transform.rotation = startRotation * Quaternion.Euler(0f, 0f, rotationZ);

        if (useSquash)
        {
            float squashWave = Mathf.Sin(progress * Mathf.PI * 4f) * squashAmount * (1f - progress);

            transform.localScale = new Vector3(
                startScale.x * (1f + squashWave),
                startScale.y * (1f - squashWave),
                startScale.z
            );
        }

        if (progress >= 1f)
        {
            isPlaying = false;
            transform.position = startPosition + new Vector3(rollDistance, 0f, 0f);
            transform.localScale = startScale;
        }
    }
}