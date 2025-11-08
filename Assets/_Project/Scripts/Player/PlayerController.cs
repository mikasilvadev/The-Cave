using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

public class PlayerController : MonoBehaviour
{
    #region Variáveis
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private float currentSpeed;

    [Header("Configurações de Movimento")]
    public float playerSpeed = 8.0f;
    public float sprintSpeed = 10.0f;
    public float gravityValue = -9.81f;
    public float pushPower = 2.0f;

    public Vector3 CurrentVelocity { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }
    public bool podeMover = true;

    [Header("Câmera e Rotação")]
    public Transform cameraTransform;
    public float sensitivityMultiplier = 20.0f;
    public float mouseSensitivity = 5.0f;
    public float anguloDeTravaFinal = 5f;
    private float xRotation = 0f;
    private bool isVerticalLookLocked = false;
    private float currentMaxLookDown;
    private float currentMaxLookUp;
    public float defaultMaxLookDown = 60f;
    public float defaultMaxLookUp = 0f;

    [Header("Lanterna e Interação")]
    public float flashlightHighlightRange = 4.0f;
    public float distanciaMaximaParaPortaFinal = 2.0f;
    public float distanciaDestaquePortaFinal = 5.0f;
    public LayerMask pickupLayer;
    public LayerMask portaLayer;
    public GameObject heldFlashlightObject;
    private bool hasFlashlight = false;
    private Light heldFlashlightLight;
    private FlashlightItem lastHighlightedItem = null;
    private HighlightableObject portaSendoDestacada = null;
    private Transform activeFlashlightTransform;
    public float maxFlashlightAngle = 25f;
    private float flashlightYaw = 0f;
    public AudioSource flashlightAudioSource;
    public AudioClip flashlightOnClip;
    public AudioClip flashlightOffClip;

    private float interactionHighlightTimer;
    private const float INTERACTION_HIGHLIGHT_INTERVAL = 0.1f;
    private Coroutine pickupCoroutine;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;

    [Header("Nomes das Ações (EXATOS do Input Asset)")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string flashlightActionName = "ToggleFlashlight";

    [Header("UI")]
    public TextMeshProUGUI hideInstructionText;
    private bool hideInstructionShown;
    #endregion

    #region Métodos Iniciais
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (heldFlashlightObject != null) heldFlashlightObject.SetActive(false);
        currentMaxLookDown = defaultMaxLookDown;
        currentMaxLookUp = defaultMaxLookUp;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        InitializeBasicInputs();

        if (hideInstructionText != null)
        {
            hideInstructionText.gameObject.SetActive(false);
            RefreshHideInstructionText();
        }

        if (SettingsManager.Instance != null)
        {
            mouseSensitivity = SettingsManager.Instance.GetSensitivity();
            SettingsManager.OnSensitivityChanged += HandleSensitivityChanged;
            SettingsManager.OnBindingsChanged += HandleBindingsChanged;
        }

        GameManager.OnGameOver += HandleGameOver;
        StartCoroutine(KickstartControls());
    }

    private IEnumerator KickstartControls()
    {
        yield return new WaitForSeconds(2.0f);
        if (SettingsManager.Instance != null && SettingsManager.Instance.playerActions != null)
        {
            SettingsManager.Instance.playerActions.FindActionMap("Player").Disable();
            yield return null;
            SettingsManager.Instance.playerActions.FindActionMap("Player").Enable();
        }
    }

