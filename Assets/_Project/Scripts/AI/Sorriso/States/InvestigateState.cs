using UnityEngine;
using UnityEngine.AI;

public class InvestigateState : BaseState
{
    private Vector3 soundPosition;
    private bool arrivedAtSound = false;
    private float maxWaitTime = 5f;
    private float currentWaitTime = 0f;

    public InvestigateState(Vector3 position)
    {
        soundPosition = position;
    }

    public override void Enter(AIController controller)
    {
        controller.Agent.stoppingDistance = controller.soundStopDistance;

        Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Ouvido um som! Indo investigar a posição {soundPosition} (Parada a {controller.soundStopDistance}m)", controller.gameObject);

        arrivedAtSound = false;
        currentWaitTime = 0f;

        controller.Agent.updateRotation = true;
        controller.Agent.isStopped = false;

        controller.Agent.speed = controller.searchSpeed;
        controller.Agent.SetDestination(soundPosition);
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("INVESTIGATE: Jogador encontrado! Mudando para ChaseState.", controller.gameObject);

            controller.LastKnownPlayerPosition = playerPos;
            controller.ChangeState(new ChaseState());
            return;
        }

        if (!arrivedAtSound)
        {
            if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
            {
                Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Chegou ao local do som ({controller.soundStopDistance}m). Iniciando observação fixa.", controller.gameObject);

                arrivedAtSound = true;
                controller.Agent.isStopped = true;
                controller.Agent.updateRotation = false;
                currentWaitTime = 0f;
            }
        }

        if (arrivedAtSound)
        {
            Vector3 lookDirection = (soundPosition - controller.transform.position);
            lookDirection.y = 0;
            lookDirection.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                targetRotation,
                Time.deltaTime * controller.investigateRotationSpeed
            );

            if (controller.Senses.CheckForSound(out Vector3 newSoundPos))
            {
                if (Vector3.Distance(newSoundPos, soundPosition) > 1.5f)
                {
                    controller.ChangeState(new InvestigateState(newSoundPos));
                    return;
                }
                else
                {
                    currentWaitTime = 0f;
                }
            }

            currentWaitTime += Time.deltaTime;
            if (currentWaitTime >= maxWaitTime)
            {
                Debug.Log("INVESTIGATE: Tempo de observação esgotado. Passando para Busca (SearchState).", controller.gameObject);

                controller.LastKnownPlayerPosition = soundPosition;
                controller.ChangeState(new SearchState());
            }
        }
    }

    public override void Exit(AIController controller)
    {
        controller.Agent.stoppingDistance = 0.5f;
        controller.Agent.updateRotation = true;
        controller.Agent.isStopped = false;
        Debug.Log("<color=#66d9ef>INVESTIGATE:</color> Fim da investigação inicial.", controller.gameObject);
    }
}