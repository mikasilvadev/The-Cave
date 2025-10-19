using UnityEngine;
using UnityEngine.AI; // Precisamos disso para controlar o NavMeshAgent (o "GPS" do monstro)

/// <summary>
/// Este é o estado "padrão" ou "ocioso" da IA.
/// É o que o monstro faz quando não está caçando, investigando ou recuando.
/// A lógica é simples: ele anda até um ponto aleatório, para, espera um tempo,
/// e escolhe outro ponto aleatório. Fica nesse ciclo.
/// Ele herda (:) de BaseState, então é obrigado a ter os métodos Enter, Execute e Exit.
/// </summary>
public class WanderState : BaseState
{
    // --- Variáveis (Campos) ---
    // Estas são as "memórias" do estado.

    // 'wanderRadius' é o "raio" (em metros) da "bolha" em volta do monstro
    // onde ele vai procurar um novo ponto aleatório. 20m é um bom valor.
    private float wanderRadius = 20f;

    // 'idleTimer' é o nosso cronômetro de espera.
    private float idleTimer;

    /// <summary>
    /// Assim como no SearchState, criamos uma "mini-máquina de estados"
    /// só para o Wander. O ciclo dele é dividido em duas fases:
    /// </summary>
    private enum WanderPhase
    {
        Idle,    // Fase 1: Parado, esperando o timer
        Walking  // Fase 2: Andando até o ponto aleatório
    }

    // 'currentPhase' guarda em qual fase o monstro está agora.
    private WanderPhase currentPhase;

    /// <summary>
    /// Método 'Enter' (Entrada). Roda SÓ UMA VEZ quando a IA entra neste estado.
    /// Configura o estado inicial de "vagar".
    /// </summary>
    public override void Enter(AIController controller)
    {
        // Manda um log pro Console pra gente saber que ele entrou aqui.
        Debug.Log("WANDER: Iniciando estado de vagar.", controller.gameObject);

        // Define a velocidade do "GPS" (Agent) para a velocidade de "vagar".
        // (Essa variável 'wanderSpeed' vem lá do AIController).
        controller.Agent.speed = controller.wanderSpeed;

        // --- LÓGICA INTELIGENTE DE INÍCIO ---
        // Força ele a começar na fase "Parado".
        currentPhase = WanderPhase.Idle;

        // Define o cronômetro de espera.
        // Pega um tempo aleatório entre o MÍNIMO e o MÁXIMO (definidos no AIController).
        // A gente divide por 2f (metade) SÓ NA PRIMEIRA VEZ (no Enter),
        // pra ele não ficar parado muito tempo assim que o jogo começa.
        idleTimer = Random.Range(controller.minWanderIdleTime, controller.maxWanderIdleTime) / 2f;

        // Manda o "GPS" (Agent) PARAR de se mover.
        controller.Agent.isStopped = true;
    }

    /// <summary>
    /// Método 'Execute' (Execução). Roda A CADA FRAME (loop)
    /// enquanto a IA estiver *neste* estado (vagando).
    /// </summary>
    public override void Execute(AIController controller)
    {
        // --- CHECAGEM DE PRIORIDADE MÁXIMA (INTERRUPÇÃO) ---
        // A *primeira coisa* que ele faz, todo frame, é checar:
        // "Estou vendo o player?"
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            // Se SIM, o estado Wander ACABA NA HORA.
            Debug.LogWarning("WANDER: Jogador detectado! Mudando para ChaseState.", controller.gameObject);

            // Salva onde viu o player.
            controller.LastKnownPlayerPosition = playerPos;

            // Muda IMEDIATAMENTE para o estado de Perseguição (ChaseState).
            controller.ChangeState(new ChaseState());
            return; // 'return' para a execução deste frame. Não faz mais nada.
        }

