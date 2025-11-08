using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System;
using UnityEngine.InputSystem.Utilities;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Configurações")]
    public AudioMixer mainMixer;
    public InputActionAsset playerActions;
    public static event Action<float> OnSensitivityChanged;
    public static event Action OnBindingsChanged;

    public const string SENSITIVITY_KEY = "MouseSensitivity";
    public const string VOLUME_KEY = "MasterVolume";
    public const string BINDINGS_KEY = "InputBindings";

    public const float DEFAULT_SENSITIVITY = 0.5f;
    public const float DEFAULT_VOLUME = 0.8f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void LoadSettings()
    {
        float volume = PlayerPrefs.GetFloat(VOLUME_KEY, DEFAULT_VOLUME);
        SetVolume(volume);

        float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, DEFAULT_SENSITIVITY);
        SetSensitivity(sensitivity);

        string bindings = PlayerPrefs.GetString(BINDINGS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(bindings))
        {
            playerActions.LoadBindingOverridesFromJson(bindings);
        }
    }

    public void SetVolume(float sliderValue)
    {
        float volumeDb = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        mainMixer.SetFloat("MasterVolume", volumeDb);
        PlayerPrefs.SetFloat(VOLUME_KEY, sliderValue);
    }

    public float GetVolume()
    {
        return PlayerPrefs.GetFloat(VOLUME_KEY, DEFAULT_VOLUME);
    }

    public void SetSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, sensitivity);
        OnSensitivityChanged?.Invoke(sensitivity);
    }

    public float GetSensitivity()
    {
        return PlayerPrefs.GetFloat(SENSITIVITY_KEY, DEFAULT_SENSITIVITY);
    }

    public string GetBindingDisplayName(string actionName)
    {
        if (playerActions == null || string.IsNullOrEmpty(actionName)) return string.Empty;

        var action = playerActions.FindAction(actionName);
        if (action == null) return string.Empty;

        int bindingIndex = -1;

        if (action.controls.Count > 0)
        {
            bindingIndex = action.GetBindingIndexForControl(action.controls[0]);
        }

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

        if (bindingIndex == -1) return string.Empty;

        return InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice | InputControlPath.HumanReadableStringOptions.UseShortNames
        ).ToUpper();
    }

    public void SaveKeybinds()
    {
        string bindings = playerActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BINDINGS_KEY, bindings);

        OnBindingsChanged?.Invoke();
    }

    public static void TriggerBindingsChanged()
    {
        OnBindingsChanged?.Invoke();
    }

}