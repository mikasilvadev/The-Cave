using UnityEngine; // A "caixa de ferramentas" principal da Unity
using UnityEngine.InputSystem; // Precisamos disso para o NOVO Input System

/// <summary>
/// Este é o "Cérebro" e "Corpo" do Jogador.
/// Este script faz TUDO:
/// 1. Controla o movimento (andar, correr)
/// 2. Controla a câmera (olhar)
/// 3. Ouve o teclado/mouse (Inputs)
/// 4. Controla a interação (pegar itens)
/// 5. Controla a lanterna (ligar/desligar)
/// </summary>
public class PlayerController : MonoBehaviour
{
    // --- Variáveis de Movimento (Privadas) ---
    // 'private' = Só este script pode ver/mudar.

    // "Caixinha" para guardar o componente de FÍSICA (o "corpo").
    private CharacterController controller;

    // "Memória" da gravidade. Guarda a velocidade que estamos caindo (Y).
    private Vector3 playerVelocity;

    // Flag (bandeira) 'true'/'false' pra saber se estamos "no chão".
    private bool groundedPlayer;

    // "Memória" da velocidade atual (se estamos andando ou correndo).
    private float currentSpeed;

    // --- Configurações de Movimento (Públicas) ---
    // 'public' = Aparece no Inspector da Unity pra gente ajustar.

    [Header("Configurações de Movimento")] // Título no Inspector
    public float playerSpeed = 8.0f; // Velocidade andando
    public float sprintSpeed = 10.0f; // Velocidade correndo

    // (NOTA: Variável 'jumpHeight' REMOVIDA daqui,
    // porque não tinha nenhuma lógica de pulo no script. Era "código morto".)

    public float gravityValue = -9.81f; // Aceleração da gravidade
    public float pushPower = 2.0f; // Força para empurrar caixas (Rigidbodies)

    /// <summary>
    /// Esta é a VELOCIDADE ATUAL do player no mundo.
    /// 'public get' = Outros scripts (como o AIController) podem LER.
    /// 'private set' = SÓ este script (PlayerController) pode ESCREVER.
    /// (É o jeito mais seguro de compartilhar informação!)
    /// </summary>
    public Vector3 CurrentVelocity { get; private set; }

    // Flags (bandeiras) que o MONSTRO (MonsterSenses) lê
    // para saber se estamos fazendo barulho.
    public bool IsRunning { get; private set; }
    public bool IsMoving { get; private set; }

    // A "Trava" de Cutscene!
    // Se 'podeMover' for 'false', o Update "congela" o player.
    // (O CutsceneEvents e o AIController mexem aqui).
    public bool podeMover = true;

    // --- Variáveis de Input (Privadas) ---
    private PlayerControls playerControls; // O "mapa" de botões (do New Input System)
    private Vector2 moveInput; // "Memória" do W,A,S,D (um "direcional" 2D)
    private Vector2 lookInput; // "Memória" do movimento do Mouse (um "direcional" 2D)

    // --- Configurações da Câmera ---
    public Transform cameraTransform; // "Caixinha" pública pra arrastar a Câmera Principal
    public float mouseSensitivity = 5.0f; // Sensibilidade do mouse

    // "Memória" da rotação CIMA/BAIXO (eixo X).
    private float xRotation = 0f;

    // --- Configurações de Interação (Pegar Itens) ---
    [Header("Flashlight & Inventory")] // Título no Inspector
    public Transform pickupZoneCenter; // Objeto "filho" que marca ONDE a detecção começa
    public float pickupRange = 1f; // O quão FUNDA (eixo Z) é a área de detecção
    public Vector2 pickupAreaSize = new Vector2(1f, 2f); // O quão LARGA (X) e ALTA (Y) é a área
    public LayerMask pickupLayer; // "Filtro" (Layer) que diz o que é um "item pegável"

    // "Caixinha" pública pra arrastar a LANTERNA DA MÃO (o GameObject
    // que fica "preso" na câmera/mão do player).
    public GameObject heldFlashlightObject;

