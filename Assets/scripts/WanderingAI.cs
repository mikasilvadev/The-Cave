using UnityEngine;
using UnityEngine.AI;

public class WanderingAI : MonoBehaviour
{
    // Variáveis de Estados
    private enum AIState { Wandering, Chasing }
    private AIState currentState;

    // Componentes e Referências
    private NavMeshAgent agent;
    public Transform playerTransform; // Neste Transform deverá ter o objeto Player, que será perseguido
    public Transform eyeTransform; // Neste transform deverá ter os "Olhos", objeto vázio criado pra simbolizar o olhos

    // Configs de Wandering
    public float wanderRadius = 10f;    // Raio da área onde ele vai procurar um novo ponto para andar
    public float wanderTimer = 5f;      // Tempo de espera antes de procurar um novo destino
    private float timer;

    // Configuração de "Visão" e "Perseguição" (Chasing)
    public float sightDistance = 15f;       // O quão longe ele pode ver
    public float fieldOfViewAngle = 90;     // O ângulo de visão (90 graus é um cone frontal)
    public float chaseSpeed = 12f;           // Velocidade quando estiver perseguindo
    private float originalSpeed;            // Guarda a velocidade original

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
                Wander();
                CheckForPlayer();
                break;

            case AIState.Chasing:
                Chase();
                break;
        }
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

    void Wander()
    {
        agent.speed = originalSpeed; // Garante que está na velocidade normal
          timer += Time.deltaTime;
        if (timer >= wanderTimer && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
             Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
              agent.SetDestination(newPos);
            timer = 0;
        }
    }

    void CheckForPlayer()
    {
        if (playerTransform == null) return;

        // 1. Checagem de distância
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > sightDistance)
        {
            return; // Player está muito longe, não faz mais nada
        }

        // 2. Checagem de ângulo de visão: O player está na frente do andarilho?
        Vector3 directionToPlayer = (playerTransform.position - eyeTransform.position).normalized;
        if (Vector3.Angle(eyeTransform.forward, directionToPlayer) > fieldOfViewAngle / 2)
        {
            return; // Player fora do cone de visão
        }

        // 3. Checagem de linha de visão (Raycast): Existe uma parede no caminho?
        RaycastHit hit;
        // Dispara um raio e verifica se o primeiro objeto atingido é o jogador
        if (Physics.Raycast(eyeTransform.position, directionToPlayer, out hit, sightDistance))
        {
            if (hit.transform == playerTransform)
            {
                // O raio atingiu o player, iniciando a perseguição.
                Debug.Log("Player avistado!");
                currentState = AIState.Chasing;
            }
            // Se atingiu outra coisa (uma parede, outro inimigo), o return implícito impede a perseguição.
        }
    }

    void Chase()
    {
        agent.speed = chaseSpeed; // Aumenta a velocidade
        agent.SetDestination(playerTransform.position); // Define o destino como a posição do player

        // Se o player fugir e sair da distância de visão, ele volta a passear
        if (Vector3.Distance(transform.position, playerTransform.position) > sightDistance * 1.5f) // Damos uma margem extra
        {
            currentState = AIState.Wandering;
        }
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