using UnityEngine;
using UnityEngine.AI;

public class WanderingAI : MonoBehaviour
{
    // Estados da IA
    private enum AIState { Wandering, Chasing, Searching }
    private AIState currentState;

    // Componentes e Referências
    private NavMeshAgent agent;
    public Transform playerTransform;           // Neste Transform deverá ter o objeto Player, que será perseguido
    public Transform eyeTransform;              // Neste transform deverá ter os "Olhos", objeto vázio criado pra simbolizar o olhos

    // Configs de passeio (Wandering)
    public float wanderRadius = 10f;            // Raio da área onde ele vai procurar um novo ponto para andar
    public float wanderTimer = 5f;              // Tempo de espera antes de procurar um novo destino
    private float timer;

    // Configuração de "Visão" e "Perseguição" (Chasing)
    public float sightDistance = 15f;           // O quão longe ele pode ver
    public float fieldOfViewAngle = 90;         // O ângulo de visão (90 graus é um cone frontal)
    public float chaseSpeed = 12f;              // Velocidade quando estiver perseguindo
    private float originalSpeed;                // Guarda a velocidade original
    private Vector3 lastKnownPlayerPosition;    // "Memória" da IA. Guarda a última posição em que o jogador foi visto

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();   // Pega a referência do componente NavMeshAgent
        originalSpeed = agent.speed;            // Guarda a velocidade normal de passeio
        timer = wanderTimer;                    // Inicializa o timer
        currentState = AIState.Wandering;       // Faz começar no estado "Passeando"

        if (playerTransform == null)
        {
            Debug.LogError("Referência do Player não atribuída no Inspector");
        }
        if (eyeTransform == null)
        {
            Debug.LogWarning("Referência do Olho não atribuída no Inspector");
            eyeTransform = this.transform;
        }
    }
    void Update()
    {
        // A máquina de estados decide qual lógica executar
        switch (currentState)
        {
            case AIState.Wandering:
                HandleWandering();
                break;
            case AIState.Chasing:
                HandleChasing();
                break;
            case AIState.Searching:
                HandleSearching();
                break;
        }
    }

    // Lógica dos estados

    private void HandleWandering()
    {
        agent.speed = originalSpeed; // Garante que está na velocidade normal
        // Se vir o jogador nequanto passeia, começa a perseguição.
        if (IsPlayerVisible())
        {
            TransitionToState(AIState.Chasing);
            return;
        }

        // Lógica de se mover para um ponto aleatório.
        timer += Time.deltaTime;
        if (timer >= wanderTimer && HasReachedDestination())
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    private void HandleChasing()
    {
        agent.speed = chaseSpeed;

        // Se ainda consegue ver o jogador, atualiza a posição e continua a perseguição
        if (IsPlayerVisible())
        {
            // Única parte do código que atualiza o destino durante a perseguição
            lastKnownPlayerPosition = playerTransform.position;
            agent.SetDestination(lastKnownPlayerPosition);
        }
        // Se perdeu o jogador de vista, para de atualizar e começa a procurar.
        else
        {
            TransitionToState(AIState.Searching);
        }
    }

    private void HandleSearching()
    {
        agent.speed = originalSpeed; // Procura em velocidade normal.

        // Se reencontrar o jogador enquanto procura, volta a persegui-lo.
        if (IsPlayerVisible())
        {
            TransitionToState(AIState.Chasing);
            return;
        }

        // Se chegou na última posição conhecida e não encontrou o jogador, desiste.
        if (HasReachedDestination())
        {
            TransitionToState(AIState.Wandering);
        }
    }


    // Funções de Apoio
    // Está é a função mais importante, os "Olhos" da IA, retorna apenas TRUE ou FALSE, não muda nenhum estado
    private bool IsPlayerVisible()
    {
        if (playerTransform == null) return false;

        // 1. Checagem de distância (a partir do olho)
        if (Vector3.Distance(eyeTransform.position, playerTransform.position) > sightDistance)
        {
            return false;
        }

        Vector3 directionToPlayer = (playerTransform.position - eyeTransform.position).normalized;
        if (Vector3.Angle(eyeTransform.forward, directionToPlayer) > fieldOfViewAngle / 2)
        {
            return false;
        }

        // 3. Checagem de linha de visão (Raycast)
            RaycastHit hit;
        if (Physics.Raycast(eyeTransform.position, directionToPlayer, out hit, sightDistance))
        {
            // Só retorna true se o raio atingir o jogador.
            if (hit.transform == playerTransform)
            {
                return true;
            }
        }

        // Se qualquer checagem falhar, retorna false.
        return false;
    }

    private void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;
        Debug.Log($"Mudando de estado: {currentState} -> {newState}");
        currentState = newState;
    }

    // Verifica se o agente chegou ao seu destino.
    private bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    void OnDrawGizmosSelected()
    {
        // Garante que o ponto de olho exista
        if (eyeTransform == null)
        {
            eyeTransform = this.transform;
        }

        // Define a cor do Gizmo
        Gizmos.color = Color.yellow;

        // Desenha uma esfera mostrando o raio ded visão máximo
        Gizmos.DrawWireSphere(eyeTransform.position, sightDistance);

        // Calcula os vetores para as bordas do cone de visão
        Vector3 forward = eyeTransform.forward;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-fieldOfViewAngle / 2, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(fieldOfViewAngle / 2, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        // Desenha as linhas que formam o cone
        Gizmos.DrawRay(eyeTransform.position, leftRayDirection * sightDistance);
        Gizmos.DrawRay(eyeTransform.position, rightRayDirection * sightDistance);
    }

    // Método que encontra um ponto aleatório na NavMesh dentro de um raio
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        // Gera uma direção aleatória dentro de uma esfera
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        // Procura o ponto mais próximo na NavMesh
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, layermask))
        {
            return navHit.position;
        }
        return origin;
    }
}
