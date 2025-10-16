using UnityEngine;
using UnityEngine.AI;

public class WanderState : BaseState
{
    private float wanderRadius = 20f;
    private float idleTimer;

    private enum WanderPhase
    {
        Idle,
        Walking
    }
    private WanderPhase currentPhase;

    public override void Enter(AIController controller)
    {
        Debug.Log("WANDER: Iniciando estado de vagar.", controller.gameObject);
        controller.Agent.speed = controller.wanderSpeed;

        // Começa no estado ocioso para ter uma pausa antes do primeiro movimento
        currentPhase = WanderPhase.Idle;
        idleTimer = Random.Range(controller.minWanderIdleTime, controller.maxWanderIdleTime) / 2f; // Primeira pausa é mais curta
        controller.Agent.isStopped = true; // Garante que ele comece parado
    }

    public override void Execute(AIController controller)
    {
        // A detecção do jogador é sempre a prioridade máxima
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("WANDER: Jogador detectado! Mudando para ChaseState.", controller.gameObject);
            controller.LastKnownPlayerPosition = playerPos;
            controller.ChangeState(new ChaseState());
            return;
        }

        // Lógica baseada na fase atual
        switch (currentPhase)
        {
            case WanderPhase.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0)
                {
                    // Tempo de espera acabou, começa a andar
                    currentPhase = WanderPhase.Walking;
                    controller.Agent.isStopped = false;
                    SetRandomDestination(controller);
                }
                break;

            case WanderPhase.Walking:
                // Se chegou ao destino, muda para ocioso
                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    currentPhase = WanderPhase.Idle;
                    controller.Agent.isStopped = true;
                    // Define um novo tempo de espera aleatório
                    idleTimer = Random.Range(controller.minWanderIdleTime, controller.maxWanderIdleTime);
                    Debug.Log($"WANDER: Chegou ao destino. Ficando ocioso por {idleTimer:F1} segundos.", controller.gameObject);
                }
                break;
        }
    }

    public override void Exit(AIController controller)
    {
        // Garante que o agente possa se mover ao sair deste estado
        controller.Agent.isStopped = false;
        controller.Agent.ResetPath();
    }

    private void SetRandomDestination(AIController controller)
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += controller.transform.position;
        NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas);
        controller.Agent.SetDestination(hit.position);

        Debug.Log($"WANDER: Novo destino aleatório definido para {hit.position}", controller.gameObject);
    }
}