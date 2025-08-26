using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Variáveis de Movimento
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    public float playerSpeed = 5.0f;
    public float jumpHeight = 1.0f;
    public float gravityValue = -9.81f;

    // Variáveis da Câmera e Input
    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    public Transform cameraTransform;
    public float mouseSensitivity = 100.0f;
    private float xRotation = 0f;

    private void Awake()
    {
        playerControls = new PlayerControls();
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
    
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Player.Disable();
    }

    void Update()
    {
        // Checa se o jogador está no chão
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Pega os inputs
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();
        lookInput = playerControls.Player.Look.ReadValue<Vector2>();

        // Rotação
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Rotação Horizontal
        transform.Rotate(Vector3.up * mouseX);

        //Rotação da Câmera (Olhar)
        // Olhar para cima e para baixo (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 50f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Movimentação do Jogador
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * playerSpeed * Time.deltaTime);

        // Gravidade
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
}