    // (NOTA: Variáveis 'heldFlashlightVisuals' e 'worldFlashlightPrefab'
    // REMOVIDAS daqui. Elas eram usadas pela função 'DropItem',
    // que foi desativada (código morto).)

    // --- "Memória" da Lanterna (Privadas) ---
    private bool hasFlashlight = false; // Flag 'true'/'false' (Já pegamos a lanterna?)
    private Light heldFlashlightLight; // "Caixinha" pra guardar a LUZ da lanterna da mão

    // "Memória" do Highlight. Guarda o "último item" que a gente olhou.
    private FlashlightItem lastHighlightedItem = null;

    // --- Configurações da "Mira" da Lanterna ---
    [Header("Dynamic Flashlight Settings")]
    private Transform activeFlashlightTransform; // "Memória" de qual lanterna tá ativa
    public float maxFlashlightAngle = 25f; // O ângulo MÁXIMO que a lanterna mexe ANTES do corpo

    // "Memória" da rotação horizontal (Y) SÓ DA LANTERNA.
    private float flashlightYaw = 0f;

    /// <summary>
    /// Método 'Awake'. Roda ANTES de todo mundo (antes do Start).
    /// Perfeito para "pegar" componentes e "ligar" os inputs.
    /// </summary>
    private void Awake()
    {
        // 1. PEGA OS COMPONENTES
        // "Enche a caixinha 'controller' com o CharacterController
        // que está neste mesmo GameObject."
        controller = GetComponent<CharacterController>();

        // Pega a Câmera Principal da cena (geralmente funciona).
        cameraTransform = Camera.main.transform;

        // 2. CONFIGURA O CURSOR
        // Trava o cursor no centro da tela.
        Cursor.lockState = CursorLockMode.Locked;
        // Esconde o cursor (setinha) do mouse.
        Cursor.visible = false;

        // 3. PREPARA A LANTERNA DA MÃO
        if (heldFlashlightObject != null) // Se a gente arrastou ela no Inspector...
        {
            // Pega o componente 'Light' (a luz) que tá nela (ou num filho dela).
            heldFlashlightLight = heldFlashlightObject.GetComponentInChildren<Light>();

            // DESLIGA a lanterna da mão. Ela só vai ligar
            // quando a gente 'PickupFlashlight'.
            heldFlashlightObject.SetActive(false);
        }

        // 4. LIGA O "OUVIDO" (INPUT SYSTEM)

        // Cria um "novo mapa de controles" na memória.
        playerControls = new PlayerControls();

        // "Habilita" o mapa (começa a ouvir o teclado/mouse).
        playerControls.Player.Enable();

        // "Se o botão 'Interact' (ex: "E") for 'performed' (apertado)..."
        // "...chame (=>) a nossa função 'Interact()'."
        // (Isso é um "Callback" ou "Evento". É super otimizado!)
        playerControls.Player.Interact.performed += ctx => Interact();

        // "Se o botão 'ToggleFlashlight' (ex: "F") for 'performed'..."
        // "...chame (=>) a nossa função 'ToggleFlashlight()'."
        playerControls.Player.ToggleFlashlight.performed += ctx => ToggleFlashlight();

        // (NOTA: A linha do 'Drop' foi REMOVIDA daqui,
        // porque a função 'DropItem' foi desativada).
    }

    /// <summary>
    /// Função Mágica da Unity. Chamada quando o script é DESATIVADO.
    /// </summary>
    private void OnDisable()
    {
        // "Desliga" o mapa de controles (para de ouvir o teclado).
        // Isso é SUPER importante pra evitar bugs se o player "morrer"
        // ou a cena for descarregada.
        playerControls.Player.Disable();
    }

    /// <summary>
    /// Método 'Update'. O CORAÇÃO do Player. Roda UMA VEZ A CADA FRAME.
    /// </summary>
    void Update()
    {
        // A "TRAVA" DE CUTSCENE
        // "Se 'podeMover' for 'true' (não estamos numa cutscene)..."
        if (podeMover)
        {
            // "...então, podemos nos mover, olhar e interagir."
            HandleMovement(); // Chama a função de movimento
            HandleLook(); // Chama a função de olhar
            HandleInteractionHighlight(); // Chama a função de "brilhar" itens
        }
    }

