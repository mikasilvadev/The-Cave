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
            Debug.Log("<color=#66d9ef>INVESTIGATE:</color> Distância de parada definida para 0.1m (investigando luz).", controller.gameObject);
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
        controller.Agent.speed = controller.searchSpeed;
        controller.Agent.SetDestination(investigatePosition);
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("INVESTIGATE: Jogador encontrado durante investigação! Mudando para ChaseState.", controller.gameObject);
            controller.LastKnownPlayerPosition = playerPos;
            controller.ChangeState(new ChaseState());
            return;
        }

        if (!arrivedAtTarget)
        {
            if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
            {
                Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Chegou ao local do {reason}. Iniciando observação.", controller.gameObject);
                arrivedAtTarget = true;
                controller.Agent.isStopped = true;
                controller.Agent.updateRotation = false;
                currentWaitTime = 0f;
            }
        }

        if (arrivedAtTarget)
        {
            if (reason == "foco de luz")
            {
                if (LookForLightSource(controller))
                {
                    return;
                }
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
            }

            if (controller.Senses.CheckForSound(out Vector3 newSoundPos))
            {
                if (Vector3.Distance(newSoundPos, investigatePosition) > 1.5f)
                {
                    controller.ChangeState(new InvestigateState(newSoundPos, "novo som"));
                    return;
                }
                else { currentWaitTime = 0f; }
            }

            currentWaitTime += Time.deltaTime;
            if (currentWaitTime >= maxWaitTime)
            {
                Debug.Log($"INVESTIGATE: Tempo de observação do {reason} esgotado. Passando para Busca (SearchState).", controller.gameObject);
                controller.LastKnownPlayerPosition = investigatePosition;
                controller.ChangeState(new SearchState());
            }
        }
    }

    private bool LookForLightSource(AIController controller)
    {
        Transform lightSource = controller.Senses.playerFlashlight.transform;
        Vector3 directionToSource = (lightSource.position - controller.transform.position).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(directionToSource);
        controller.transform.rotation = Quaternion.Slerp(
            controller.transform.rotation,
            targetRotation,
            Time.deltaTime * controller.investigateRotationSpeed
        );

        if (Physics.Raycast(controller.transform.position, directionToSource, out RaycastHit hit, controller.Senses.visionDistance))
        {
            if (hit.collider.CompareTag("Player"))
            {
                Debug.LogWarning("<color=cyan>INVESTIGATE:</color> FONTE DA LUZ ENCONTRADA! É O JOGADOR! Perseguindo!", controller.gameObject);
                controller.LastKnownPlayerPosition = hit.point;
                controller.ChangeState(new ChaseState());
                return true;
            }
        }
        return false;
    }

    public override void Exit(AIController controller)
    {
        controller.Agent.stoppingDistance = 0.5f;
        controller.Agent.updateRotation = true;
        controller.Agent.isStopped = false;
        Debug.Log("<color=#66d9ef>INVESTIGATE:</color> Fim da investigação inicial.", controller.gameObject);
    }
}