using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    // --- VARIÁVEIS DE MOVIMENTO ---
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

    // --- VARIÁVEIS DE CÂMERA ---
    [Header("Câmera e Rotação")]
    public Transform cameraTransform;
    public float sensitivityMultiplier = 10.0f; // Nosso multiplicador
    public float mouseSensitivity = 5.0f; // Este valor virá do SettingsManager
    [Tooltip("Ângulo vertical final (em graus) após a trava. 0 = reto, 10 = olhando um pouco para baixo")]
    public float anguloDeTravaFinal = 5f;
    private float xRotation = 0f;
    private bool isVerticalLookLocked = false;
    private float currentMaxLookDown;
    private float currentMaxLookUp;
    public float defaultMaxLookDown = 60f;
    public float defaultMaxLookUp = 0f;

    // --- VARIÁVEIS DE INTERAÇÃO ---
    [Header("Lanterna e Interação")]
    public Transform pickupZoneCenter;
    public float pickupRange = 1f;
    public Vector2 pickupAreaSize = new Vector2(1f, 2f);
    public float distanciaMaximaParaPortaFinal = 2.0f;
    public float distanciaDestaquePortaFinal = 5.0f;
    public LayerMask pickupLayer;
    public LayerMask portaLayer;
    public GameObject heldFlashlightObject;
    private bool hasFlashlight = false;
    private Light heldFlashlightLight;
    private FlashlightItem lastHighlightedItem = null;
    private HighlightableObject portaSendoDestacada = null;

    // --- OUTRAS VARIÁVEIS ---
    [Header("Rotação da Lanterna")]
    private Transform activeFlashlightTransform;
    public float maxFlashlightAngle = 25f;
    private float flashlightYaw = 0f;
    [Header("Áudio da Lanterna")]
    public AudioSource flashlightAudioSource;
    public AudioClip flashlightOnClip;
    public AudioClip flashlightOffClip;
    private float interactionHighlightTimer;
    private const float INTERACTION_HIGHLIGHT_INTERVAL = 0.1f;
    private Coroutine pickupCoroutine;

    // --- MUDANÇA (REFERÊNCIAS DE INPUT) ---
    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction flashlightAction;

    [Header("Nomes das Ações (EXATOS do Input Asset)")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string flashlightActionName = "ToggleFlashlight";
    // --- FIM DA MUDANÇA ---

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (heldFlashlightObject != null)
        {
            heldFlashlightObject.SetActive(false);
        }

        // --- MUDANÇA (PEGANDO INPUTS DO SETTINGS MANAGER) ---
        if (SettingsManager.Instance == null || SettingsManager.Instance.playerActions == null)
        {
            Debug.LogError("PlayerController: SettingsManager ou PlayerActions não encontrados! Keybinds não funcionarão.", this);
            podeMover = false;
            return;
        }
        playerMap = SettingsManager.Instance.playerActions.FindActionMap("Player");
        if (playerMap == null)
        {
            Debug.LogError("PlayerController: Não foi possível encontrar o Action Map 'Player' no Asset do SettingsManager!", this);
            podeMover = false;
            return;
        }
        moveAction = playerMap.FindAction(moveActionName);
        lookAction = playerMap.FindAction(lookActionName);
        sprintAction = playerMap.FindAction(sprintActionName);
        interactAction = playerMap.FindAction(interactActionName);
        flashlightAction = playerMap.FindAction(flashlightActionName);
        interactAction.performed += Interact;
        flashlightAction.performed += ToggleFlashlight;
        // --- FIM DA MUDANÇA ---

        currentMaxLookDown = defaultMaxLookDown;
        currentMaxLookUp = defaultMaxLookUp;
        mouseSensitivity = SettingsManager.Instance.GetSensitivity();
        SettingsManager.OnSensitivityChanged += HandleSensitivityChanged;
        GameManager.OnGameOver += HandleGameOver;
    }

    private void OnEnable()
    {
        playerMap?.Enable();
    }

    private void OnDisable()
    {
        playerMap?.Disable();
    }

    private void HandleSensitivityChanged(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }

    void Update()
    {
        if (!podeMover || playerMap == null) return;
        HandleMovement();
        HandleLook();
        interactionHighlightTimer += Time.deltaTime;
        if (interactionHighlightTimer >= INTERACTION_HIGHLIGHT_INTERVAL)
        {
            HandleInteractionHighlight();
            interactionHighlightTimer = 0f;
        }
    }

    void HandleMovement()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
            playerVelocity.y = 0f;
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        IsMoving = moveInput.magnitude > 0.1f;
        bool isSprintingInput = sprintAction.IsPressed();
        currentSpeed = isSprintingInput ? sprintSpeed : playerSpeed;
        IsRunning = isSprintingInput && IsMoving;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
        CurrentVelocity = controller.velocity;
    }

    void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float finalSensitivity = mouseSensitivity * sensitivityMultiplier;
        float mouseX = lookInput.x * finalSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * finalSensitivity * Time.deltaTime;

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
                if (xRotation < currentMaxLookDown)
                {
                    currentMaxLookDown = xRotation;
                }
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
            if (Mathf.Abs(flashlightYaw + mouseX) > maxFlashlightAngle)
                transform.Rotate(Vector3.up * mouseX);
            else
                flashlightYaw += mouseX;
            activeFlashlightTransform.localRotation = Quaternion.Euler(0f, flashlightYaw, 0f);
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    private void HandleInteractionHighlight()
    {
        if (InteractionPromptUI.Instance == null) return;
        bool isLightOn = heldFlashlightLight != null && heldFlashlightLight.enabled;
        HighlightableObject portaProxima = null;
        float distanciaMinimaPorta = float.MaxValue;
        Collider[] portasNaArea = Physics.OverlapSphere(transform.position, distanciaDestaquePortaFinal, portaLayer);

        foreach (var col in portasNaArea)
        {
            if (col.CompareTag("PortaFinal"))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < distanciaMinimaPorta)
                {
                    distanciaMinimaPorta = dist;
                    portaProxima = col.GetComponent<HighlightableObject>();
                }
            }
        }
        bool promptMostrado = false;
        if (portaProxima != null)
        {
            if (distanciaMinimaPorta <= distanciaMaximaParaPortaFinal && isLightOn)
            {
                InteractionPromptUI.Instance.ShowPrompt(interactActionName, "to Open Door");
                promptMostrado = true;
            }
            else if (isLightOn)
            {
                if (portaSendoDestacada != portaProxima)
                {
                    if (portaSendoDestacada != null) portaSendoDestacada.RemoveHighlight();
                    portaSendoDestacada = portaProxima;
                    portaSendoDestacada.Highlight();
                }
            }
            else
            {
                if (portaSendoDestacada != null)
                {
                    portaSendoDestacada.RemoveHighlight();
                    portaSendoDestacada = null;
                }
            }
        }
        else
        {
            if (portaSendoDestacada != null)
            {
                portaSendoDestacada.RemoveHighlight();
                portaSendoDestacada = null;
            }
        }
        if (hasFlashlight)
        {
            if (lastHighlightedItem != null)
            {
                lastHighlightedItem.RemoveHighlight();
                lastHighlightedItem = null;
            }
            if (!promptMostrado) InteractionPromptUI.Instance.HidePrompt();
            return;
        }
        Vector3 boxCenter = pickupZoneCenter.position;
        Vector3 halfExtents = new Vector3(pickupAreaSize.x / 2, pickupAreaSize.y / 2, pickupRange / 2);
        Collider[] colliders = Physics.OverlapBox(boxCenter, halfExtents, pickupZoneCenter.rotation, pickupLayer);
        FlashlightItem closestItem = null;
        float closestDistance = float.MaxValue;
        foreach (var col in colliders)
        {
            if (col.CompareTag("Interactable"))
            {
                FlashlightItem item = col.GetComponent<FlashlightItem>();
                if (item != null && item.canBePickedUp)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestItem = item;
                    }
                }
            }
        }
        if (closestItem != lastHighlightedItem)
        {
            if (lastHighlightedItem != null) lastHighlightedItem.RemoveHighlight();
            if (closestItem != null) closestItem.Highlight();
            lastHighlightedItem = closestItem;
        }
        if (closestItem != null && !promptMostrado)
        {
            InteractionPromptUI.Instance.ShowPrompt(interactActionName, "to Collect");
            promptMostrado = true;
        }
        if (!promptMostrado)
        {
            InteractionPromptUI.Instance.HidePrompt();
        }
    }

    // --- MUDANÇA (ASSINATURA DO MÉTODO) ---
    private void Interact(InputAction.CallbackContext context)
    {
        if (InteractionPromptUI.Instance == null) return;
        if (lastHighlightedItem != null)
        {
            if (pickupCoroutine != null)
            {
                StopCoroutine(pickupCoroutine);
            }
            pickupCoroutine = StartCoroutine(PickupFlashlightRoutine(lastHighlightedItem));
            lastHighlightedItem = null;
            InteractionPromptUI.Instance.HidePrompt();
            return;
        }
        bool isLightOn = heldFlashlightLight != null && heldFlashlightLight.enabled;
        if (portaSendoDestacada != null)
        {
            float distanceToDoor = Vector3.Distance(transform.position, portaSendoDestacada.transform.position);
            if (distanceToDoor <= distanciaMaximaParaPortaFinal && isLightOn)
            {
                FecharJogo();
                return;
            }
        }
    }

    private IEnumerator PickupFlashlightRoutine(FlashlightItem item)
    {
        hasFlashlight = true;
        heldFlashlightObject.SetActive(true);
        activeFlashlightTransform = heldFlashlightObject.transform;
        heldFlashlightLight = heldFlashlightObject.GetComponentInChildren<Light>();
        if (heldFlashlightLight == null)
        {
            Debug.LogError("PlayerController: Objeto da lanterna não tem um componente 'Light'!");
        }
        item.OnPickup();
        MonsterController monster = FindFirstObjectByType<MonsterController>();
        if (monster != null)
        {
            monster.ActivateMonster();
        }
        else
        {
            Debug.LogWarning("Player não conseguiu encontrar o MonsterController para ativar");
        }
        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.ShowPrompt(flashlightActionName, "to Toggle Flashlight", 5.0f);
        yield return null;
    }

    // --- MUDANÇA (ASSINATURA DO MÉTODO) ---
    private void ToggleFlashlight(InputAction.CallbackContext context)
    {
        if (!hasFlashlight || heldFlashlightLight == null) return;
        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.HidePrompt();
        bool newState = !heldFlashlightLight.enabled;
        heldFlashlightLight.enabled = newState;
        if (flashlightAudioSource != null)
        {
            flashlightAudioSource.clip = newState ? flashlightOnClip : flashlightOffClip;
            flashlightAudioSource.pitch = Random.Range(0.95f, 1.05f);
            flashlightAudioSource.Play();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.linearVelocity = pushDir * pushPower;
    }

    void OnDrawGizmosSelected()
    {
        if (pickupZoneCenter == null) return;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(pickupZoneCenter.position, pickupZoneCenter.rotation, Vector3.one);
        Vector3 boxSize = new Vector3(pickupAreaSize.x, pickupAreaSize.y, pickupRange);
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }

    private void FecharJogo()
    {
        Debug.Log("Porta encontrada, iniciando sequência de vitória");
        GameManager.Instance.TriggerGameWin();
    }

    private void HandleGameOver()
    {
        Debug.Log("PLAYER: Game Over, congelando.");
        podeMover = false;
        if (controller != null)
            controller.enabled = false;
        enabled = false;
    }

    // --- MUDANÇA (LIMPEZA DE EVENTOS) ---
    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        SettingsManager.OnSensitivityChanged -= HandleSensitivityChanged;

        if (interactAction != null)
        {
            interactAction.performed -= Interact;
        }
        if (flashlightAction != null)
        {
            flashlightAction.performed -= ToggleFlashlight;
        }
    }
}