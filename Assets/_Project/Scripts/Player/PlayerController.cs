using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Variáveis de Movimento
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;

    public float playerSpeed = 8.0f; // Velocidade de caminhada
    public float sprintSpeed = 10.0f; // Velocidade de corrida
    private float currentSpeed; // Guarda a velocidade atual
    public float jumpHeight = 1.0f; 
    public float gravityValue = -9.81f;

    // Variáveis da Câmera e Input
    private PlayerControls playerControls;
    private Vector2 moveInput;
    private Vector2 lookInput;
    public Transform cameraTransform;
    public float mouseSensitivity = 5.0f;
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
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // Rotação Horizontal
        transform.Rotate(Vector3.up * mouseX * Time.deltaTime);

        //Rotação da Câmera (Olhar)
        // Olhar para cima e para baixo (pitch)
        xRotation -= mouseY * Time.deltaTime;
        xRotation = Mathf.Clamp(xRotation, -80f, 50f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Lógica de corrida
        bool isSprinting = playerControls.Player.Sprint.IsPressed(); // Verifica se o botão de Sprint está pressionado
        currentSpeed = isSprinting ? sprintSpeed : playerSpeed;

        // Movimentação do Jogador
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Gravidade
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
}