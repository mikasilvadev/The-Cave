using UnityEngine;
using UnityEngine.AI; // Precisamos disso para controlar o NavMeshAgent (o "GPS" do monstro)

/// <summary>
/// Este é o estado "Tático de Recuo".
/// A IA entra aqui quando estava perseguindo o player (ChaseState)
/// e o player DESLIGA a lanterna. O monstro fica "cego" e decide
/// recuar para um local seguro antes de começar a procurar (SearchState).
/// Ele herda (:) de BaseState, então é obrigado a ter os métodos Enter, Execute e Exit.
/// </summary>
public class RetreatState : BaseState
{
    // --- Variáveis (Campos) ---

    // A distância (em metros) que o monstro vai tentar correr para trás.
    private float retreatDistance = 7f;

    /// <summary>
    /// Método 'Enter' (Entrada). Roda SÓ UMA VEZ quando a IA entra neste estado.
    /// Configura o recuo.
    /// </summary>
    public override void Enter(AIController controller)
    {
        // Manda uma mensagem pro Console do Unity pra gente saber o que tá rolando.
        Debug.Log("<color=purple>RETREAT:</color> Jogador apagou a luz. Calculando ponto de recuo tático...", controller.gameObject);

        // Define a velocidade. Ele não recua correndo desesperado (chaseSpeed),
        // ele recua na velocidade de "busca" (searchSpeed). É uma escolha de design.
        controller.Agent.speed = controller.searchSpeed;

        // 'autoBraking = true' faz o Agent desacelerar suavemente quando
        // estiver chegando no destino. Fica mais natural.
        controller.Agent.autoBraking = true;

        // --- A PARTE INTELIGENTE ---
        // Chama a função 'FindBestRetreatPoint' (lá embaixo) para "pensar"
        // no melhor local para recuar.
        Vector3 retreatPoint = FindBestRetreatPoint(controller);

        // --- PLANO B (Failsafe) ---
        // Se a função não achou nenhum lugar bom (ela retorna Vector3.zero),
        // o monstro não fica parado sem saber o que fazer.
        if (retreatPoint == Vector3.zero)
        {
            Debug.LogWarning("<color=purple>RETREAT:</color> Não foi possível encontrar um ponto de recuo seguro. Iniciando busca imediatamente.", controller.gameObject);

            // Ele desiste de recuar e pula direto pro estado de Busca (SearchState).
            controller.ChangeState(new SearchState());
            return; // 'return' para a execução. Não faz mais nada no 'Enter'.
        }

        // --- PLANO A (Deu tudo certo) ---
        // Se ele achou um ponto bom, manda o Agent (GPS) ir para lá.
        Debug.Log($"<color=purple>RETREAT:</color> Recuando para o ponto {retreatPoint}", controller.gameObject);
        controller.Agent.SetDestination(retreatPoint);
    }