    private void InitializeBasicInputs()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.playerActions == null) return;
        var map = SettingsManager.Instance.playerActions.FindActionMap("Player");
        if (map != null)
        {
            moveAction = map.FindAction(moveActionName);
            lookAction = map.FindAction(lookActionName);
            sprintAction = map.FindAction(sprintActionName);
            map.Enable();
        }
    }
    #endregion

    #region Update Loop
    void Update()
    {
        if (podeMover && SettingsManager.Instance?.playerActions != null)
        {
            var playerMap = SettingsManager.Instance.playerActions.FindActionMap("Player");
            if (playerMap != null && !playerMap.enabled) playerMap.Enable();
        }

        if (!podeMover) return;

        HandleMovement();
        HandleLook();

        if (SettingsManager.Instance?.playerActions != null)
        {
            InputAction interact = SettingsManager.Instance.playerActions.FindAction(interactActionName);
            if (interact != null && (interact.WasPerformedThisFrame() || interact.WasPressedThisFrame()))
                TryInteract();

            InputAction flashlight = SettingsManager.Instance.playerActions.FindAction(flashlightActionName);
            if (flashlight != null && flashlight.WasPerformedThisFrame())
                TryToggleFlashlight();
        }

        interactionHighlightTimer += Time.deltaTime;
        if (interactionHighlightTimer >= INTERACTION_HIGHLIGHT_INTERVAL)
        {
            HandleInteractionHighlight();
            interactionHighlightTimer = 0f;
        }
    }
    #endregion

    #region Movimento e Câmera
    void HandleMovement()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0) playerVelocity.y = 0f;

        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            IsMoving = moveInput.magnitude > 0.1f;
            bool isSprintingInput = sprintAction != null && sprintAction.IsPressed();
            currentSpeed = isSprintingInput ? sprintSpeed : playerSpeed;
            IsRunning = isSprintingInput && IsMoving;
            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            controller.Move(move * currentSpeed * Time.deltaTime);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
        CurrentVelocity = controller.velocity;
    }

    void HandleLook()
    {
        if (lookAction == null) return;
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float finalSens = mouseSensitivity * sensitivityMultiplier;
        float mouseX = lookInput.x * finalSens * Time.deltaTime;
        float mouseY = lookInput.y * finalSens * Time.deltaTime;

        if (hasFlashlight && isVerticalLookLocked)
        {
            xRotation = anguloDeTravaFinal;
            currentMaxLookUp = anguloDeTravaFinal;
            currentMaxLookDown = anguloDeTravaFinal;
        }
        else
        {
            xRotation -= mouseY;
            if (hasFlashlight)
            {
                currentMaxLookUp = defaultMaxLookUp;
                if (xRotation < currentMaxLookDown) currentMaxLookDown = xRotation;
                if (currentMaxLookDown <= anguloDeTravaFinal)
                {
                    isVerticalLookLocked = true;
                    currentMaxLookDown = anguloDeTravaFinal;
                    xRotation = anguloDeTravaFinal;
                }
            }
            else
            {
                currentMaxLookUp = defaultMaxLookUp;
                currentMaxLookDown = defaultMaxLookDown;
            }
            xRotation = Mathf.Clamp(xRotation, currentMaxLookUp, currentMaxLookDown);
        }

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (activeFlashlightTransform != null)
        {
            if (Mathf.Abs(flashlightYaw + mouseX) > maxFlashlightAngle) transform.Rotate(Vector3.up * mouseX);
            else flashlightYaw += mouseX;
            activeFlashlightTransform.localRotation = Quaternion.Euler(0f, flashlightYaw, 0f);
        }
        else transform.Rotate(Vector3.up * mouseX);
    }
    #endregion

    #region Interação e Destaque
    private void HandleInteractionHighlight()
    {
        if (!hasFlashlight)
        {
            Physics.SyncTransforms();
            Collider[] colliders = Physics.OverlapSphere(transform.position, flashlightHighlightRange, pickupLayer);
            FlashlightItem closestItem = null;
            float closestDist = float.MaxValue;
            foreach (var col in colliders)
            {
                if (col.CompareTag("Interactable"))
                {
                    FlashlightItem item = col.GetComponent<FlashlightItem>();
                    if (item != null && item.canBePickedUp)
                    {
                        float d = Vector3.Distance(transform.position, col.transform.position);
                        if (d < closestDist) { closestDist = d; closestItem = item; }
                    }
                }
            }

            if (closestItem != lastHighlightedItem)
            {
                if (lastHighlightedItem != null) lastHighlightedItem.RemoveHighlight();
                if (closestItem != null) closestItem.Highlight();
                lastHighlightedItem = closestItem;
            }
        }
        else
        {
            if (lastHighlightedItem != null) { lastHighlightedItem.RemoveHighlight(); lastHighlightedItem = null; }

            if (heldFlashlightLight != null && heldFlashlightLight.enabled)
            {
                Collider[] portas = Physics.OverlapSphere(transform.position, distanciaDestaquePortaFinal, portaLayer);
                HighlightableObject portaProx = null;
                float minDist = float.MaxValue;
                foreach (var c in portas)
                    if (c.CompareTag("PortaFinal"))
                    {
                        float d = Vector3.Distance(transform.position, c.transform.position);
                        if (d < minDist) { minDist = d; portaProx = c.GetComponent<HighlightableObject>(); }
                    }

                if (portaProx != null && minDist <= distanciaMaximaParaPortaFinal)
                    GameManager.Instance.TriggerGameWin();

                if (portaSendoDestacada != portaProx)
                {
                    if (portaSendoDestacada != null) portaSendoDestacada.RemoveHighlight();
                    portaSendoDestacada = portaProx;
                    if (portaSendoDestacada != null) portaSendoDestacada.Highlight();
                }
            }
            else if (portaSendoDestacada != null)
            {
                portaSendoDestacada.RemoveHighlight();
                portaSendoDestacada = null;
            }
        }
    }
    #endregion

    #region Interagir e Lanterna
    private void TryInteract()
    {
        if (lastHighlightedItem != null && !hasFlashlight)
        {
            if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
            pickupCoroutine = StartCoroutine(PickupFlashlightRoutine(lastHighlightedItem));
            lastHighlightedItem = null;
        }
    }

    private IEnumerator PickupFlashlightRoutine(FlashlightItem item)
    {
        hasFlashlight = true;
        if (item != null) item.OnPickup();
        heldFlashlightObject.SetActive(true);
        activeFlashlightTransform = heldFlashlightObject.transform;
        heldFlashlightLight = heldFlashlightObject.GetComponentInChildren<Light>();

        MonsterController monster = FindFirstObjectByType<MonsterController>();
        if (monster != null) monster.ActivateMonster();

        ShowHideInstructionPrompt();
        yield return null;
    }

    private void TryToggleFlashlight()
    {
        if (hideInstructionShown)
        {
            hideInstructionText.gameObject.SetActive(false);
            hideInstructionShown = false;
        }

        if (!hasFlashlight || heldFlashlightLight == null) return;

        bool newState = !heldFlashlightLight.enabled;
        heldFlashlightLight.enabled = newState;
        if (flashlightAudioSource != null)
        {
            flashlightAudioSource.clip = newState ? flashlightOnClip : flashlightOffClip;
            flashlightAudioSource.pitch = Random.Range(0.95f, 1.05f);
            flashlightAudioSource.Play();
        }
    }
    #endregion

    #region UI e Eventos
    private void ShowHideInstructionPrompt()
    {
        if (hideInstructionText == null)
        {
            Debug.LogWarning("PlayerController: 'hideInstructionText' não foi arrastado no Inspector!");
            return;
        }
        RefreshHideInstructionText();
        hideInstructionText.gameObject.SetActive(true);
        hideInstructionShown = true;
    }

    private void RefreshHideInstructionText()
    {
        if (hideInstructionText == null) return;
        string keyLabel = "?";
        if (SettingsManager.Instance != null)
        {
            string bindingLabel = SettingsManager.Instance.GetBindingDisplayName(flashlightActionName);
            if (!string.IsNullOrEmpty(bindingLabel))
                keyLabel = bindingLabel;
        }
        hideInstructionText.text = $"Press [{keyLabel}] to Hide from the monster";
    }

    private void HandleSensitivityChanged(float newSens) => mouseSensitivity = newSens;

    private void HandleBindingsChanged()
    {
        // Atualiza instruções de UI ao trocar teclas
        if (hideInstructionShown)
            RefreshHideInstructionText();
    }

    private void HandleGameOver()
    {
        podeMover = false;
        if (controller != null) controller.enabled = false;
        enabled = false;
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        if (SettingsManager.Instance != null)
        {
            SettingsManager.OnSensitivityChanged -= HandleSensitivityChanged;
            SettingsManager.OnBindingsChanged -= HandleBindingsChanged;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.25f);
        Gizmos.DrawSphere(transform.position, flashlightHighlightRange);
        Gizmos.color = new Color(0, 0, 1, 0.25f);
        Gizmos.DrawSphere(transform.position, distanciaDestaquePortaFinal);
    }
    #endregion
}
