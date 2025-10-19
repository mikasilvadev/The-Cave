using UnityEngine;
using UnityEngine.AI; // Precisamos disso para controlar o NavMeshAgent (o "GPS" do monstro)

/// <summary>
/// Este é o estado de "Busca Inteligente".
/// A IA entra aqui quando perdeu o player de vista (depois do Chase ou Investigate).
/// O objetivo é ir até a última posição vista, e depois TENTAR ADIVINHAR
/// para onde o player foi, baseado na última velocidade dele.
/// Ele herda (:) de BaseState, então é obrigado a ter os métodos Enter, Execute e Exit.
/// </summary>
public class SearchState : BaseState
{
    // --- Variáveis (Campos) ---
    // Estas são as "memórias" do estado.

    // 'searchTimer' é o nosso cronômetro.
    private float searchTimer;

    // 'timeToGiveUp' é o tempo máximo (em segundos) que ele vai ficar
    // parado "procurando" (girando) antes de desistir.
    private float timeToGiveUp = 7f;

    /// <summary>
    /// Aqui, criamos uma "mini-máquina de estados" SÓ PARA O SEARCH.
    /// Um 'enum' (enumerador) é uma forma de criar um tipo de variável
    /// que só pode ter alguns valores específicos.
    /// A nossa 'SearchPhase' só pode ser um desses 3 valores:
    /// </summary>
    private enum SearchPhase
    {
        MovingToLastSeen,   // Fase 1: Indo para o último local que viu o player
        MovingToPrediction, // Fase 2: Indo para o local "previsto"
        SearchingOnSpot     // Fase 3: Parado no local, girando e procurando
    }

    // 'currentPhase' é a variável que "guarda" em qual fase o monstro está agora.
    private SearchPhase currentPhase;

    /// <summary>
    /// Método 'Enter' (Entrada). Roda SÓ UMA VEZ quando a IA entra neste estado.
    /// Configura a busca inicial.
    /// </summary>
    public override void Enter(AIController controller)
    {
        // Manda uma mensagem pro Console, avisando que a Fase 1 começou.
        // O '{controller.LastKnownPlayerPosition}' pega a "memória" da IA
        // de onde o player foi visto pela última vez.
        Debug.Log($"<color=yellow>SEARCH:</color> FASE 1: Indo para a última posição vista em {controller.LastKnownPlayerPosition}", controller.gameObject);

        // Define a fase inicial.
        currentPhase = SearchPhase.MovingToLastSeen;

        // Define a velocidade de "busca" (mais lento que a perseguição).
        controller.Agent.speed = controller.searchSpeed;

        // Manda o "GPS" (Agent) ir para a última posição conhecida do player.
        controller.Agent.SetDestination(controller.LastKnownPlayerPosition);

        // Zera o cronômetro (ele só vai ser usado na Fase 3).
        searchTimer = 0f;
    }

    /// <summary>
    /// Método 'Execute' (Execução). Roda A CADA FRAME (loop)
    /// enquanto a IA estiver *neste* estado (buscando).
    /// </summary>
    public override void Execute(AIController controller)
    {
        // --- CHECAGEM DE PRIORIDADE MÁXIMA (INTERRUPÇÃO) ---
        // Se, a C-U-A-L-Q-U-E-R momento da busca, ele te ver...
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            // A busca é CANCELADA.
            Debug.LogWarning("SEARCH: Jogador reencontrado durante a busca! Voltando para ChaseState.", controller.gameObject);

            // Atualiza a memória (última posição vista é "agora").
            controller.LastKnownPlayerPosition = playerPos;

            // Muda IMEDIATAMENTE para o estado de Perseguição (ChaseState).
            controller.ChangeState(new ChaseState());
            return; // 'return' para a execução deste frame. Não faz mais nada.
        }