    /// <summary>
    /// Método 'Execute' (Execução). Roda A CADA FRAME (loop)
    /// enquanto a IA estiver *neste* estado (recuando).
    /// </summary>
    public override void Execute(AIController controller)
    {
        // --- CHECAGEM DE PRIORIDADE MÁXIMA (INTERRUPÇÃO) ---
        // Se o player for "corajoso" (ou burro) e acender a luz DE NOVO
        // no meio do recuo do monstro...
        if (controller.Senses.playerFlashlight.enabled && controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            // O recuo é CANCELADO NA HORA.
            // O monstro vê o player e volta pro estado de Perseguição total.
            controller.ChangeState(new ChaseState());
            return; // Para a execução deste frame.
        }

        // --- CHECAGEM DE CHEGADA ---
        // Se o monstro não foi interrompido, ele continua recuando.
        // '!controller.Agent.pathPending' = O Agent já calculou a rota.
        // 'controller.Agent.remainingDistance <= 1.0f' = Ele chegou perto o suficiente (a 1 metro) do destino.
        if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= 1.0f)
        {
            // Ele chegou no local seguro. Missão "Recuar" cumprida.
            // Agora ele começa a "Procurar" (SearchState) o player,
            // começando deste novo ponto seguro.
            controller.ChangeState(new SearchState());
        }
    }

    /// <summary>
    /// Esta é a função "cérebro" que calcula o melhor lugar para recuar.
    /// Ela é 'private' porque só este script (RetreatState) precisa usar ela.
    /// </summary>
    /// <returns>Retorna um Vector3 (coordenada) do melhor ponto, ou Vector3.zero se não achar.</returns>
    private Vector3 FindBestRetreatPoint(AIController controller)
    {
        // "Memória" da função. Guarda o melhor ponto achado até agora.
        Vector3 bestPoint = Vector3.zero;

        // Guarda a distância desse "melhor ponto" (começa em 0).
        float maxDistanceToPlayer = 0f;

        // Faz um loop 'for' que roda 3 vezes (i=0, i=1, i=2).
        // Vamos testar 3 direções de recuo.
        for (int i = 0; i < 3; i++)
        {
            // 1. CALCULAR A DIREÇÃO BASE
            // Pega a direção OPOSTA ao player.
            // (Posição do Monstro - Posição do Player) = Vetor "para longe" do player.
            // '.normalized' transforma isso num vetor de comprimento 1 (só a direção).
            Vector3 directionAwayFromPlayer = (controller.transform.position - controller.Player.position).normalized;

            // 2. TESTAR DIREÇÕES DIFERENTES (a parte inteligente)
            // 'i == 0' = Usa a direção pura (para trás).
            if (i == 1) directionAwayFromPlayer = Quaternion.Euler(0, 45, 0) * directionAwayFromPlayer; // Tenta "trás-direita"
            if (i == 2) directionAwayFromPlayer = Quaternion.Euler(0, -45, 0) * directionAwayFromPlayer; // Tenta "trás-esquerda"
            // Isso (multiplicar por Quaternion) é um truque de matemática pra girar um vetor.

            // 3. CALCULAR O PONTO-ALVO
            // Pega o ponto de partida (onde o monstro tá) e soma a direção * a distância.
            // Ex: "Ponto atual + 7 metros para trás-direita".
            Vector3 targetPoint = controller.transform.position + directionAwayFromPlayer * retreatDistance;

            // 4. VERIFICAR SE O PONTO É VÁLIDO NO "CHÃO" (NavMesh)
            // 'NavMesh.SamplePosition' é a mágica. Ele checa:
            // "Existe algum ponto no NavMesh (chão) perto de 'targetPoint'?"
            // (num raio de 2.0f, no caso).
            // Se achar, ele salva em 'hit' e retorna 'true'.
            if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                // 5. VERIFICAR SE O PONTO VÁLIDO É "BOM"
                // Ok, achamos um ponto no chão ('hit.position').
                // Mas esse ponto é realmente LONGE do player?
                // (Evita ele recuar pra trás de uma pilastra e ainda estar do lado do player).
                if (Vector3.Distance(hit.position, controller.Player.position) > retreatDistance - 1f)
                {
                    // 6. VERIFICAR SE É O "MELHOR" PONTO ATÉ AGORA
                    // Se a distância desse ponto até o player for a MAIOR que já vimos...
                    if (Vector3.Distance(hit.position, controller.Player.position) > maxDistanceToPlayer)
                    {
                        // ...nós atualizamos nossa "memória".
                        maxDistanceToPlayer = Vector3.Distance(hit.position, controller.Player.position); // Salva a nova distância máxima
                        bestPoint = hit.position; // Salva esse ponto como o "melhor"
                    }
                }
            }
        }

        // 7. DEVOLVER O RESULTADO
        // Depois de testar as 3 direções, retorna o 'bestPoint' que achamos.
        // (Se não achou nenhum, ele ainda vai ser Vector3.zero, como começou).
        return bestPoint;
    }

    /// <summary>
    /// Método 'Exit' (Saída). Roda SÓ UMA VEZ quando a IA sai deste estado.
    /// </summary>
    public override void Exit(AIController controller)
    {
        // Não precisa "limpar" nada.
        // O próximo estado (SearchState) vai reconfigurar o Agent do jeito que ele precisa.
    }
}