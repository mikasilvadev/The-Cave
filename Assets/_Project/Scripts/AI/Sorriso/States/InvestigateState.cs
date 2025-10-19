using UnityEngine;
using UnityEngine.AI; // Precisamos disso para usar o NavMeshAgent (o "GPS" do monstro)

/// <summary>
/// Este é o estado "O que foi isso?".
/// A IA entra aqui quando ouve um som ou vê um "respingo" de luz,
/// mas NÃO está vendo o player diretamente. Ele vai checar o local.
/// Ele herda (:) de BaseState, então ele é OBRIGADO a ter os métodos Enter, Execute e Exit.
/// </summary>
public class InvestigateState : BaseState
{
    // --- Variáveis (Campos) ---
    // Estas são as "memórias" do estado. Elas guardam informações
    // enquanto este estado estiver ativo.

    // 'investigatePosition' guarda o local (coordenada X, Y, Z) que o monstro vai investigar.
    private Vector3 investigatePosition;

    // 'reason' é a parte mais importante. É uma string (texto) que diz
    // O PORQUÊ de ele estar investigando. Foi "som" ou foi "foco de luz"?
    private string reason;

    // 'arrivedAtTarget' é uma flag (bandeira) 'true'/'false'.
    // Começa como 'false' (ele ainda não chegou) e vira 'true' quando ele chega.
    // Usamos isso pra dividir o estado em duas fases: "Indo até o local" e "Parado no local".
    private bool arrivedAtTarget = false;

    // 'maxWaitTime' é o tempo MÁXIMO (em segundos) que ele vai ficar
    // parado olhando, caso ele tenha ido investigar um "som".
    private float maxWaitTime = 5f;

    // 'currentWaitTime' é o cronômetro. Começa em 0 e vai subindo até
    // atingir o 'maxWaitTime'.
    private float currentWaitTime = 0f;

    /// <summary>
    /// Este é o "Construtor". É um método especial chamado SÓ QUANDO
    /// o estado é CRIADO (lá no AIController, quando fazemos "new InvestigateState(...)").
    /// Ele serve para "injetar" as informações que o estado precisa para funcionar.
    /// </summary>
    /// <param name="position">Onde o monstro deve ir.</param>
    /// <param name="reason">Por que ele deve ir (ex: "som").</param>
    public InvestigateState(Vector3 position, string reason = "ponto de interesse")
    {
        // 'this.investigatePosition' se refere à variável "memória" lá de cima.
        // 'position' é a variável que recebemos de quem nos criou.
        // Isso "salva" a posição na memória do estado.
        this.investigatePosition = position;

        // Salva o motivo também.
        this.reason = reason;
    }

    /// <summary>
    /// Método 'Enter' (Entrada). Roda SÓ UMA VEZ quando a IA entra neste estado.
    /// Usamos para configurar tudo antes da lógica principal começar.
    /// </summary>
    public override void Enter(AIController controller)
    {
        // --- LÓGICA DE COMPORTAMENTO INTELIGENTE ---
        // Aqui, checamos o *motivo* da investigação pra mudar a 'Distância de Parada'.
        if (reason == "foco de luz")
        {
            // Se ele viu uma luz, ele vai *exatamente* em cima do ponto de luz.
            // Por isso, a distância de parada é quase zero (0.1f).
            controller.Agent.stoppingDistance = 0.1f;
        }
        else // Se não foi "foco de luz" (ou seja, foi "som")
        {
            // Se ele ouviu um som, ele para um pouco ANTES do local (ex: 1 metro).
            // Isso é pra ele poder "olhar" pro local do som, em vez de ficar em cima.
            // Esse valor (soundStopDistance) vem lá do AIController.
            controller.Agent.stoppingDistance = controller.soundStopDistance;
        }
        // ------------------------------------------

        // Manda uma mensagem pro Console do Unity, pra gente saber o que a IA está fazendo.
        Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Detectado um {reason}! Indo investigar a posição {investigatePosition}", controller.gameObject);

        // Reseta as flags e timers, só pra garantir.
        arrivedAtTarget = false;
        currentWaitTime = 0f;

        // Configura o NavMeshAgent (o "GPS")
        controller.Agent.updateRotation = true; // Deixa o Agent controlar a rotação (virar)
        controller.Agent.isStopped = false;     // Manda ele ANDAR (não ficar parado)

        // Aqui usamos a velocidade de PERSEGUIÇÃO. Isso é uma escolha de design!
        // Significa que ele vai CORRENDO checar o barulho. Isso é ótimo pro terror.
        controller.Agent.speed = controller.chaseSpeed;

        // Define o destino final no "GPS".
        controller.Agent.SetDestination(investigatePosition);
    }

