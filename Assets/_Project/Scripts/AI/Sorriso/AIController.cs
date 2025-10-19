using UnityEngine;
using UnityEngine.AI; // Precisamos disso para o NavMeshAgent (o "GPS" do monstro)
using UnityEngine.SceneManagement; // Precisamos disso para recarregar a cena (GameOver)
using System.Collections; // Precisamos disso para as Coroutines (funções com pausa)

// [RequireComponent(...)] é um "cadeado" de segurança.
// Ele FORÇA o GameObject (o "Sorriso") que tem esse script
// a TAMBÉM ter os outros componentes listados.
// Se você tentar adicionar o AIController sem um Animator, a Unity não deixa.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MonsterSenses))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(MonsterAudio))]
public class AIController : MonoBehaviour
{
    // --- O "PAINEL DE CONTROLE" DO MONSTRO ---

    [Header("Componentes")] // Título para organizar o Inspector

    // 'public NavMeshAgent Agent' = Estamos criando uma "caixinha" pública chamada 'Agent'.
    // '{ get; private set; }' = Isso é uma "Propriedade" (uma variável chique).
    // 'public get' = QUALQUER script pode LER o que tá na caixinha (ex: ChaseState pode ver controller.Agent).
    // 'private set' = SÓ ESSE SCRIPT (AIController) pode COLOCAR coisas na caixinha (lá no Awake).
    // Isso é super seguro e organizado!
    public NavMeshAgent Agent { get; private set; }
    public MonsterSenses Senses { get; private set; }
    public Transform Player { get; private set; }
    public PlayerController PlayerController { get; private set; }
    public Animator Animator { get; private set; }
    public MonsterAudio MonsterAudio { get; private set; }

    [Header("Componentes de UI")]
    public ScreenFader screenFader; // Referência ao script que faz a tela ficar preta

    // --- CONFIGURAÇÕES DE DIFICULDADE (TUNING) ---
    // Todas essas variáveis 'public' aparecem no Inspector da Unity.
    // Podemos mudar a dificuldade do jogo aqui, sem mexer no código!

    [Header("Configurações de Movimento")]
    public float wanderSpeed = 2f; // Velocidade andando
    public float minWanderIdleTime = 3.0f; // Tempo mínimo parado
    public float maxWanderIdleTime = 7.0f; // Tempo máximo parado
    public float chaseSpeed = 7f; // Velocidade correndo
    public float searchSpeed = 3.5f; // Velocidade procurando
    public float chaseRotationSpeed = 10f; // Quão rápido ele vira pra te encarar
    public float investigateRotationSpeed = 3f; // Quão rápido ele vira pra olhar um som

    [Header("Configurações de Parada")]
    // (Variável 'darkStopDistance' removida, não era usada)
    public float soundStopDistance = 1.0f; // A que distância ele para de um som

    [Header("Configurações da Investida (Lunge)")]
    public float lungeDistance = 3f; // A que distância ele começa o "bote"
    public float lungeSpeed = 12f; // A velocidade do "bote"
    // (Variável 'lungeAcceleration' removida, não era usada)

    [Header("Configurações de Animação")]
    [Tooltip("Multiplicador geral da velocidade da animação para ajustar o visual da corrida.")]
    public float animationSpeedMultiplier = 1.2f;

    [Header("Configurações de Fim de Jogo")]
    public float restartFadeTime = 4.0f; // Tempo de espera na tela preta antes de reiniciar

    [Header("Configurações de Perseguição")]
    // (Variável 'predictionDistance' removida, não era usada)
    [Tooltip("Quantos segundos no futuro o monstro deve 'prever' a posição do jogador.")]
    public float predictionTime = 1.5f;

    // --- VARIÁVEIS INTERNAS (O "CÉREBRO") ---

    [Header("Estado Atual")]
    [SerializeField] // Faz uma variável 'private' aparecer no Inspector (bom pra debug)
    private string currentStateName; // (Só pra debug) Mostra o nome do estado atual

    // 'currentState' é a "memória" principal. Guarda qual estado
    // (Wander, Chase, etc.) está no comando AGORA.
    private BaseState currentState;

    // 'isGameOver' é uma flag (bandeira) 'true'/'false'
    // para travar o jogo quando o player for pego.
    private bool isGameOver = false;

    // "Memórias" públicas que os estados de 'Busca' usam.
    // O 'ChaseState' ESCREVE aqui, e o 'SearchState' LÊ daqui.
    public Vector3 LastKnownPlayerVelocity { get; set; }
    public Vector3 LastKnownPlayerPosition { get; set; }

