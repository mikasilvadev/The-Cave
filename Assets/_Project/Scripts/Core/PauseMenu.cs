using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    [Header("Referências da UI")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;
    public Slider volumeSlider;
    public TextMeshProUGUI volumeValueText;
    public Button exitButton;

    [Header("Keybinds UI")]
    public Button interactButton;
    public TextMeshProUGUI interactButtonText;
    public Button flashlightButton;
    public TextMeshProUGUI flashlightButtonText;
    public Button sprintButton;
    public TextMeshProUGUI sprintButtonText;
    public Text interactionKeyLabel; // arraste no inspector para mostrar tecla atual

    [Header("Ações (Nomes EXATOS do Input Asset)")]
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string flashlightActionName = "ToggleFlashlight";
    [SerializeField] private string sprintActionName = "Sprint";

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    void OnEnable()
    {
        ToggleUIInteractable(true);
        RemoveAllListeners();
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        exitButton.onClick.AddListener(OnExitClicked);
        interactButton.onClick.AddListener(() => StartRebinding(interactActionName, interactButtonText));
        flashlightButton.onClick.AddListener(() => StartRebinding(flashlightActionName, flashlightButtonText));
        sprintButton.onClick.AddListener(() => StartRebinding(sprintActionName, sprintButtonText));
        LoadSettingsToUI();
    }

    void OnDisable()
    {
        RemoveAllListeners();
        rebindingOperation?.Cancel();
        rebindingOperation?.Dispose();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveKeybinds();
        }
    }

    private void RemoveAllListeners()
    {
        sensitivitySlider.onValueChanged.RemoveAllListeners();
        volumeSlider.onValueChanged.RemoveAllListeners();
        exitButton.onClick.RemoveAllListeners();
        interactButton.onClick.RemoveAllListeners();
        flashlightButton.onClick.RemoveAllListeners();
        sprintButton.onClick.RemoveAllListeners();
    }

    private void LoadSettingsToUI()
    {
        if (SettingsManager.Instance == null) return;

        float sensitivity = SettingsManager.Instance.GetSensitivity();
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        UpdateSensitivityText(sensitivity);

        float volume = SettingsManager.Instance.GetVolume();
        volumeSlider.SetValueWithoutNotify(volume);
        UpdateVolumeText(volume);

        UpdateBindingText(interactActionName, interactButtonText);
        UpdateBindingText(flashlightActionName, flashlightButtonText);
        UpdateBindingText(sprintActionName, sprintButtonText);

        if (interactionKeyLabel != null)
            interactionKeyLabel.text = SettingsManager.InteractionKey.ToString();
    }

    public void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.SetSensitivity(value);
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F1");
    }

    public void OnVolumeChanged(float value)
    {
        SettingsManager.Instance.SetVolume(value);
        UpdateVolumeText(value);
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = (value * 100).ToString("F0") + "%";
    }

    public void OnExitClicked()
    {
        GameManager.Instance.QuitGame();
    }

    private void StartRebinding(string actionName, TextMeshProUGUI buttonText)
    {
        InputAction action = SettingsManager.Instance.playerActions.FindAction(actionName);
        if (action == null) return;

        ToggleUIInteractable(false);
        buttonText.text = "...";

        rebindingOperation?.Cancel();
        action.Disable();

        rebindingOperation = action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinishRebinding(operation, action, actionName, buttonText))
            .OnCancel(operation => FinishRebinding(operation, action, actionName, buttonText));

        rebindingOperation.Start();
    }

    private void FinishRebinding(InputActionRebindingExtensions.RebindingOperation operation, InputAction action, string actionName, TextMeshProUGUI buttonText)
    {
        operation.Dispose();
        rebindingOperation = null;

        action.Enable();
        UpdateBindingText(actionName, buttonText);
        ToggleUIInteractable(true);
    }

    private void UpdateBindingText(string actionName, TextMeshProUGUI buttonText)
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.playerActions == null) return;

        var action = SettingsManager.Instance.playerActions.FindAction(actionName);
        if (action != null)
        {
            int bindingIndex = action.GetBindingIndexForControl(action.controls[0]);
            if (bindingIndex == -1)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (!action.bindings[i].isPartOfComposite)
                    {
                        bindingIndex = i;
                        break;
                    }
                }
            }

            if (bindingIndex != -1)
            {
                buttonText.text = InputControlPath.ToHumanReadableString(
                    action.bindings[bindingIndex].effectivePath,
                    InputControlPath.HumanReadableStringOptions.OmitDevice | InputControlPath.HumanReadableStringOptions.UseShortNames
                ).ToUpper();
            }
            else
            {
                buttonText.text = "NONE";
            }
        }
        else
        {
            buttonText.text = "ERR";
        }
    }

    private void ToggleUIInteractable(bool isInteractable)
    {
        sensitivitySlider.interactable = isInteractable;
        volumeSlider.interactable = isInteractable;
        exitButton.interactable = isInteractable;
        interactButton.interactable = isInteractable;
        flashlightButton.interactable = isInteractable;
        sprintButton.interactable = isInteractable;
    }

    // Chame este método via OnClick do botão "Redefinir Interagir" no menu de pause
    public void StartRebindInteractionKey()
    {
        StartCoroutine(WaitForKeyPressAndBind());
        if (interactionKeyLabel != null)
            interactionKeyLabel.text = "Pressione uma tecla...";
    }

    private IEnumerator WaitForKeyPressAndBind()
    {
        bool bound = false;
        while (!bound)
        {
            // varre todos os KeyCodes simples — suficiente para rebind comum
            foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kc))
                {
                    SettingsManager.SetInteractionKey(kc, true);
                    if (interactionKeyLabel != null)
                        interactionKeyLabel.text = kc.ToString();
                    bound = true;
                    break;
                }
            }
            yield return null;
        }
    }
}