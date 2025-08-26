using UnityEngine;
using UnityEngine.AI;

public class WanderingAI : MonoBehaviour
{
    private NavMeshAgent agent;

    // Raio da área onde ele vai procurar um novo ponto para andar
    public float wanderRadius = 10f;

    // Tempo de espera antes de procurar um novo destino
    public float wanderTimer = 5f;
    private float timer;

    void Start()
    {
        // Pega a referência do componente NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        // Inicializa o timer
        timer = wanderTimer;
    }
    void Update()
    {
        // Incrementa o timer
        timer += Time.deltaTime;

        // Se o timer atingiu o tempo de espera e o agente não está ocupado
        if (timer >= wanderTimer && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Encontra uma nova posição aleatória e se move para lá
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);

            // Reseta o timer
            timer = 0;
        }
    }

    // Método que encontra um ponto aleatório na NavMesh dentrod e um raio
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        // Gera uma direção aleatória dentro de uma esfera
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;

        // Procura o ponto mais próximo na NavMesh
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
}