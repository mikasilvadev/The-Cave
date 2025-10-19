using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Este é o estado de perseguição direta. É o modo "Predador".
/// A IA entra aqui quando tem 100% de certeza de onde o player está (visão direta).
/// A lógica aqui é ser rápido, implacável e ter um "bote" final.
/// </summary>
public class ChaseState : BaseState
{
    // A que distância o monstro para do player.
    // Na prática, ele nunca vai usar isso, porque o "Lunge" (bote)
    // vai ativar antes dele chegar a 0.1m.
    private float stopDistance = 0.1f;

    // Timer pra não recalcular a rota (SetDestination) a cada frame.
    // É uma micro-otimização, ajuda a performance.
    private float pathUpdateInterval = 0.25f;
    private float pathUpdateTimer;

    // Flag pra controlar se ele já está no meio do "bote" final.
    private bool isLunging = false;

    public override void Enter(AIController controller)
    {
        Debug.Log("<color=red>CHASE:</color> Iniciando perseguição implacável!", controller.gameObject);

        // Configura o Agent pra correr
        controller.Agent.speed = controller.chaseSpeed;
        controller.Agent.stoppingDistance = stopDistance; // Seta a distância de parada mínima
        controller.Agent.isStopped = false;

        // Reseta as flags de controle
        isLunging = false;
        controller.Agent.updatePosition = true; // Garante que o NavMesh tá controlando a posição

        // **MUITO IMPORTANTE:** Eu desligo a rotação do NavMesh.
        // Eu quero controlar manualmente pra onde o monstro "olha".
        // Fica muito mais ameaçador ele te encarando enquanto corre.
        controller.Agent.updateRotation = false;

        pathUpdateTimer = 0f; // Zera o timer de atualização de rota
    }

    public override void Execute(AIController controller)
    {
        // Pega os sentidos (som) ANTES de decidir.
        // Mesmo correndo, ele ainda pode ouvir se o player mudar de direção rápido.
        Vector3 soundPos;
        bool heardSound = controller.Senses.CheckForSound(out soundPos);

        // A prioridade MÁXIMA é a visão.
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            // 1. LÓGICA DE "LUNGE" (BOTE / ATAQUE)
            float distanceToPlayer = Vector3.Distance(controller.transform.position, playerPos);
            Vector3 directionToPlayer = (playerPos - controller.transform.position).normalized;

            // Se o player estiver perto o suficiente (ex: 3m)
            if (distanceToPlayer < controller.lungeDistance)
            {
                // Se ele não estava dando o bote antes, ele começa AGORA.
                if (!isLunging)
                {
                    isLunging = true;
                    Debug.LogWarning("CHASE: Distância crítica! MODO DE ATAQUE COM FORÇA BRUTA!", controller.gameObject);

                    // **MUITO IMPORTANTE:** Eu desligo o controle de posição do NavMesh.
                    // Agora EU controlo o movimento do monstro manualmente.
                    controller.Agent.updatePosition = false;
                }

                // Move o monstro pra frente na marra, ignorando o NavMesh.
                // Isso dá o efeito de "bote", muito mais rápido e direto.
                controller.transform.position += directionToPlayer * controller.lungeSpeed * Time.deltaTime;
            }
            // 2. LÓGICA DE PERSEGUIÇÃO NORMAL (se estiver longe demais pro bote)
            else
            {
                if (!controller.Agent.updatePosition) controller.Agent.updatePosition = true;

                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    // --- NOVA LÓGICA DE DESTINO SEGURO ---
                    Vector3 targetPosition = playerPos; // Começa com a posição real

                    // Tenta achar o ponto MAIS PRÓXIMO no NavMesh perto do player
                    // (num raio de, por exemplo, 1 metro)
                    if (NavMesh.SamplePosition(playerPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                    {
                        // Se achou um ponto válido perto, usa ele como destino!
                        targetPosition = hit.position;
                    }
                    // Se SamplePosition falhar (muito raro), ele ainda usa playerPos.

                    // Define o destino SEGURO
                    controller.Agent.SetDestination(targetPosition);
                    // Debug.Log($"CHASE: Novo destino seguro: {targetPosition}"); // (Opcional: Log pra ver o destino)
                    // --- FIM DA NOVA LÓGICA ---

                    pathUpdateTimer = pathUpdateInterval;
                }
            }

            // 3. LÓGICA DE ROTAÇÃO (Sempre ativa)
            // Como eu desliguei o "Agent.updateRotation", eu giro ele manualmente.
            // Ele vai sempre "slerpar" (girar suavemente) pra encarar o player.
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                targetRotation,
                Time.deltaTime * controller.chaseRotationSpeed
            );

            // 4. ATUALIZAR MEMÓRIA
            // Guarda a última posição e velocidade do player.
            // Isso é ESSENCIAL pro "SearchState" (Busca) saber pra onde o player estava indo.
            controller.LastKnownPlayerPosition = playerPos;
            controller.LastKnownPlayerVelocity = controller.PlayerController.CurrentVelocity;
        }
        // **SE PERDEU O PLAYER DE VISTA**
        else
        {
            // 1. CHECAGEM DE LUZ (Mecânica Principal)
            // Se o player apagou a lanterna, o monstro PERDE o "lock" e recua.
            if (!controller.Senses.playerFlashlight.enabled)
            {
                Debug.LogWarning("CHASE: Perdeu o jogador de vista porque a luz apagou. Mudando para RetreatState.", controller.gameObject);
                controller.ChangeState(new RetreatState());
                return;
            }

            // 2. CHECAGEM DE SOM
            // A luz tá acesa, mas ele sumiu (atrás de uma parede, etc).
            // Mas ele ouviu um barulho?
            if (heardSound)
            {
                Debug.Log("CHASE: Perdeu de vista, mas ouviu som. Investigando o som.", controller.gameObject);
                controller.ChangeState(new InvestigateState(soundPos, "som no escuro"));
            }
            // 3. SEM SINAIS
            // Perdeu de vista, não ouviu nada. Hora de ADIVINHAR.
            else
            {
                Debug.LogWarning("CHASE: Perdeu o jogador de vista (bloqueio). Mudando para SearchState.", controller.gameObject);
                controller.ChangeState(new SearchState()); // Inicia a busca inteligente
            }
        }
    }

    public override void Exit(AIController controller)
    {
        // "Limpa a sujeira" antes de sair do estado.
        // Garante que o NavMesh volte a controlar tudo (posição e rotação).
        controller.Agent.updatePosition = true;
        controller.Agent.updateRotation = true;
        Debug.Log("<color=red>CHASE:</color> Fim da perseguição.", controller.gameObject);
    }
}