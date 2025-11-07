using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIMovement : MonoBehaviour
{
    [Header("Configurações de Velocidade")]
    public float monitoringSpeed = 1.5f;
    public float chasingSpeed = 30.0f;

    [Header("Configurações de Agente (Ajuste Fino)")]
    [Tooltip("Quão rápido ele atinge a velocidade máxima.")]
    public float agentAcceleration = 200f;

    [Tooltip("Velocidade de giro em graus/s. 2000+ para curvas super fechadas.")]
    public float agentAngularSpeed = 3000f;

    public bool autoBraking = true;

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
            Debug.LogError("AIMovement: NavMeshAgent não encontrado", gameObject);
            enabled = false;
            return;
        }

        agent.stoppingDistance = defaultStoppingDistance;
        agent.acceleration = agentAcceleration;
        agent.angularSpeed = agentAngularSpeed;
        agent.autoBraking = autoBraking;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    void Update()
    {
        if (agent != null)
        {
            if (agent.acceleration != agentAcceleration) agent.acceleration = agentAcceleration;
            if (agent.angularSpeed != agentAngularSpeed) agent.angularSpeed = agentAngularSpeed;
        }
    }

    public bool IsMoving()
    {
        if (!IsAgentValid()) return false;
        return !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;
    }

    public void MoveTo(Vector3 destination)
    {
        if (!IsAgentValid()) return;

        if (!agent.SetDestination(destination))
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(destination, out hit, maxSearchRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        if (agent.isStopped) agent.isStopped = false;
    }

    public void FollowTarget(Transform target)
    {
        if (target == null) return;
        MoveTo(target.position);
    }

    public void StopMovement()
    {
        if (IsAgentValid() && !agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    public void SetSpeed(float speed)
    {
        if (IsAgentValid()) agent.speed = speed;
    }

    public void SetStoppingDistance(float newDistance)
    {
        if (IsAgentValid()) agent.stoppingDistance = newDistance;
    }

    private bool IsAgentValid()
    {
        return agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled;
    }
}