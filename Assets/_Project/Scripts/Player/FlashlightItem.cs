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
    
    // Variáveis internas para funcionar
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

    // --- CORPO DAS FUNÇÕES RESTAURADO ---
    void Start()
    {
        // Garante que a luz sempre comece ligada
        if (myLight != null)
        {
            myLight.enabled = true;
        }
    }

    public void OnPickup()
    {
        // Quando o jogador pegar este item, ele será destruído da cena.
        Destroy(gameObject);
    }

    public void OnDrop()
    {
        // Inicia a rotina que vai reativar a coleta após um tempo
        StartCoroutine(EnablePickupAfterDelay(pickupDelay));
    }

    private IEnumerator EnablePickupAfterDelay(float delay)
    {
        // Imediatamente impede a coleta
        canBePickedUp = false;

        // Espera o tempo definido
        yield return new WaitForSeconds(delay);

        // Permite a coleta novamente
        canBePickedUp = true;
    }
    // --- FIM DAS FUNÇÕES RESTAURADAS ---

    // --- NOVAS FUNÇÕES PARA O CONTORNO ---
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