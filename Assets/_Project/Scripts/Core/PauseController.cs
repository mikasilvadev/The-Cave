using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PauseController : MonoBehaviour
{
    public GameObject pauseMenuCanvas;
    public PlayerController playerController;
    private bool isPaused = false;
    private InputActionMap uiMap;
    private InputActionMap playerMap;
    private CanvasGroup pauseCanvasGroup;

    void Start()
    {
        if (pauseMenuCanvas == null)
        {
            Debug.LogError("PauseController: A variável 'Pause Menu Canvas' não foi arrastada no Inspector do _GameManager!", this);
            return;
        }

        pauseCanvasGroup = pauseMenuCanvas.GetComponent<CanvasGroup>();
        if (pauseCanvasGroup == null)
        {
            Debug.LogWarning($"PauseController: O objeto '{pauseMenuCanvas.name}' não tem um CanvasGroup. Recomendo adicionar um para melhor controle da UI.", pauseMenuCanvas);
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (SettingsManager.Instance != null && SettingsManager.Instance.playerActions != null)
        {
            uiMap = SettingsManager.Instance.playerActions.FindActionMap("UI");
            playerMap = SettingsManager.Instance.playerActions.FindActionMap("Player");
            ForceEventSystemToUseInstance(SettingsManager.Instance.playerActions);
            if (uiMap != null) uiMap.Disable();
            if (playerMap != null) playerMap.Enable();
        }
        else
        {
            Debug.LogError("PauseController: SettingsManager ou playerActions não encontrados no Start!");
        }

        HideMenu();
    }

    private void ForceEventSystemToUseInstance(InputActionAsset assetInstance)
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null) return;
        InputSystemUIInputModule uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (uiModule != null)
            uiModule.actionsAsset = assetInstance;
    }

    void Update()
    {
        if (pauseMenuCanvas == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            if (InteractionPromptUI.Instance != null)
                InteractionPromptUI.Instance.HidePrompt();

            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerController != null)
                playerController.enabled = false;

            ShowMenu();

            if (playerMap != null) playerMap.Disable();
            if (uiMap != null) uiMap.Enable();
        }
        else
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController != null)
                playerController.enabled = true;

            HideMenu();

            if (uiMap != null) uiMap.Disable();
            if (playerMap != null) playerMap.Enable();

            SettingsManager.TriggerBindingsChanged();

        }
    }

    private void ShowMenu()
    {
        pauseMenuCanvas.SetActive(true);
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 1f;
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideMenu()
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            pauseMenuCanvas.SetActive(false);
        }
    }
}