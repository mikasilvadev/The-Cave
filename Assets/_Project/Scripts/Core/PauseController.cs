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

    // A referência para o CanvasGroup (pode ser nula se não existir)
    private CanvasGroup pauseCanvasGroup;

    void Start()
    {
        if (pauseMenuCanvas == null)
        {
            Debug.LogError("PauseController: A variável 'Pause Menu Canvas' não foi arrastada no Inspector do _GameManager!", this);
            return; // Para o script aqui
        }

        // Pega o CanvasGroup, mas não dá erro se não achar
        pauseCanvasGroup = pauseMenuCanvas.GetComponent<CanvasGroup>();
        if (pauseCanvasGroup == null)
        {
            Debug.LogWarning($"PauseController: O objeto '{pauseMenuCanvas.name}' não tem um CanvasGroup. Recomendo adicionar um para melhor controle da UI.", pauseMenuCanvas);
        }

        // Encontra o Player
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        // Pega os mapas do SettingsManager
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

        // Esconde o menu no início
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
        // Se a referência principal faltar, não faz nada.
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
            // --- NOVA ADIÇÃO ---
            // Garante que o texto "Press E" suma antes do menu abrir
            if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.HidePrompt();
            }
            // -------------------

            // Pausar
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerController != null)
                playerController.enabled = false;

            ShowMenu(); // Mostra o menu

            if (playerMap != null) playerMap.Disable();
            if (uiMap != null) uiMap.Enable();
        }
        else
        {
            // Despausar
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerController != null)
                playerController.enabled = true;

            HideMenu(); // Esconde o menu

            if (uiMap != null) uiMap.Disable();
            if (playerMap != null) playerMap.Enable();
        }
    }

    private void ShowMenu()
    {
        pauseMenuCanvas.SetActive(true); // LIGA o GameObject
        if (pauseCanvasGroup != null)
        {
            // Se temos um CanvasGroup, usa ele
            pauseCanvasGroup.alpha = 1f;
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HideMenu()
    {
        if (pauseCanvasGroup != null)
        {
            // Se temos um CanvasGroup, usa ele para esconder
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            // Se não temos, apenas desliga o GameObject
            pauseMenuCanvas.SetActive(false);
        }
    }
}