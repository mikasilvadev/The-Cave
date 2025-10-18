using UnityEngine;
using UnityEngine.AI;

public class InvestigateState : BaseState
{
    private Vector3 investigatePosition;
    private string reason;
    private bool arrivedAtTarget = false;
    private float maxWaitTime = 5f;
    private float currentWaitTime = 0f;

    public InvestigateState(Vector3 position, string reason = "ponto de interesse")
    {
        this.investigatePosition = position;
        this.reason = reason;
    }

    public override void Enter(AIController controller)
    {
        if (reason == "foco de luz")
        {
            controller.Agent.stoppingDistance = 0.1f;
        }
        else
        {
            controller.Agent.stoppingDistance = controller.soundStopDistance;
        }

        Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Detectado um {reason}! Indo investigar a posição {investigatePosition}", controller.gameObject);

        arrivedAtTarget = false;
        currentWaitTime = 0f;
        controller.Agent.updateRotation = true;
        controller.Agent.isStopped = false;
        controller.Agent.speed = controller.chaseSpeed;
        controller.Agent.SetDestination(investigatePosition);
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("INVESTIGATE: Jogador encontrado durante investigação! Mudando para ChaseState.", controller.gameObject);
            controller.ChangeState(new ChaseState());
            return;
        }

        if (!arrivedAtTarget)
        {
            if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
            {
                Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Chegou ao local do {reason}.", controller.gameObject);
                arrivedAtTarget = true;
                controller.Agent.isStopped = true;
            }
        }

        if (arrivedAtTarget)
        {
            if (reason == "foco de luz")
            {
                Debug.LogWarning("<color=cyan>INVESTIGATE:</color> A luz estava aqui. A fonte deve estar por perto. INICIANDO BUSCA!", controller.gameObject);

                controller.LastKnownPlayerPosition = investigatePosition;
                controller.ChangeState(new SearchState());
                return;
            }
            else
            {
                Vector3 lookDirection = (investigatePosition - controller.transform.position).normalized;
                lookDirection.y = 0;
                if (lookDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    controller.transform.rotation = Quaternion.Slerp(
                        controller.transform.rotation,
                        targetRotation,
                        Time.deltaTime * controller.investigateRotationSpeed
                    );
                }

                currentWaitTime += Time.deltaTime;
                if (currentWaitTime >= maxWaitTime)
                {
                    Debug.Log("INVESTIGATE: Tempo de observação do som esgotado. Passando para Busca (SearchState).", controller.gameObject);
                    controller.LastKnownPlayerPosition = investigatePosition;
                    controller.ChangeState(new SearchState());
                }
            }
        }
    }

    public override void Exit(AIController controller)
    {
        controller.Agent.isStopped = false;
        Debug.Log("<color=#66d9ef>INVESTIGATE:</color> Fim da investigação.", controller.gameObject);
    }
}