    /// <summary>
    /// Método 'Awake'. Roda ANTES de todo mundo (antes do Start).
    /// É o lugar perfeito para "pegar" (GetComponent) os componentes
    /// e "encher as caixinhas" (as Propriedades).
    /// </summary>
    void Awake()
    {
        // 'Agent = GetComponent<NavMeshAgent>()'
        // "Enche a nossa caixinha 'Agent' com o componente NavMeshAgent
        // que está neste mesmo GameObject."
        Agent = GetComponent<NavMeshAgent>();
        Senses = GetComponent<MonsterSenses>();
        Animator = GetComponent<Animator>();
        MonsterAudio = GetComponent<MonsterAudio>();

        // O Player é o único que não está no mesmo GameObject.
        // Então, procuramos ele pelo "Tag" (etiqueta) "Player".
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player = playerObject.transform; // Pegamos a posição/rotação dele
        PlayerController = playerObject.GetComponent<PlayerController>(); // Pegamos o script dele
    }

    /// <summary>
    /// Método 'Start'. Roda UMA VEZ, no primeiro frame do jogo (depois do Awake).
    /// É o lugar perfeito para dar a "primeira ordem" para a IA.
    /// </summary>
    void Start()
    {
        // A primeira ordem é: "Comece a Vagar (Wander)".
        ChangeState(new WanderState());
    }

