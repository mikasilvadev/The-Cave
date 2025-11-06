using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIMovement : MonoBehaviour
{
    [Header("Configurações de Velocidade")]
    public float monitoringSpeed = 1.5f;
    public float chasingSpeed = 30.0f;

    [Header("Configurações de Agente (Ajuste Aqui)")]
    [Tooltip("Controla a velocidade que o monstro atinge a velocidade máxima. (Maior = Mais Ágil)")]
    public float agentAcceleration = 500f;

    [Tooltip("Controla a velocidade de rotação do monstro durante o NavMesh. (Maior = Curvas mais Rápidas)")]
    public float agentAngularSpeed = 1080f;

    [Tooltip("Distância máxima para procurar pontos válidos na NavMesh")]
    public float maxSearchRadius = 10f;

    private NavMeshAgent agent;
    private float defaultStoppingDistance = 0.5f;
    public NavMeshAgent Agent { get { return agent; } }

    void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("AIMovement: Não foi encontrado um componente NavMeshAgent", this.gameObject);
            enabled = false;
            return;
        }

        agent.stoppingDistance = defaultStoppingDistance;
        agent.acceleration = agentAcceleration;
        agent.angularSpeed = agentAngularSpeed;
    }

    public bool IsMoving()
    {
        if (!agent.isOnNavMesh) return false;
        return !agent.isStopped && agent.velocity.magnitude > 0.1f;
    }

    public void MoveTo(Vector3 destination)
    {
        if (!IsAgentValid()) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, maxSearchRadius, NavMesh.AllAreas))
        {
            try
            {
                agent.SetDestination(hit.position);
                agent.isStopped = false;
                agent.updateRotation = false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"AIMovement: Erro ao mover para {destination}: {e.Message}");
                StopMovement();
            }
        }
        else
        {
            Debug.LogWarning($"AIMovement: Não encontrou ponto válido próximo a {destination}");
            StopMovement();
        }
    }

    public void FollowTarget(Transform target)
    {
        if (target == null) return;
        MoveTo(target.position);
    }

    public void FollowAtDistance(Vector3 targetPosition, float distance)
    {
        if (!IsAgentValid()) return;
        agent.stoppingDistance = defaultStoppingDistance;
        float currentDistance = Vector3.Distance(transform.position, targetPosition);
        if (currentDistance > distance)
        {
            MoveTo(targetPosition);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    public void StopMovement()
    {
        if (!IsAgentValid()) return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void SetSpeed(float speed)
    {
        if (!IsAgentValid()) return;
        agent.speed = speed;
    }

    public void SetStoppingDistance(float newDistance)
    {
        if (IsAgentValid())
        {
            agent.stoppingDistance = newDistance;
        }
    }

    private bool IsAgentValid()
    {
        return agent != null && agent.isOnNavMesh;
    }
}