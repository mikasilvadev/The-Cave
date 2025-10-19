using System.Collections; // "Caixa de ferramentas" da Unity para Corrotinas (IEnumerator)
using UnityEngine; // "Caixa de ferramentas" principal da Unity

/// <summary>
/// Este script vai no GameObject da "Lanterna do Chão".
/// Ele controla o comportamento do item ANTES dele ser coletado.
/// Ele sabe como "brilhar" (Highlight) quando o player olha
/// e como "sumir" (OnPickup) quando o player pega.
/// </summary>
public class FlashlightItem : MonoBehaviour
{
    // --- Variáveis (Campos) ---
    // São as "configurações" que aparecem no Inspector da Unity.

    [Header("Configurações")] // Título no Inspector

    // (NOTA: Esta variável 'pickupDelay' não é usada em lugar nenhum
    // porque a função 'OnDrop' está desativada (comentada).
    // Pode ser apagada para "limpar" o código.)
    [Tooltip("Tempo em segundos antes de poder coletar o item após ser dropado.")]
    public float pickupDelay = 1.5f;

    [Header("Materiais de Destaque")]
    [Tooltip("Arraste aqui o material de contorno (Outline).")]
    // 'Material' é a "tinta" (textura/cor/shader) de um objeto.
    public Material highlightMaterial; // A "tinta" de brilho

    // --- Variáveis Privadas (Memória Interna) ---

    // "Caixinha" para guardar a "tinta" ORIGINAL da lanterna.
    private Material originalMaterial;

    // "Caixinha" para guardar o "Pintor" (o MeshRenderer).
    // O MeshRenderer é o componente que "pinta" o objeto 3D.
    private MeshRenderer meshRenderer;

    // "Caixinha" para guardar a "luzinha" que vem da
    // própria lanterna (para ela brilhar no escuro).
    private Light myLight;

    // Flag (bandeira) 'true'/'false' que diz se o item pode ser pego.
    // (O PlayerController checa isso antes de tentar pegar).
    public bool canBePickedUp = true;

    /// <summary>
    /// Método 'Awake'. Roda ANTES de todo mundo (antes do Start).
    /// Perfeito para "pegar" componentes e guardar na "memória".
    /// </summary>
    void Awake()
    {
        // 'GetComponentInChildren<Light>()'
        // "Procure no MEU GameObject, e em todos os meus FILHOS,
        // pelo componente 'Light' e guarde ele na caixinha 'myLight'."
        myLight = GetComponentInChildren<Light>();

        // "Procure... pelo componente 'MeshRenderer' e guarde na caixinha 'meshRenderer'."
        meshRenderer = GetComponentInChildren<MeshRenderer>();

        // Checagem de Segurança: Se ele ACHOU um "Pintor"...
        if (meshRenderer != null)
        {
            // "Guarde na 'originalMaterial' a 'tinta' que o
            // 'meshRenderer' está usando AGORA."
            // (Isso é "tirar uma foto" do material normal
            // para a gente poder restaurar ele depois).
            originalMaterial = meshRenderer.material;
        }
    }

    /// <summary>
    /// Método 'Start'. Roda UMA VEZ, no primeiro frame (depois do Awake).
    /// Perfeito para "ações" iniciais.
    /// </summary>
    void Start()
    {
        // Checagem de Segurança: Se a gente achou uma "luzinha" no Awake...
        if (myLight != null)
        {
            // ...LIGA ela!
            // (Isso faz a lanterna no chão brilhar,
            // ajudando o player a achar ela no escuro).
            myLight.enabled = true;
        }
    }

    /// <summary>
    /// "Botão" público que o PLAYER (PlayerController) "aperta"
    /// quando ele coleta este item.
    /// </summary>
    public void OnPickup()
    {
        // 'Destroy(gameObject)' = "Destrua o GameObject
        // (e todos os seus componentes) onde este script está."
        // A lanterna do chão "some" do mundo.
        Destroy(gameObject);
    }

    // --- LÓGICA DE "DROPAR" O ITEM ---
    // (Como os comentários dizem, esta lógica foi desativada.
    // É uma escolha de design: o player não pode largar a lanterna.
    // Isso é ótimo!)

    // A função será desativada.
    /* public void OnDrop()
    {
        StartCoroutine(EnablePickupAfterDelay(pickupDelay));
    }

    private IEnumerator EnablePickupAfterDelay(float delay)
    {
        canBePickedUp = false;

        yield return new WaitForSeconds(delay);

        canBePickedUp = true;
    }
    */

    /// <summary>
    /// "Botão" público que o PLAYER (PlayerController) "aperta"
    /// quando ele "olha" para este item (pra fazer ele brilhar).
    /// </summary>
    public void Highlight()
    {
        // Manda um log pro Console (bom pra debug).
        Debug.Log("Comando Highlight() recebido por " + gameObject.name);

        // Checagem de Segurança DUPLA:
        // "Se o 'meshRenderer' (Pintor) EXISTE E
        // o 'highlightMaterial' (Tinta de Brilho) EXISTE..."
        // (Evita erros se a gente esquecer de arrastar o material no Inspector).
        if (meshRenderer != null && highlightMaterial != null)
        {
            // "Pintor, troque sua 'tinta' atual
            // pela 'tinta de brilho'."
            meshRenderer.material = highlightMaterial;
            Debug.Log("Material de destaque APLICADO!");
        }
        else // Se deu ruim (faltou arrastar alguma coisa no Inspector)...
        {
            // Avisa a gente no Console com um ERRO em vermelho.
            Debug.LogError("ERRO: Tentativa de Highlight, mas meshRenderer ou highlightMaterial é NULO!");
        }
    }

    /// <summary>
    /// "Botão" público que o PLAYER (PlayerController) "aperta"
    /// quando ele "para de olhar" para este item.
    /// </summary>
    public void RemoveHighlight()
    {
        // Checagem de Segurança:
        // "Se o 'meshRenderer' EXISTE E
        // a 'originalMaterial' (nossa "foto" do Awake) EXISTE..."
        if (meshRenderer != null && originalMaterial != null)
        {
            // "Pintor, troque sua 'tinta' atual
            // de volta para a 'tinta original' que a gente guardou."
            // (O item para de brilhar).
            meshRenderer.material = originalMaterial;
        }
    }
}