    /// <summary>
    /// Método 'Execute' (Execução). Roda A CADA FRAME (loop)
    /// enquanto a IA estiver *neste* estado. É a lógica principal.
    /// </summary>
    public override void Execute(AIController controller)
    {
        // --- CHECAGEM DE PRIORIDADE MÁXIMA ---
        // Não importa se ele tá investigando, se ele BATER O OLHO no player,
        // a investigação acaba NA HORA.
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("INVESTIGATE: Jogador encontrado durante investigação! Mudando para ChaseState.", controller.gameObject);
            controller.ChangeState(new ChaseState()); // Muda pro estado de Perseguição
            return; // 'return' para a execução deste frame. Não faz mais nada.
        }

        // --- FASE 1: INDO ATÉ O LOCAL ---
        // Se a nossa flag 'arrivedAtTarget' for 'false', rodamos essa parte.
        if (!arrivedAtTarget)
        {
            // Checamos se o Agent (GPS) já chegou no destino.
            // '!controller.Agent.pathPending' = O Agent já calculou a rota.
            // 'controller.Agent.remainingDistance <= controller.Agent.stoppingDistance' = A distância que falta
            // é menor ou igual à distância de parada que definimos. (ELE CHEGOU!)
            if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
            {
                // Manda um log pra gente saber que ele chegou.
                Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Chegou ao local do {reason}.", controller.gameObject);

                // Manda o Agent PARAR de andar.
                controller.Agent.isStopped = true;

                // ATUALIZA A FLAG! Agora ele 'arrivedAtTarget' é 'true'.
                // No próximo frame, ele vai pular este 'if' e ir pro 'else' lá embaixo.
                arrivedAtTarget = true;
            }
        }

        // --- FASE 2: PARADO NO LOCAL ---
        // Se a nossa flag 'arrivedAtTarget' for 'true', rodamos essa parte.
        if (arrivedAtTarget)
        {
            // --- LÓGICA DE COMPORTAMENTO BASEADA NO "MOTIVO" ---

            // Se o motivo foi "foco de luz"...
            if (reason == "foco de luz")
            {
                // Ele não espera. Ele já viu que o player não tá no ponto de luz.
                Debug.LogWarning("<color=cyan>INVESTIGATE:</color> A luz estava aqui. A fonte deve estar por perto. INICIANDO BUSCA!", controller.gameObject);

                // Ele "anota" o local da luz como a "Última Posição Conhecida".
                controller.LastKnownPlayerPosition = investigatePosition;

                // Muda IMEDIATAMENTE para o estado de Busca (SearchState).
                controller.ChangeState(new SearchState());
                return; // Para a execução.
            }
            // Se o motivo NÃO foi luz (ou seja, foi "som")...
            else
            {
                // Ele vai ficar parado e "olhar" na direção do som.

                // Calcula a direção do som (de onde ele veio)
                Vector3 lookDirection = (investigatePosition - controller.transform.position).normalized;
                lookDirection.y = 0; // Zera o Y pra ele não olhar pra cima ou pra baixo.

                // Se a direção for válida...
                if (lookDirection != Vector3.zero)
                {
                    // Calcula a rotação final (pra onde ele tem que olhar)
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                    // Gira o monstro SUAVEMENTE (Slerp) da rotação atual
                    // para a 'targetRotation', usando uma velocidade (investigateRotationSpeed).
                    controller.transform.rotation = Quaternion.Slerp(
                        controller.transform.rotation,
                        targetRotation,
                        Time.deltaTime * controller.investigateRotationSpeed
                    );
                }

                // Inicia o cronômetro.
                currentWaitTime += Time.deltaTime; // 'Time.deltaTime' é o tempo do último frame.

                // Se o tempo de espera (cronômetro) ultrapassou o máximo...
                if (currentWaitTime >= maxWaitTime)
                {
                    // Ele desiste de esperar.
                    Debug.Log("INVESTIGATE: Tempo de observação do som esgotado. Passando para Busca (SearchState).", controller.gameObject);
                    controller.LastKnownPlayerPosition = investigatePosition;
                    controller.ChangeState(new SearchState()); // Muda pra Busca.
                }
            }
        }
    }

    /// <summary>
    /// Método 'Exit' (Saída). Roda SÓ UMA VEZ quando a IA sai deste estado.
    /// Usamos para "limpar a casa" antes de ir pro próximo estado.
    /// </summary>
    public override void Exit(AIController controller)
    {
        // Garante que o Agent possa se mover no próximo estado
        // (já que nós o paramos aqui na FASE 2).
        controller.Agent.isStopped = false;
        Debug.Log("<color=#66d9ef>INVESTIGATE:</color> Fim da investigação.", controller.gameObject);
    }
}