        // --- LÓGICA DO CICLO DE VAGAR ---
        // Se ele não viu o player, ele continua o ciclo dele.
        // O 'switch' checa qual é a 'currentPhase' (fase atual).
        switch (currentPhase)
        {
            // --- FASE 1: PARADO (Idle) ---
            case WanderPhase.Idle:

                // Diminui o cronômetro 'idleTimer' aos pouquinhos.
                // 'Time.deltaTime' é o "tempo que passou desde o último frame".
                // (Ex: se o timer era 5.0, no próximo frame será 4.98, depois 4.96...)
                idleTimer -= Time.deltaTime;

                // Se o cronômetro chegou a zero ou menos...
                if (idleTimer <= 0)
                {
                    // ...o tempo de espera acabou. Hora de andar.
                    // MUDA A FASE.
                    currentPhase = WanderPhase.Walking;

                    // Manda o "GPS" (Agent) poder se mover de novo.
                    controller.Agent.isStopped = false;

                    // Chama a função lá de baixo pra achar um novo destino aleatório.
                    SetRandomDestination(controller);
                }
                break; // Fim da Fase 1.

            // --- FASE 2: ANDANDO (Walking) ---
            case WanderPhase.Walking:

                // Checa se o "GPS" (Agent) já chegou no destino.
                // '!controller.Agent.pathPending' = O Agent já calculou a rota.
                // 'controller.Agent.remainingDistance <= controller.Agent.stoppingDistance' = A distância que falta
                // é menor ou igual à distância de parada. (ELE CHEGOU!)
                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    // Ele chegou no ponto aleatório. Hora de parar e esperar.
                    // MUDA A FASE.
                    currentPhase = WanderPhase.Idle;

                    // Manda o "GPS" (Agent) PARAR de se mover.
                    controller.Agent.isStopped = true;

                    // Define um NOVO tempo de espera aleatório (desta vez, completo).
                    idleTimer = Random.Range(controller.minWanderIdleTime, controller.maxWanderIdleTime);

                    // Manda um log pra gente saber quanto tempo ele vai esperar.
                    // O '{idleTimer:F1}' formata o número pra mostrar só 1 casa decimal (ex: "5.2 segundos").
                    Debug.Log($"WANDER: Chegou ao destino. Ficando ocioso por {idleTimer:F1} segundos.", controller.gameObject);
                }
                break; // Fim da Fase 2.
        }
    }

    /// <summary>
    /// Método 'Exit' (Saída). Roda SÓ UMA VEZ quando a IA sai deste estado.
    /// Usamos para "limpar a casa" antes de ir pro próximo estado.
    /// </summary>
    public override void Exit(AIController controller)
    {
        // Garante que o Agent possa se mover no próximo estado
        // (já que ele podia estar 'parado' na fase Idle).
        controller.Agent.isStopped = false;

        // Limpa qualquer rota/caminho que ele estava seguindo.
        // Se ele estava indo pro "Ponto A" e viu o player,
        // ele "esquece" o Ponto A antes de começar a perseguir (Chase).
        controller.Agent.ResetPath();
    }

    /// <summary>
    /// Esta é a função "cérebro" que acha um ponto aleatório VÁLIDO.
    /// Ela é 'private' porque só este script (WanderState) precisa usar ela.
    /// </summary>
    private void SetRandomDestination(AIController controller)
    {
        // 1. ACHA UM PONTO ALEATÓRIO NUMA "BOLHA"
        // 'Random.insideUnitSphere' pega uma direção aleatória (x,y,z) dentro de uma esfera de raio 1.
        // Multiplicar pelo 'wanderRadius' (ex: 20) estica essa esfera.
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;

        // Soma a posição ATUAL do monstro.
        // (Resultado: um ponto aleatório a até 20m de distância do monstro).
        randomDirection += controller.transform.position;

        // 2. VERIFICA SE O PONTO É VÁLIDO (A PARTE IMPORTANTE)
        // O 'randomDirection' pode ter caído dentro de uma parede ou no ar.
        // 'NavMesh.SamplePosition' checa:
        // "Existe algum ponto no NavMesh (chão) perto de 'randomDirection'?"
        // (num raio de 'wanderRadius', em qualquer área do NavMesh).
        // Se achar, ele salva em 'hit' e retorna 'true'.
        NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas);

        // 3. DEFINE O DESTINO
        // Manda o "GPS" (Agent) ir para 'hit.position' (o ponto VÁLIDO que o SamplePosition achou).
        controller.Agent.SetDestination(hit.position);

        // Manda um log pra gente saber pra onde ele decidiu ir.
        Debug.Log($"WANDER: Novo destino aleatório definido para {hit.position}", controller.gameObject);
    }
}