    /// <summary>
    /// Função "Ajudante" que cuida SÓ do MOVIMENTO (andar, correr, gravidade).
    /// </summary>
    void HandleMovement()
    {
        // 1. CHECA O CHÃO E A GRAVIDADE
        // Pergunta pro "corpo" (controller) se ele tá 'isGrounded' (no chão).
        groundedPlayer = controller.isGrounded;

        // "Se estamos no chão E nossa velocidade de queda ('playerVelocity.y')
        // for negativa (ou seja, estávamos caindo)..."
        if (groundedPlayer && playerVelocity.y < 0)
        {
            // "...ZERA a velocidade de queda!"
            // (Isso é ESSENCIAL pra gravidade não ficar acumulando
            // e nos "esmagando" contra o chão).
            playerVelocity.y = 0f;
        }

        // 2. LÊ OS INPUTS (W,A,S,D)
        // "Lê" o "direcional" (Vector2) do mapa de controles "Move".
        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        // 3. ATUALIZA AS FLAGS (para o Monstro saber)
        // 'moveInput.magnitude' = o "tamanho" do vetor (de 0 a 1).
        // Se for maior que 0.1 (o analógico mexeu um pouquinho),
        // então 'IsMoving' = 'true'.
        IsMoving = moveInput.magnitude > 0.1f;

        // "Lê" se o botão "Sprint" (ex: Shift) está 'IsPressed' (sendo segurado).
        bool isSprintingInput = playerControls.Player.Sprint.IsPressed();

        // 4. DECIDE A VELOCIDADE
        // 'isSprintingInput ? sprintSpeed : playerSpeed'
        // É um "if" rápido (ternário):
        // "SE 'isSprintingInput' for 'true', 'currentSpeed' = 'sprintSpeed'.
        // SENÃO (':'), 'currentSpeed' = 'playerSpeed'."
        currentSpeed = isSprintingInput ? sprintSpeed : playerSpeed;

        // 'IsRunning' = 'true' SÓ SE as duas coisas forem verdadeiras
        // (está segurando Shift E está se movendo).
        IsRunning = isSprintingInput && IsMoving;

        // 5. CALCULA A DIREÇÃO DO MOVIMENTO
        // 'transform.right * moveInput.x' = Direção Lado (A/D)
        // 'transform.forward * moveInput.y' = Direção Frente/Trás (W/S)
        // (Soma os dois pra ter o movimento diagonal certo).
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // 6. APLICA O MOVIMENTO HORIZONTAL
        // "Corpo (controller), Mova-se! Na direção 'move',
        // na velocidade 'currentSpeed', ajustado pelo tempo ('Time.deltaTime')."
        // (Time.deltaTime faz o movimento ser suave e igual
        // em PCs rápidos ou lentos).
        controller.Move(move * currentSpeed * Time.deltaTime);

        // 7. APLICA A GRAVIDADE
        // "Aumenta" a velocidade de queda (Y) baseado na gravidade.
        playerVelocity.y += gravityValue * Time.deltaTime;

        // 8. APLICA O MOVIMENTO VERTICAL
        // "Corpo (controller), Mova-se! Na direção Y (queda),
        // na velocidade Y ('playerVelocity'), ajustado pelo tempo."
        controller.Move(playerVelocity * Time.deltaTime);

        // 9. SALVA A VELOCIDADE (para o Monstro ler)
        // 'controller.velocity' é a velocidade REAL que o corpo se moveu.
        // A gente salva na nossa Propriedade pública.
        CurrentVelocity = controller.velocity;
    }

