using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("Referências")]
    public GameObject promptVisuals;
    public TextMeshProUGUI promptText;
    private Coroutine hideCoroutine;
    private Dictionary<string, string> keyNameCache = new Dictionary<string, string>();
    private string currentActionName;
    private string currentPromptMessage;
    private bool promptVisible;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        if (promptVisuals != null)
            promptVisuals.SetActive(false);
    }

    void Start()
    {
        SettingsManager.OnBindingsChanged += ClearCache;
    }

    void OnDestroy()
    {
        SettingsManager.OnBindingsChanged -= ClearCache;
    }

    private void ClearCache()
    {
        keyNameCache.Clear();
    }

    void OnEnable()
    {
        SettingsManager.OnBindingsChanged += HandleBindingsChanged;
    }

    void OnDisable()
    {
        SettingsManager.OnBindingsChanged -= HandleBindingsChanged;
    }

    private void HandleBindingsChanged()
    {
        if (!promptVisible || string.IsNullOrEmpty(currentActionName))
            return;

        ShowPrompt(currentActionName, currentPromptMessage);
    }

    public void ShowPrompt(string actionName, string actionText)
    {
        currentActionName = actionName;
        currentPromptMessage = actionText;
        promptVisible = true;

        string keyName = GetCachedKeyName(actionName);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        if (promptText != null)
            promptText.text = $"Press [{keyName}] {actionText}";

        if (promptVisuals != null)
            promptVisuals.SetActive(true);
    }

    public void ShowPrompt(string actionName, string actionText, float duration)
    {
        ShowPrompt(actionName, actionText);
        if (gameObject.activeInHierarchy)
            hideCoroutine = StartCoroutine(HideAfterSeconds(duration));
    }

    public void HidePrompt()
    {
        promptVisible = false;
        currentActionName = null;
        currentPromptMessage = null;

        if (promptVisuals != null)
            promptVisuals.SetActive(false);
    }

    public string GetCachedKeyName(string actionName)
    {
        if (keyNameCache.TryGetValue(actionName, out string cachedName))
        {
            return cachedName;
        }

        if (SettingsManager.Instance == null || SettingsManager.Instance.playerActions == null)
            return "?";

        var action = SettingsManager.Instance.playerActions.FindAction(actionName);
        string result = "NONE";

        if (action != null)
        {
            int bindingIndex = -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!action.bindings[i].isPartOfComposite)
                {
                    bindingIndex = i;
                    break;
                }
            }

            if (bindingIndex != -1)
            {
                result = InputControlPath.ToHumanReadableString(
                    action.bindings[bindingIndex].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice | InputControlPath.HumanReadableStringOptions.UseShortNames
                ).ToUpper();
            }
        }

        keyNameCache[actionName] = result;
        return result;
    }

    private System.Collections.IEnumerator HideAfterSeconds(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        HidePrompt();
    }
}