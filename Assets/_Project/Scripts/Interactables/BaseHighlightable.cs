using UnityEngine;
using System.Collections;

public class BaseHighlightable : MonoBehaviour
{
    [Header("Configurações de Destaque")]
    public Material highlightMaterial;
    [Tooltip("Qual índice de material deve ser trocado? (0 é o primeiro, 1 é o segundo, etc.)")]
    public int materialIndexToChange = 0;

    [Tooltip("Velocidade do pulso (em segundos). 0.5 = pulsa 2x por segundo.")]
    public float pulseSpeed = 0.5f;

    [Tooltip("Intensidade mínima do destaque durante o pulso (ex: 0.2 para nunca apagar totalmente).")]
    [Range(0, 1)] public float minHighlightIntensity = 0.2f;

    [Tooltip("Intensidade máxima do destaque durante o pulso (ex: 0.8 para nunca brilhar totalmente).")]
    [Range(0, 1)] public float maxHighlightIntensity = 0.8f;

    protected MeshRenderer meshRenderer;
    private Material originalMaterialInstance;
    private Material[] allMaterials;
    private Coroutine pulseCoroutine;
    private bool isPulsing = false;

    protected virtual void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            allMaterials = meshRenderer.materials;

            if (materialIndexToChange >= 0 && materialIndexToChange < allMaterials.Length)
            {
                originalMaterialInstance = new Material(allMaterials[materialIndexToChange]);

                if (highlightMaterial != null && originalMaterialInstance != null && highlightMaterial.shader.name == "Custom/HighlightBlendShader")
                {
                    if (originalMaterialInstance.HasProperty("_BaseMap"))
                    {
                        highlightMaterial.SetTexture("_MainTex", originalMaterialInstance.GetTexture("_BaseMap"));
                        if (originalMaterialInstance.HasProperty("_BaseColor"))
                        {
                            highlightMaterial.SetColor("_Color", originalMaterialInstance.GetColor("_BaseColor"));
                        }
                    }
                    else if (originalMaterialInstance.HasProperty("_MainTex"))
                    {
                        highlightMaterial.SetTexture("_MainTex", originalMaterialInstance.GetTexture("_MainTex"));
                        if (originalMaterialInstance.HasProperty("_Color"))
                        {
                            highlightMaterial.SetColor("_Color", originalMaterialInstance.GetColor("_Color"));
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"BaseHighlightable: Índice de material {materialIndexToChange} é INVÁLIDO para {gameObject.name}. O objeto tem {allMaterials.Length} materiais.");
                enabled = false;
            }
        }
        else
        {
            Debug.LogWarning($"BaseHighlightable: Não encontrou MeshRenderer em {gameObject.name}");
            enabled = false;
        }
    }

    public virtual void Highlight()
    {
        if (isPulsing) return;

        SetHighlightMaterial(true);
        pulseCoroutine = StartCoroutine(PulseEffect());
        isPulsing = true;
    }

    public virtual void RemoveHighlight()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        isPulsing = false;
        SetHighlightMaterial(false);
    }

    private IEnumerator PulseEffect()
    {
        float timer = 0f;
        while (true)
        {
            float pingPong = Mathf.PingPong(timer * pulseSpeed, 1f);
            float currentIntensity = Mathf.Lerp(minHighlightIntensity, maxHighlightIntensity, pingPong);

            if (highlightMaterial != null && highlightMaterial.shader.name == "Custom/HighlightBlendShader")
            {
                highlightMaterial.SetFloat("_HighlightIntensity", currentIntensity);
            }

            SetHighlightMaterial(true);

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void SetHighlightMaterial(bool applyHighlight)
    {
        if (meshRenderer == null || allMaterials == null || materialIndexToChange < 0 || materialIndexToChange >= allMaterials.Length) return;

        if (applyHighlight)
        {
            allMaterials[materialIndexToChange] = highlightMaterial;
        }
        else
        {
            allMaterials[materialIndexToChange] = originalMaterialInstance;
        }
        meshRenderer.materials = allMaterials;
    }

    protected virtual void OnDisable()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        isPulsing = false;
        SetHighlightMaterial(false);
    }

    protected virtual void OnDestroy()
    {
        if (originalMaterialInstance != null)
        {
            Destroy(originalMaterialInstance);
        }
    }
}