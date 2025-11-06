using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("Referências")]
    public GameObject promptVisuals;
    public TextMeshProUGUI promptText;

    private Coroutine hideCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        if (promptVisuals != null)
            promptVisuals.SetActive(false);
    }

    public void ShowPrompt(string actionName, string actionText)
    {
        string keyName = GetCleanKeyName(actionName);

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
        if (promptVisuals != null)
            promptVisuals.SetActive(false);
    }

    private string GetCleanKeyName(string actionName)
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.playerActions == null)
            return "?";

        var action = SettingsManager.Instance.playerActions.FindAction(actionName);
        if (action == null) return "NONE";

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
            return InputControlPath.ToHumanReadableString(
                action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice | InputControlPath.HumanReadableStringOptions.UseShortNames
            ).ToUpper();
        }

        return "NONE";
    }

    private System.Collections.IEnumerator HideAfterSeconds(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        HidePrompt();
    }
}