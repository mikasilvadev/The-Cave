using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
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
    public float sensitivityMultiplier = 10.0f;
    public float mouseSensitivity = 5.0f;
    public float anguloDeTravaFinal = 5f;
    private float xRotation = 0f;
    private bool isVerticalLookLocked = false;
    private float currentMaxLookDown;
    private float currentMaxLookUp;
    public float defaultMaxLookDown = 60f;
    public float defaultMaxLookUp = 0f;

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
        if (SettingsManager.Instance != null)
        {
            mouseSensitivity = SettingsManager.Instance.GetSensitivity();
            SettingsManager.OnSensitivityChanged += HandleSensitivityChanged;
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

    void Update()
    {
        if (!podeMover) return;

        HandleMovement();
        HandleLook();

        if (SettingsManager.Instance != null && SettingsManager.Instance.playerActions != null)
        {
            InputAction interact = SettingsManager.Instance.playerActions.FindAction(interactActionName);
            if (interact != null && (interact.WasPerformedThisFrame() || interact.WasPressedThisFrame()))
            {
                TryInteract();
            }

            InputAction flashlight = SettingsManager.Instance.playerActions.FindAction(flashlightActionName);
            if (flashlight != null && flashlight.WasPerformedThisFrame())
            {
                TryToggleFlashlight();
            }
        }

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
            currentMaxLookUp = anguloDeTravaFinal; currentMaxLookDown = anguloDeTravaFinal;
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
                    isVerticalLookLocked = true; currentMaxLookDown = anguloDeTravaFinal; xRotation = anguloDeTravaFinal;
                }
            }
            else
            {
                currentMaxLookUp = defaultMaxLookUp; currentMaxLookDown = defaultMaxLookDown;
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
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    private void HandleInteractionHighlight()
    {
        if (!hasFlashlight)
        {
            Physics.SyncTransforms();
            Collider[] colliders = Physics.OverlapBox(pickupZoneCenter.position, new Vector3(pickupAreaSize.x / 2, pickupAreaSize.y / 2, pickupRange / 2), pickupZoneCenter.rotation, pickupLayer);
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
            if (InteractionPromptUI.Instance != null)
            {
                if (closestItem != null) InteractionPromptUI.Instance.ShowPrompt(interactActionName, "to Collect");
                else InteractionPromptUI.Instance.HidePrompt();
            }
        }
        else if (heldFlashlightLight != null && heldFlashlightLight.enabled)
        {
            if (lastHighlightedItem != null) { lastHighlightedItem.RemoveHighlight(); lastHighlightedItem = null; }

            Collider[] portas = Physics.OverlapSphere(transform.position, distanciaDestaquePortaFinal, portaLayer);
            HighlightableObject portaProx = null;
            float minDist = float.MaxValue;

            foreach (var c in portas) if (c.CompareTag("PortaFinal"))
                {
                    float d = Vector3.Distance(transform.position, c.transform.position);
                    if (d < minDist) { minDist = d; portaProx = c.GetComponent<HighlightableObject>(); }
                }

            if (portaProx != null && minDist <= distanciaMaximaParaPortaFinal)
            {
                GameManager.Instance.TriggerGameWin();
            }
            else if (InteractionPromptUI.Instance != null)
            {
                InteractionPromptUI.Instance.HidePrompt();
            }

            if (portaSendoDestacada != portaProx)
            {
                if (portaSendoDestacada != null) portaSendoDestacada.RemoveHighlight();
                portaSendoDestacada = portaProx;
                if (portaSendoDestacada != null) portaSendoDestacada.Highlight();
            }
        }
    }

    private void TryInteract()
    {
        if (lastHighlightedItem != null && !hasFlashlight)
        {
            if (pickupCoroutine != null) StopCoroutine(pickupCoroutine);
            pickupCoroutine = StartCoroutine(PickupFlashlightRoutine(lastHighlightedItem));
            lastHighlightedItem = null;
            if (InteractionPromptUI.Instance != null) InteractionPromptUI.Instance.HidePrompt();
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
        if (InteractionPromptUI.Instance != null)
            InteractionPromptUI.Instance.ShowPrompt(flashlightActionName, "to Toggle Flashlight", 5.0f);
        yield return null;
    }

    private void ToggleFlashlight()
    {
        TryToggleFlashlight();
    }

    private void TryToggleFlashlight()
    {
        if (!hasFlashlight || heldFlashlightLight == null) return;
        if (InteractionPromptUI.Instance != null) InteractionPromptUI.Instance.HidePrompt();
        bool newState = !heldFlashlightLight.enabled;
        heldFlashlightLight.enabled = newState;
        if (flashlightAudioSource != null)
        {
            flashlightAudioSource.clip = newState ? flashlightOnClip : flashlightOffClip;
            flashlightAudioSource.pitch = Random.Range(0.95f, 1.05f);
            flashlightAudioSource.Play();
        }
    }

    private void HandleSensitivityChanged(float newSens) => mouseSensitivity = newSens;
    private void HandleGameOver() { podeMover = false; if (controller != null) controller.enabled = false; enabled = false; }
    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
        if (SettingsManager.Instance != null) SettingsManager.OnSensitivityChanged -= HandleSensitivityChanged;
    }
    void OnDrawGizmosSelected()
    {
        if (pickupZoneCenter == null) return;
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.matrix = Matrix4x4.TRS(pickupZoneCenter.position, pickupZoneCenter.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(pickupAreaSize.x, pickupAreaSize.y, pickupRange));
    }
}