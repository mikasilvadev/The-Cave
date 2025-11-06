using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Configurações")]
    public AudioMixer mainMixer;
    public InputActionAsset playerActions;

    public static event Action<float> OnSensitivityChanged;

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

    public void SaveKeybinds()
    {
        string bindings = playerActions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(BINDINGS_KEY, bindings);
    }

    public static class SettingsManager
    {
        // Nova API para tecla de interação (padrão E)
        public const string PREF_INTERACT_KEY = "interact_key";
        private static KeyCode _interactionKey = KeyCode.E;
        public static KeyCode InteractionKey
        {
            get => _interactionKey;
            private set => _interactionKey = value;
        }

        static SettingsManager()
        {
            // tenta carregar de PlayerPrefs (salvo como int)
            if (PlayerPrefs.HasKey(PREF_INTERACT_KEY))
            {
                int saved = PlayerPrefs.GetInt(PREF_INTERACT_KEY);
                try { _interactionKey = (KeyCode)saved; }
                catch { _interactionKey = KeyCode.E; }
            }
        }

        public static void SetInteractionKey(KeyCode newKey, bool save = true)
        {
            _interactionKey = newKey;
            if (save) PlayerPrefs.SetInt(PREF_INTERACT_KEY, (int)newKey);
        }
    }
}