    /// <summary>
    /// Método 'Update'. O CORAÇÃO da IA. Roda UMA VEZ A CADA FRAME.
    /// É a "lista de prioridades" do Gerente. Ele checa as coisas
    /// mais importantes primeiro.
    /// </summary>
    void Update()
    {
        // --- PRIORIDADE 1: O JOGO ACABOU? ---
        // Se 'isGameOver' for 'true', não faz mais NADA.
        if (isGameOver)
        {
            return; // 'return' para o Update NA HORA.
        }

        // --- PRIORIDADE 2: O PLAYER APAGOU A LUZ? ---
        // Esta é a mecânica principal do jogo.
        // Checa se a referência da lanterna existe ANTES de usar.
        if (Senses.playerFlashlight != null && !Senses.playerFlashlight.enabled)
        {
            // Se a luz estiver desligada, o monstro "congela"...
            // ...MAS SÓ SE ele não estiver no meio de uma perseguição ou investigação ativa.
            // Se ele estiver em Chase, o *próprio ChaseState* vai mandar ele Recuar (Retreat).
            // Se estiver Investigando um som, ele continua até chegar lá.

            // "Se o Agent (GPS) pode andar E ele não está parado..."
            // "E (&&) o estado atual é Wander OU Search..." (ou seja, estados "calmos")
            if (Agent.enabled && !Agent.isStopped && (currentState is WanderState || currentState is SearchState))
            {
                // ...então mandamos ele PARAR.
                Agent.isStopped = true;
                Animator.SetFloat("Speed", 0); // Para a animação de andar
                Animator.speed = 1f; // Volta a velocidade da animação ao normal
            }

            // !! BUG CORRIGIDO !!
            // ANTES TINHA UM "return;" AQUI.
            // Eu tirei. Se a gente desse 'return', a linha 'currentState.Execute(this)'
            // lá embaixo NUNCA rodaria. Isso impedia o ChaseState de
            // chamar o RetreatState quando a luz apagava.
            // Agora, mesmo com a luz apagada, os estados podem "pensar".
        }
        else // Se a luz estiver LIGADA (ou não existe)...
        {
            // Se o Agent (GPS) pode andar E ele está parado...
            // (e ele não deveria estar parado por causa da luz apagada)
            if (Agent.enabled && Agent.isStopped)
            {
                // Checa se o estado atual NÃO é um que *deveria* estar parado (como Idle do Wander)
                // Se for Wander, só destrava se NÃO estiver Idle.
                bool shouldUnfreeze = true;
                if (currentState is WanderState wanderState)
                {
                    // Precisaríamos expor a 'currentPhase' ou ter um método IsIdle() no WanderState
                    // Para simplificar, vamos destravar sempre, e o WanderState manda parar de novo se for Idle.
                }

                if (shouldUnfreeze)
                {
                    Agent.isStopped = false;
                }
            }
        }

        // --- PRIORIDADE 3: ESTOU SENDO ILUMINADO DIRETAMENTE? ---
        // Se o player apontar a luz direto NA CARA do monstro...
        if (Senses.IsPlayerShiningLightOnMe(out Vector3 playerPosShining))
        {
            // *** NOVA CONDIÇÃO ***
            // Só muda pro ChaseState SE ele JÁ NÃO ESTIVER em ChaseState.
            // Isso evita o loop infinito de reiniciar o mesmo estado.
            if (!(currentState is ChaseState))
            {
                Debug.LogWarning("AIController: FUI ILUMINADO DIRETAMENTE! MUDANDO PARA CHASESTATE!", this.gameObject);
                ChangeState(new ChaseState());
                return; // Para o Update aqui.
            }
            // Se ele JÁ está em ChaseState, a gente IGNORA essa checagem aqui
            // e deixa o Execute() do ChaseState rodar normalmente.
            // O próprio ChaseState vai decidir se perdeu o player ou não.
        }

        // --- PRIORIDADE 4: OUVI UM BARULHO? ---
        // "Se o estado atual NÃO É (is not) Investigate E NÃO É Chase..."
        // (Não queremos interromper uma investigação ativa ou perseguição).
        if (!(currentState is InvestigateState) && !(currentState is ChaseState))
        {
            // "O Senses ouviu um som?"
            if (Senses.CheckForSound(out Vector3 soundPos))
            {
                // "O som foi longe o suficiente (ex: 1.5m)?"
                // (Evita ele investigar o próprio pé).
                if (Vector3.Distance(transform.position, soundPos) > 1.5f)
                {
                    Debug.Log("AIController: OUVIU SOM! Mudando para InvestigateState.", this.gameObject);

                    // Muda o estado, passando o local do som e o "motivo".
                    ChangeState(new InvestigateState(soundPos, "som"));
                    return; // Para o Update aqui.
                }
            }
        }

        // --- PRIORIDADE 5 E 5.5: DETECÇÃO DE LUZ (FOCO E FACHO) ---
        // "Se o estado atual é Wander OU Search..."
        // (Só procuramos a luz se estivermos "calmos").
        if (currentState is WanderState || currentState is SearchState)
        {
            // --- PRIORIDADE 5: VIU O *FOCO* DE LUZ (Ponto Final)? ---
            if (Senses.CanSeeFlashlightBeam(out Vector3 lightPos)) // <-- Pergunta antiga
            {
                Debug.Log("AIController: VIU O FOCO DE LUZ! Mudando para InvestigateState.", this.gameObject);
                ChangeState(new InvestigateState(lightPos, "foco de luz"));
                return; // Para o Update aqui.
            }
            // --- PRIORIDADE 5.5: PERCEBEU LUZ *CRUZANDO* A VISÃO? ---
            // 'else if' -> Só checa isso SE não viu o foco direto.
            else if (Senses.CanDetectCrossingBeam(out Vector3 beamCrossPos)) // <-- Pergunta NOVA!
            {
                Debug.Log("AIController: PERCEBEU LUZ CRUZANDO A VISÃO! Mudando para InvestigateState.", this.gameObject);
                // O "motivo" pode ser diferente para debug, ou o mesmo "foco de luz".
                ChangeState(new InvestigateState(beamCrossPos, "luz cruzando"));
                return; // Para o Update aqui.
            }
        }

        // --- PRIORIDADE 6: DEIXA O ESTADO ATUAL TRABALHAR ---
        // Se nenhuma das prioridades acima (1 a 5.5) aconteceu...
        // ...então a gente só fala pro estado atual (seja ele Wander,
        // Chase, Search, etc) continuar fazendo o que ele já estava fazendo.
        if (currentState != null) // Checa se 'currentState' não é nulo (segurança)
        {
            // 'currentState.Execute(this)'
            // "Estado atual, rode a sua lógica de 'Execute' (Update)".
            // O '(this)' passa o "Gerente" (o AIController) inteiro
            // para o estado, para ele poder usar o 'controller.Agent', etc.
            currentState.Execute(this);
        }

        // Finalmente, depois de toda a lógica, atualiza a animação.
        UpdateAnimator();
    }