    /// <summary>
    /// Função "Ajudante" que cuida SÓ da CÂMERA (olhar com o mouse).
    /// </summary>
    void HandleLook()
    {
        // 1. LÊ OS INPUTS (Mouse)
        // "Lê" o "direcional" (Vector2) do mapa "Look" (o mouse).
        lookInput = playerControls.Player.Look.ReadValue<Vector2>();

        // Ajusta o movimento do mouse pela sensibilidade e pelo tempo.
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // 2. LÓGICA DE OLHAR CIMA/BAIXO (Vertical - Eixo X)

        // 'xRotation -= mouseY' (é -= porque o mouse Y é invertido).
        xRotation -= mouseY;

        // 'Mathf.Clamp' = "Trava" a rotação X.
        // Não deixa o player "quebrar o pescoço" (olhar mais que -80º pra cima
        // ou 50º pra baixo).
        xRotation = Mathf.Clamp(xRotation, -80f, 50f);

        // 'cameraTransform.localRotation'
        // Aplica a rotação SÓ na CÂMERA (o "pescoço").
        // (O corpo não olha pra cima/baixo).
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 3. LÓGICA DE OLHAR LADOS (Horizontal - Eixo Y)
        // (Esta é a sua lógica "diferente" de mira da lanterna).

        // "Se a gente JÁ PEGOU a lanterna ('activeFlashlightTransform' não é nulo)..."
        if (activeFlashlightTransform != null)
        {
            // "SE o 'yaw' (giro) da lanterna + o novo 'mouseX'
            // for MAIOR que o 'maxFlashlightAngle' (ex: 25º)..."
            if (Mathf.Abs(flashlightYaw + mouseX) > maxFlashlightAngle)
            {
                // ...então a lanterna "travou" no limite.
                // Gira o CORPO INTEIRO do player.
                transform.Rotate(Vector3.up * mouseX);
            }
            else // "SE a lanterna AINDA ESTIVER dentro do limite..."
            {
                // ...então NÃO gira o corpo.
                // Acumula o 'mouseX' na "memória" de giro da lanterna.
                flashlightYaw += mouseX;
            }

            // Aplica o giro SÓ NO MODELO da lanterna.
            activeFlashlightTransform.localRotation = Quaternion.Euler(0f, flashlightYaw, 0f);
        }
        else // "SE a gente AINDA NÃO PEGOU a lanterna..."
        {
            // ...só gira o corpo do player normalmente.
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    /// <summary>
    /// Função "Ajudante" que "sente" os itens próximos e
    /// manda eles brilharem (Highlight).
    /// </summary>
    private void HandleInteractionHighlight()
    {
        // 1. CHECAGEM INICIAL
        // "Se a gente JÁ tem a lanterna ('hasFlashlight' == true)..."
        if (hasFlashlight)
        {
            // "...então não precisamos mais procurar itens."

            // (Limpeza): Se a gente ainda tinha um item "brilhando"
            // na memória, manda ele parar de brilhar.
            if (lastHighlightedItem != null)
            {
                lastHighlightedItem.RemoveHighlight();
                lastHighlightedItem = null;
            }
            return; // Para a função aqui.
        }

        // 2. DISPARA O "SENSOR" (OverlapBox)
        // "Cria" uma caixa invisível na nossa frente
        // (baseado no 'pickupZoneCenter' e nos tamanhos 'pickupAreaSize')
        // e nos devolve uma LISTA ('colliders') de TUDO
        // que "encostou" na caixa (e que esteja na 'pickupLayer').
        Vector3 boxCenter = pickupZoneCenter.position;
        Vector3 halfExtents = new Vector3(pickupAreaSize.x / 2, pickupAreaSize.y / 2, pickupRange / 2);
        Collider[] colliders = Physics.OverlapBox(boxCenter, halfExtents, pickupZoneCenter.rotation, pickupLayer);

        // 3. ACHA O ITEM MAIS PRÓXIMO (A Lógica Inteligente)
        FlashlightItem closestItem = null; // "Memória" do mais próximo (começa vazia)
        float closestDistance = float.MaxValue; // "Memória" da distância (começa infinito)

        // 'foreach' = Loop que "visita" CADA 'Collider col'
        // que o OverlapBox achou.
        foreach (var col in colliders)
        {
            // "Se o item tiver a Tag 'Interactable'..."
            if (col.CompareTag("Interactable"))
            {
                // Pega o script 'FlashlightItem' desse item.
                FlashlightItem item = col.GetComponent<FlashlightItem>();

                // "Se ele tiver o script E ele 'canBePickedUp' (pode ser pego)..."
                if (item != null && item.canBePickedUp)
                {
                    // Mede a distância.
                    float distance = Vector3.Distance(transform.position, col.transform.position);

                    // "Se essa distância for MENOR que a 'closestDistance'
                    // (que era infinito, ou era a do item anterior)..."
                    if (distance < closestDistance)
                    {
                        // ...achamos um novo "mais próximo"!
                        closestDistance = distance; // Salva a nova distância
                        closestItem = item; // Salva o novo item
                    }
                }
            }
        }

        // 4. ATUALIZA O "BRILHO"

        // "Se o item mais próximo que achamos (closestItem)
        // for DIFERENTE do último item que a gente tava olhando (lastHighlightedItem)..."
        if (closestItem != lastHighlightedItem)
        {
            // "...significa que o player mudou a mira."

            // 1. Apaga o brilho antigo:
            // "Se o 'lastHighlightedItem' não for nulo (tinha um item brilhando)..."
            if (lastHighlightedItem != null)
            {
                // "...manda ele parar de brilhar."
                lastHighlightedItem.RemoveHighlight();
            }

            // 2. Acende o brilho novo:
            // "Se o 'closestItem' não for nulo (achamos um item novo)..."
            if (closestItem != null)
            {
                // "...manda ele brilhar."
                closestItem.Highlight();
            }

            // 3. Atualiza a memória:
            // "O 'último item' agora é o 'item atual'."
            lastHighlightedItem = closestItem;
        }
    }

    /// <summary>
    /// Função "Ajudante" que é chamada pelo "Ouvido" do Input System
    /// quando o player aperta "E" (Interact).
    /// </summary>
    private void Interact()
    {
        // --- CHECAGEM 1: "PORTA FINAL" (Win Condition) ---

        // Dispara um "raio laser" (Raycast) curto (3f) da câmera
        // pra frente, pra ver se o player tá olhando pra porta.
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3f))
        {
            // Se o "laser" bateu E o que ele bateu tem a Tag "PortaFinal"...
            if (hit.collider.CompareTag("PortaFinal"))
            {
                // ...chama a função de fechar o jogo (lá embaixo).
                FecharJogo();
                return; // Para a função aqui.
            }
        }

        // --- CHECAGEM 2: "PEGAR ITEM" ---

        // Se a "PortaFinal" não foi ativada, vamos checar os itens.
        // "Se a nossa 'memória' 'lastHighlightedItem' NÃO for nula
        // (ou seja, estamos olhando para um item brilhando)..."
        if (lastHighlightedItem != null)
        {
            // ...chama a função de "Pegar" o item.
            PickupFlashlight(lastHighlightedItem);

            // E "limpa" a memória (não estamos mais olhando pra ele).
            lastHighlightedItem = null;
        }
    }

