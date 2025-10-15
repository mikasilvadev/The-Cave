using System.Collections;
using UnityEngine;

public class FlashlightItem : MonoBehaviour
{
    [Header("Configurações")]
    [Tooltip("Tempo em segundos antes de poder coletar o item após ser dropado.")]
    public float pickupDelay = 1.5f;

    [Header("Materiais de Destaque")]
    [Tooltip("Arraste aqui o material de contorno (Outline).")]
    public Material highlightMaterial;

    private Material originalMaterial;
    private MeshRenderer meshRenderer;
    private Light myLight;
    public bool canBePickedUp = true;

    void Awake()
    {
        myLight = GetComponentInChildren<Light>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
    }

    void Start()
    {
        if (myLight != null)
        {
            myLight.enabled = true;
        }
    }

    public void OnPickup()
    {
        Destroy(gameObject);
    }

    public void OnDrop()
    {
        StartCoroutine(EnablePickupAfterDelay(pickupDelay));
    }

    private IEnumerator EnablePickupAfterDelay(float delay)
    {
        canBePickedUp = false;

        yield return new WaitForSeconds(delay);

        canBePickedUp = true;
    }

    public void Highlight()
    {
        Debug.Log("Comando Highlight() recebido por " + gameObject.name);
        if (meshRenderer != null && highlightMaterial != null)
        {
            meshRenderer.material = highlightMaterial;
            Debug.Log("Material de destaque APLICADO!");
        }
        else
        {
            Debug.LogError("ERRO: Tentativa de Highlight, mas meshRenderer ou highlightMaterial é NULO!");
        }
    }

    public void RemoveHighlight()
    {
        if (meshRenderer != null && originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
    }
}