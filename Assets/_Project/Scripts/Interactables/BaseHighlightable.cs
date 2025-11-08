using UnityEngine;
using System.Collections;
using TMPro;

public class BaseHighlightable : MonoBehaviour
{
    [Header("Configurações de Destaque")]
    public Material highlightMaterial;
    public int materialIndexToChange = 0;
    public float pulseSpeed = 0.5f;
    [Range(0, 1)] public float minHighlightIntensity = 0.2f;
    [Range(0, 1)] public float maxHighlightIntensity = 0.8f;

    [Header("Prompt Flutuante (3D)")]
    public GameObject worldSpacePromptVisuals;
    public bool showWorldSpacePrompt = true;
    public string associatedActionName = "Interact";
    public string promptText = "to Interact";

    protected MeshRenderer meshRenderer;
    private Material originalMaterialInstance;
    private Material[] allMaterials;
    private Coroutine pulseCoroutine;
    private bool isPulsing = false;
    private TextMeshProUGUI worldSpaceTextComponent;
    private bool pendingRefresh = false;

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
                if (highlightMaterial != null && highlightMaterial.shader.name == "Custom/HighlightBlendShader")
                {
                    if (originalMaterialInstance.HasProperty("_BaseMap"))
                    {
                        highlightMaterial.SetTexture("_MainTex", originalMaterialInstance.GetTexture("_BaseMap"));
                        if (originalMaterialInstance.HasProperty("_BaseColor"))
                            highlightMaterial.SetColor("_Color", originalMaterialInstance.GetColor("_BaseColor"));
                    }
                    else if (originalMaterialInstance.HasProperty("_MainTex"))
                    {
                        highlightMaterial.SetTexture("_MainTex", originalMaterialInstance.GetTexture("_MainTex"));
                        if (originalMaterialInstance.HasProperty("_Color"))
                            highlightMaterial.SetColor("_Color", originalMaterialInstance.GetColor("_Color"));
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

        if (worldSpacePromptVisuals != null)
        {
            worldSpaceTextComponent = worldSpacePromptVisuals.GetComponentInChildren<TextMeshProUGUI>();
            worldSpacePromptVisuals.SetActive(false);
        }
    }

    protected virtual void OnEnable()
    {
        SettingsManager.OnBindingsChanged += HandleBindingsChanged;

        if (pendingRefresh)
        {
            UpdateWorldSpacePromptText();
            pendingRefresh = false;
        }
    }

    protected virtual void OnDisable()
    {
        SettingsManager.OnBindingsChanged -= HandleBindingsChanged;

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        isPulsing = false;
        SetHighlightMaterial(false);
    }

    public virtual void Highlight()
    {
        if (isPulsing) return;
        isPulsing = true;

        SetHighlightMaterial(true);
        pulseCoroutine = StartCoroutine(PulseEffect());

        if (showWorldSpacePrompt && worldSpacePromptVisuals != null)
        {
            worldSpacePromptVisuals.SetActive(true);
            UpdateWorldSpacePromptText();
        }
    }

    public virtual void RemoveHighlight()
    {
        if (!isPulsing) return;
        isPulsing = false;

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        SetHighlightMaterial(false);
        if (worldSpacePromptVisuals != null)
            worldSpacePromptVisuals.SetActive(false);
    }

    private IEnumerator PulseEffect()
    {
        float timer = 0f;
        while (true)
        {
            float pingPong = Mathf.PingPong(timer * pulseSpeed, 1f);
            float currentIntensity = Mathf.Lerp(minHighlightIntensity, maxHighlightIntensity, pingPong);

            if (highlightMaterial != null && highlightMaterial.shader.name == "Custom/HighlightBlendShader")
                highlightMaterial.SetFloat("_HighlightIntensity", currentIntensity);

            SetHighlightMaterial(true);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void SetHighlightMaterial(bool applyHighlight)
    {
        if (meshRenderer == null || allMaterials == null || materialIndexToChange < 0 || materialIndexToChange >= allMaterials.Length) return;
        if (applyHighlight) allMaterials[materialIndexToChange] = highlightMaterial;
        else allMaterials[materialIndexToChange] = originalMaterialInstance;
        meshRenderer.materials = allMaterials;
    }

    private void HandleBindingsChanged()
    {
        if (isPulsing && showWorldSpacePrompt && worldSpacePromptVisuals != null && worldSpacePromptVisuals.activeSelf)
        {
            UpdateWorldSpacePromptText();
            pendingRefresh = false;
        }
        else
        {
            pendingRefresh = true;
        }
    }

    private void UpdateWorldSpacePromptText()
    {
        if (InteractionPromptUI.Instance != null && worldSpaceTextComponent != null)
        {
            string keyName = InteractionPromptUI.Instance.GetCachedKeyName(associatedActionName);
            worldSpaceTextComponent.text = $"Press [{keyName}] {promptText}";
        }
        else if (worldSpaceTextComponent != null)
        {
            worldSpaceTextComponent.text = promptText;
        }
    }

    protected virtual void OnDestroy()
    {
        if (originalMaterialInstance != null) Destroy(originalMaterialInstance);
    }
}
