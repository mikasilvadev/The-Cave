using UnityEngine;

public class ChaseState : BaseState
{
    private float catchDistance = 0.5f;
    private float pathUpdateInterval = 0.25f;
    private float pathUpdateTimer;

    public override void Enter(AIController controller)
    {
        Debug.Log("<color=red>CHASE:</color> Iniciando perseguição!", controller.gameObject);
        controller.Agent.speed = controller.chaseSpeed;
        controller.Agent.stoppingDistance = catchDistance;
        controller.Agent.updateRotation = false;

        pathUpdateTimer = 0f;
    }

    public override void Execute(AIController controller)
    {
        Vector3 soundPos = Vector3.zero;
        bool heardSound = controller.Senses.CheckForSound(out soundPos);

        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                controller.Agent.SetDestination(playerPos);
                pathUpdateTimer = pathUpdateInterval;
            }

            controller.LastKnownPlayerPosition = playerPos;
            controller.LastKnownPlayerVelocity = controller.PlayerController.CurrentVelocity;

            Vector3 directionToPlayer = (playerPos - controller.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                targetRotation,
                Time.deltaTime * controller.chaseRotationSpeed
            );

            if (Vector3.Distance(controller.transform.position, playerPos) < catchDistance)
            {
                Debug.LogError("CHASE: O JOGADOR FOI PEGO! REINICIANDO O JOGO.", controller.gameObject);
                controller.Agent.isStopped = true;
                controller.gameObject.SetActive(false);
                controller.GameOverAndRestart();
                return;
            }
        }
        else
        {
            bool playerMovingInDark = !controller.Senses.playerFlashlight.enabled && heardSound;

            if (playerMovingInDark)
            {
                controller.Agent.stoppingDistance = controller.darkStopDistance;
                controller.Agent.SetDestination(controller.LastKnownPlayerPosition);

                Debug.Log($"CHASE: Jogador no escuro! Indo para {controller.darkStopDistance}m da última posição vista, ouvindo.", controller.gameObject);

                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    Debug.Log("CHASE: Chegou à distância de parada. Mudando para InvestigateState para seguir o som.", controller.gameObject);
                    controller.ChangeState(new InvestigateState(soundPos, "som no escuro"));
                }
            }
            else
            {
                Debug.LogWarning("CHASE: Perdeu o jogador de vista (bloqueio ou jogador parado). Mudando para SearchState.", controller.gameObject);
                controller.ChangeState(new SearchState());
            }
        }
    }

    public override void Exit(AIController controller)
    {
        controller.Agent.updateRotation = true;
        controller.Agent.stoppingDistance = 0.5f;
        Debug.Log("<color=red>CHASE:</color> Fim da perseguição.", controller.gameObject);
    }
}