    /// <summary>
    /// Função "Ajudante" (privada) que faz a lógica de PEGAR a lanterna.
    /// </summary>
    private void PickupFlashlight(FlashlightItem item)
    {
        // 1. "Avisa" o script que agora 'hasFlashlight' = 'true'.
        hasFlashlight = true;

        // 2. LIGA o GameObject da "Lanterna da Mão".
        heldFlashlightObject.SetActive(true);

        // 3. "Guarda" o transform da lanterna da mão
        // (para a lógica do 'HandleLook' usar).
        activeFlashlightTransform = heldFlashlightObject.transform;

        // 4. "Avisa" o ITEM DO CHÃO (o script 'FlashlightItem')
        // para ele "se destruir" ('OnPickup()').
        item.OnPickup();
    }

    /// <summary>
    /// Função "Ajudante" chamada pelo "Ouvido" do Input System
    /// quando o player aperta "F" (ToggleFlashlight).
    /// </summary>
    private void ToggleFlashlight()
    {
        // 1. Checagem de Segurança:
        // "Se a gente JÁ 'hasFlashlight' E
        // a 'luz' ('heldFlashlightLight') não for nula..."
        if (hasFlashlight && heldFlashlightLight != null)
        {
            // 2. A "Mágica" do Toggle (Ligar/Desligar):
            // 'heldFlashlightLight.enabled = !heldFlashlightLight.enabled'
            // "O novo estado 'enabled' (ligado/desligado)
            // é igual ao 'enabled' antigo, SÓ QUE AO CONTRÁRIO (!)."
            // (Se estava 'true', vira 'false'. Se estava 'false', vira 'true').
            heldFlashlightLight.enabled = !heldFlashlightLight.enabled;
        }
    }