        // --- LÓGICA DAS FASES DE BUSCA ---
        // O 'switch' é como um "if / else if / else" mais organizado.
        // Ele vai checar o valor da nossa variável 'currentPhase'
        // e rodar o código do 'case' correspondente.
        switch (currentPhase)
        {
            // --- FASE 1: INDO PARA O ÚLTIMO LOCAL VISTO ---
            case SearchPhase.MovingToLastSeen:

                // Checa se o "GPS" (Agent) já chegou no destino.
                // '!controller.Agent.pathPending' = O Agent já calculou a rota.
                // 'controller.Agent.remainingDistance <= controller.Agent.stoppingDistance' = A distância que falta
                // é menor ou igual à distância de parada. (ELE CHEGOU!)
                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    // --- COMEÇA A LÓGICA DE PREDIÇÃO (A PARTE INTELIGENTE) ---
                    Debug.Log("<color=yellow>SEARCH:</color> Chegou na última posição. FASE 2: Calculando e indo para a posição prevista.", controller.gameObject);

                    // Pega as "memórias" salvas pelo AIController (lá no ChaseState).
                    Vector3 lastPos = controller.LastKnownPlayerPosition; // Onde ele estava
                    Vector3 lastVel = controller.LastKnownPlayerVelocity; // Pra onde ele estava indo (velocidade)

                    // **A MÁGICA:** Calcula a posição futura "prevista".
                    // Posição Futura = Posição Atual + (Velocidade * Tempo no Futuro)
                    // (Ex: Onde ele vai estar daqui a 1.5 segundos?)
                    Vector3 predictedPosition = lastPos + lastVel * controller.predictionTime;

                    // **A SEGURANÇA:** O 'predictedPosition' pode estar numa parede!
                    // 'NavMesh.SamplePosition' acha o ponto "válido" (no chão/NavMesh)
                    // MAIS PRÓXIMO do nosso ponto previsto (num raio de 5f).
                    // Isso "puxa" o destino pra um lugar que o monstro PODE andar.
                    NavMesh.SamplePosition(predictedPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas);
                    Vector3 destination = hit.position; // O destino seguro.

                    // Manda um log avisando pra onde ele "acha" que o player foi.
                    Debug.Log($"<color=yellow>SEARCH:</color> Prevendo que o jogador estará em <color=cyan>{destination}</color> daqui a {controller.predictionTime}s", controller.gameObject);

                    // Manda o "GPS" (Agent) ir para este NOVO ponto (o previsto).
                    controller.Agent.SetDestination(destination);

                    // MUDA A FASE! Agora ele vai pra Fase 2.
                    currentPhase = SearchPhase.MovingToPrediction;
                }
                break; // Fim da Fase 1.

            // --- FASE 2: INDO PARA O LOCAL PREVISTO ---
            case SearchPhase.MovingToPrediction:

                // Mesma lógica de checar se chegou.
                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    // Chegou no local previsto.
                    Debug.Log("<color=yellow>SEARCH:</color> Chegou ao local previsto. FASE 3: Procurando na área.", controller.gameObject);

                    // MUDA A FASE! Agora ele vai pra Fase 3.
                    currentPhase = SearchPhase.SearchingOnSpot;
                }
                break; // Fim da Fase 2.

            // --- FASE 3: PARADO NO LOCAL, PROCURANDO (GIRANDO) ---
            case SearchPhase.SearchingOnSpot:

                // Gira o monstro no próprio eixo Y (30 graus por segundo).
                // Isso dá a impressão de que ele está "olhando ao redor".
                controller.transform.Rotate(0, 30f * Time.deltaTime, 0);

                // Começa a contar o tempo do cronômetro.
                searchTimer += Time.deltaTime; // 'Time.deltaTime' é o tempo do último frame.

                // Checa se o tempo do cronômetro já passou do limite ('timeToGiveUp').
                if (searchTimer > timeToGiveUp)
                {
                    // O tempo acabou. Ele desiste.
                    Debug.Log("SEARCH: Tempo de busca esgotado. Desistindo e voltando a vagar.", controller.gameObject);

                    // Muda o estado para 'WanderState' (Vagar pelo mapa).
                    controller.ChangeState(new WanderState());
                }
                break; // Fim da Fase 3.
        }
    }

    /// <summary>
    /// Método 'Exit' (Saída). Roda SÓ UMA VEZ quando a IA sai deste estado.
    /// </summary>
    public override void Exit(AIController controller)
    {
        // Não precisa "limpar" nada.
        // O próximo estado (Wander ou Chase) vai reconfigurar o Agent.
    }
}