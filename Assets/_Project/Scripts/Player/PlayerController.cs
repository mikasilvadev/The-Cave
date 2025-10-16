using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    public float playerSpeed = 8.0f;
    public float sprintSpeed = 10.0f;
    private float currentSpeed;
    public float jumpHeight = 1.0f;
    public float gravityValue = -9.81f;
    public float pushPower = 2.0f;
    public Vector3 CurrentVelocity { get; private set; }

    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }

    public bool podeMover = true;

    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    public Transform cameraTransform;
    public float mouseSensitivity = 5.0f;
    private float xRotation = 0f;

    [Header("Flashlight & Inventory")]
    public Transform pickupZoneCenter;
    public float pickupRange = 1f;
    public Vector2 pickupAreaSize = new Vector2(1f, 2f);
    public LayerMask pickupLayer;
    public GameObject heldFlashlightObject;
    public Transform heldFlashlightVisuals;
    public GameObject worldFlashlightPrefab;

    private bool hasFlashlight = false;
    private Light heldFlashlightLight;
    private FlashlightItem lastHighlightedItem = null;

    [Header("Dynamic Flashlight Settings")]
    private Transform activeFlashlightTransform;
    public float maxFlashlightAngle = 25f;
    private float flashlightYaw = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (heldFlashlightObject != null)
        {
            heldFlashlightLight = heldFlashlightObject.GetComponentInChildren<Light>();
            heldFlashlightObject.SetActive(false);
        }

        playerControls = new PlayerControls();
        playerControls.Player.Enable();
        playerControls.Player.Interact.performed += ctx => Interact();
        playerControls.Player.ToggleFlashlight.performed += ctx => ToggleFlashlight();
        // LINHA REMOVIDA: A linha abaixo era a que conectava o botão de drop à função.
        // playerControls.Player.Drop.performed += ctx => DropItem();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }

    void Update()
    {
        if (podeMover)
        {
            HandleMovement();
            HandleLook();
            HandleInteractionHighlight();
        }
    }

    void HandleMovement()
    {
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        IsMoving = moveInput.magnitude > 0.1f;

        bool isSprintingInput = playerControls.Player.Sprint.IsPressed();

        currentSpeed = isSprintingInput ? sprintSpeed : playerSpeed;

        IsRunning = isSprintingInput && IsMoving;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        CurrentVelocity = controller.velocity;
    }

    void HandleLook()
    {
        lookInput = playerControls.Player.Look.ReadValue<Vector2>();
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 50f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (activeFlashlightTransform != null)
        {
            if (Mathf.Abs(flashlightYaw + mouseX) > maxFlashlightAngle)
            {
                transform.Rotate(Vector3.up * mouseX);
            }
            else
            {
                flashlightYaw += mouseX;
            }
            activeFlashlightTransform.localRotation = Quaternion.Euler(0f, flashlightYaw, 0f);
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    private void HandleInteractionHighlight()
    {
        if (hasFlashlight)
        {
            if (lastHighlightedItem != null)
            {
                lastHighlightedItem.RemoveHighlight();
                lastHighlightedItem = null;
            }
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
            if (lastHighlightedItem != null)
            {
                lastHighlightedItem.RemoveHighlight();
            }

            if (closestItem != null)
            {
                closestItem.Highlight();
            }

            lastHighlightedItem = closestItem;
        }
    }

    private void Interact()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            if (hit.collider.CompareTag("PortaFinal"))
            {
                FecharJogo();
                return;
            }
        }

        if (lastHighlightedItem != null)
        {
            PickupFlashlight(lastHighlightedItem);
            lastHighlightedItem = null;
        }
    }

    private void PickupFlashlight(FlashlightItem item)
    {
        hasFlashlight = true;
        heldFlashlightObject.SetActive(true);
        activeFlashlightTransform = heldFlashlightObject.transform;
        item.OnPickup();
    }

    private void ToggleFlashlight()
    {
        if (hasFlashlight && heldFlashlightLight != null)
        {
            heldFlashlightLight.enabled = !heldFlashlightLight.enabled;
        }
    }

    // MÉTODO REMOVIDO: A função será desativada.
    /*
    private void DropItem()
    {
        if (worldFlashlightPrefab == null)
        {
            Debug.LogError("ERRO CRÍTICO: O campo 'World Flashlight Prefab' não foi atribuído no Inspector do Player! Arraste o prefab para lá.");
            return;
        }

        if (!hasFlashlight) return;

        Vector3 currentPosition = heldFlashlightVisuals.position;
        Quaternion currentRotation = heldFlashlightVisuals.rotation;

        hasFlashlight = false;
        heldFlashlightObject.SetActive(false);
        activeFlashlightTransform = null;

        GameObject droppedFlashlightObject = Instantiate(worldFlashlightPrefab, currentPosition, currentRotation);

        Rigidbody rb = droppedFlashlightObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            if (controller != null)
            {
                rb.linearVelocity = controller.velocity;
            }
        } 
        

    FlashlightItem itemScript = droppedFlashlightObject.GetComponent<FlashlightItem>();
        if (itemScript != null)
        {
            itemScript.OnDrop();
        }
    }
    */

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) { return; }
        if (hit.moveDirection.y < -0.3f) { return; }
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
        Debug.Log("Porta final encontrada! Fechando o jogo...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}