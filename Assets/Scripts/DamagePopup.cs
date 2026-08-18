using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Vector3 moveDirection;
    private Color startColor;

    private float lifeTime = 0.6f;
    private float timer;
    private float moveSpeed = 1.6f;

    private void Awake()
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();

        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 4f;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.enableWordWrapping = false;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 300;
        }

        moveDirection = new Vector3(
            Random.Range(-0.25f, 0.25f),
            1f,
            0f
        ).normalized;
    }

    public void Setup(int damage, Color color)
    {
        if (textMesh == null)
        {
            return;
        }

        if (damage <= 0)
        {
            textMesh.text = "0";
        }
        else
        {
            textMesh.text = "-" + damage;
        }

        startColor = color;
        textMesh.color = startColor;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        float progress = Mathf.Clamp01(timer / lifeTime);
        float scale = Mathf.Lerp(1.2f, 0.8f, progress);
        transform.localScale = Vector3.one * scale;

        if (textMesh != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, progress);
            textMesh.color = color;
        }

        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    public static void Create(Vector3 position, int damage, Color color)
    {
        GameObject popupObject = new GameObject("DamagePopup");
        popupObject.transform.position = position;

        DamagePopup popup = popupObject.AddComponent<DamagePopup>();
        popup.Setup(damage, color);
    }
}