    /// <summary>
    /// Função separada só para cuidar das animações.
    /// (Chamada todo frame, no final do Update).
    /// </summary>
    private void UpdateAnimator()
    {
        // Se o Agent (GPS) estiver ligado E o Animator (Marionete) existir...
        if (Agent.enabled && Animator != null)
        {
            // 1. Pega a velocidade ATUAL do "GPS".
            // 'Agent.velocity.magnitude' = velocidade real no mundo (ex: 3.48 m/s).
            float worldSpeed = Agent.velocity.magnitude;

            // 2. Pega a velocidade MÁXIMA que ele *poderia* ter.
            // (Se estiver em Chase, 'maxSpeed' = 7. Se em Wander, 'maxSpeed' = 2).
            float maxSpeed = Agent.speed;

            // 3. NORMALIZA a velocidade.
            // O Animator não entende "7 m/s". Ele quer um número de 0 a 1.
            // (Velocidade Atual / Velocidade Máxima) = % da velocidade (ex: 0.5 = 50%).
            // 'maxSpeed > 0 ? ... : 0' é um "if" rápido (ternário) pra evitar divisão por zero.
            float normalizedSpeed = maxSpeed > 0 ? worldSpeed / maxSpeed : 0;

            // 4. Manda pro Animator.
            // "Avisa o Animator que a variável 'Speed' agora é (ex:) 0.8".
            Animator.SetFloat("Speed", normalizedSpeed);

            // 5. Ajusta a velocidade da ANIMAÇÃO.
            // Se ele estiver correndo (worldSpeed > 0.1f),
            // toca a animação um pouco mais rápido (ex: 1.2x),
            // pra dar a sensação de "corrida" (definido no 'animationSpeedMultiplier').
            // Se estiver parado, a velocidade da animação é 1 (normal).
            Animator.speed = (worldSpeed > 0.1f) ? animationSpeedMultiplier : 1f;
        }
    }

    /// <summary>
    /// Esta é a função "Mágica" que troca de um estado para outro.
    /// É 'public' pra qualquer estado poder chamar (ex: 'controller.ChangeState(...)').
    /// </summary>
    /// <param name="newState">O NOVO estado que vai assumir (ex: new ChaseState()).</param>
    public void ChangeState(BaseState newState)
    {
        // Manda um Log super detalhado pro Console.
        // O '?' é um "checador nulo". "Se 'currentState' não for nulo,
        // pega o nome (GetType().Name). Se for nulo, escreve 'NULL'."
        Debug.Log($"<color=orange>STATE CHANGE:</color> De <color=yellow>{currentState?.GetType().Name ?? "NULL"}</color> para <color=green>{newState.GetType().Name}</color>", this.gameObject);

        // 1. "Limpa a sujeira" do estado antigo.
        if (currentState != null) // Se já existia um estado...
        {
            currentState.Exit(this); // ...chama o 'Exit()' dele.
        }

        // 2. Troca o "chefe".
        // A "memória" principal agora guarda o NOVO estado.
        currentState = newState;

        // (Debug) Atualiza o nome no Inspector.
        currentStateName = newState.GetType().Name;

        // 3. "Prepara" o novo estado.
        // Chama o 'Enter()' do NOVO estado.
        currentState.Enter(this);
    }

    /// <summary>
    /// Função de Fim de Jogo. É chamada pelo 'PlayerCaptureZone'
    /// quando o monstro encosta no player.
    /// </summary>
    public void GameOverAndRestart()
    {
        // 1. Trava o 'Update'
        isGameOver = true;
        Debug.Log("Iniciando GameOverAndRestart...");

        // 2. Fade para o preto (instantâneo)
        if (screenFader != null)
        {
            screenFader.FadeToBlackInstant();
        }

        // 3. DESLIGA TUDO
        MonsterAudio.StopAllSounds(); // Para todos os sons
        Animator.enabled = false; // Desliga a "marionete"

        if (Agent.enabled) // Se o "GPS" estiver ligado...
        {
            Agent.isStopped = true; // Manda parar
            Agent.enabled = false; // Desliga o "GPS"
        }

        if (PlayerController != null) // Se o script do player existir...
        {
            PlayerController.podeMover = false; // Trava o movimento do player
        }

        // 4. Inicia a Coroutine (função com pausa) para recarregar a cena.
        StartCoroutine(FadeAndReloadScene(restartFadeTime));
    }

    /// <summary>
    /// Uma 'IEnumerator' é uma função que pode ser "pausada".
    /// Usada para o 'GameOverAndRestart'.
    /// </summary>
    /// <param name="delay">O tempo (em segundos) que ela deve esperar.</param>
    private IEnumerator FadeAndReloadScene(float delay)
    {
        Debug.Log($"GAME OVER! Iniciando fade ({delay}s) antes de recarregar.");

        // 'yield return new WaitForSeconds(delay)'
        // Esta é a "pausa". O código para AQUI e espera
        // 'delay' segundos (ex: 4.0s) antes de continuar.
        yield return new WaitForSeconds(delay);

        // Pega o nome da cena que estamos JOGANDO AGORA.
        string sceneToLoad = SceneManager.GetActiveScene().name;

        // Manda recarregar essa mesma cena.
        Debug.Log($"Recarregando a cena: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}