    // (NOTA: O método 'DropItem()' inteiro foi REMOVIDO daqui.
    // Ele estava todo comentado e era "código morto".)

    /// <summary>
    /// Função MÁGICA da Unity (Física).
    /// É chamada AUTOMATICAMENTE sempre que o nosso 'CharacterController'
    /// "esbarra" em outro Collider (que NÃO é Trigger).
    /// </summary>
    /// <param name="hit">A informação da colisão (o que batemos, onde, etc).</param>
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Esta função é só pra "empurrar" caixas (Rigidbodies).

        // Pega o "corpo de física" (Rigidbody) da coisa que batemos.
        Rigidbody body = hit.collider.attachedRigidbody;

        // 1. Checagem de Segurança:
        // "Se o 'body' for nulo (não tem física) OU
        // for 'isKinematic' (é física, mas não se move com forças)..."
        if (body == null || body.isKinematic) { return; } // ...para a função.

        // 2. Checagem de Ângulo:
        // "Se 'hit.moveDirection.y' for menor que -0.3..."
        // (Isso significa que estamos "pousando" em cima da caixa,
        // não "empurrando" ela de lado).
        if (hit.moveDirection.y < -0.3f) { return; } // ...para a função.

        // 3. APLICA A FORÇA
        // Pega a direção do "empurrão" (só X e Z, ignora o Y).
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // "Corpo ('body'), sua velocidade linear ('linearVelocity')
        // agora é a direção do 'empurrão' * a nossa 'pushPower'."
        body.linearVelocity = pushDir * pushPower;
    }

    /// <summary>
    /// Função MÁGICA da Unity (Desenho).
    /// Desenha "Gizmos" (ajudas visuais) na tela do EDITOR
    /// SÓ QUANDO o objeto está selecionado.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // (Isso desenha a "caixa verde" de detecção de itens).

        // Se a gente esqueceu de arrastar o 'pickupZoneCenter'...
        if (pickupZoneCenter == null) return; // ...para a função (evita erro).

        Gizmos.color = new Color(0, 1, 0, 0.5f); // Verde transparente

        // Alinha o "lápis" do Gizmo com a posição/rotação
        // da nossa 'pickupZoneCenter'.
        Gizmos.matrix = Matrix4x4.TRS(pickupZoneCenter.position, pickupZoneCenter.rotation, Vector3.one);

        // Define o tamanho da caixa.
        Vector3 boxSize = new Vector3(pickupAreaSize.x, pickupAreaSize.y, pickupRange);

        // Desenha a "caixa de arame" (WireCube).
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }

    /// <summary>
    /// Função "Ajudante" (privada) chamada pelo 'Interact'
    /// quando olhamos para a "PortaFinal".
    /// </summary>
    private void FecharJogo()
    {
        Debug.Log("Porta final encontrada! Fechando o jogo...");

        // 'Application.Quit()' = O comando REAL para fechar o jogo
        // (só funciona no jogo "buildado" / .exe).
        Application.Quit();

        // --- "Hack" para o Editor ---
        // '#if UNITY_EDITOR' = Este bloco de código SÓ EXISTE
        // se a gente estiver rodando o jogo dentro do Editor da Unity.
        // (Ele é "apagado" na build .exe).
#if UNITY_EDITOR
        // Manda o "Play Mode" do Editor parar.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}