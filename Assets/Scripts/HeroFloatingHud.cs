using TMPro;
using UnityEngine;

public class HeroFloatingHud : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 0.9f, 0f);
    public float fontSize = 2.3f;
    public bool showHeroName = true;

    private HeroHealth heroHealth;
    private GameObject textObject;
    private TextMeshPro textMesh;

    private void Awake()
    {
        heroHealth = GetComponent<HeroHealth>();
        CreateTextObject();
    }

    private void LateUpdate()
    {
        if (heroHealth == null)
        {
            return;
        }

        if (textObject == null || textMesh == null)
        {
            CreateTextObject();
        }

        textObject.transform.position = transform.position + offset;
        textObject.transform.rotation = Quaternion.identity;

        Refresh();
    }

    public void Refresh()
    {
        if (heroHealth == null || textMesh == null)
        {
            return;
        }

        string hpText = "HP " + heroHealth.currentHp + " / " + heroHealth.maxHp;

        if (showHeroName)
        {
            textMesh.text = heroHealth.heroName + "\n" + hpText;
        }
        else
        {
            textMesh.text = hpText;
        }

        float hpRate = 0f;

        if (heroHealth.maxHp > 0)
        {
            hpRate = (float)heroHealth.currentHp / heroHealth.maxHp;
        }

        if (hpRate <= 0.25f)
        {
            textMesh.color = new Color(1f, 0.25f, 0.2f, 1f);
        }
        else if (hpRate <= 0.5f)
        {
            textMesh.color = new Color(1f, 0.85f, 0.25f, 1f);
        }
        else
        {
            textMesh.color = Color.white;
        }
    }

    private void CreateTextObject()
    {
        if (textObject != null)
        {
            return;
        }

        textObject = new GameObject("HeroFloatingHudText");
        textObject.transform.position = transform.position + offset;

        textMesh = textObject.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.enableWordWrapping = false;

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingOrder = 250;
        }
    }

    private void OnDestroy()
    {
        if (textObject != null)
        {
            Destroy(textObject);
        }
    }
}