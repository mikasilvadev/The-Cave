using UnityEngine;
using UnityEngine.AI;

public class ChaseState : BaseState
{
    private float stopDistance = 0.1f;
    private float pathUpdateInterval = 0.25f;
    private float pathUpdateTimer;
    private bool isLunging = false;

    public override void Enter(AIController controller)
    {
        Debug.Log("<color=red>CHASE:</color> Iniciando perseguição implacável!", controller.gameObject);
        controller.Agent.speed = controller.chaseSpeed;
        controller.Agent.stoppingDistance = stopDistance;

        isLunging = false;
        controller.Agent.updatePosition = true;
        controller.Agent.updateRotation = false;
        pathUpdateTimer = 0f;
    }

    public override void Execute(AIController controller)
    {
        Vector3 soundPos;
        bool heardSound = controller.Senses.CheckForSound(out soundPos);

        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            float distanceToPlayer = Vector3.Distance(controller.transform.position, playerPos);
            Vector3 directionToPlayer = (playerPos - controller.transform.position).normalized;

            if (distanceToPlayer < controller.lungeDistance)
            {
                if (!isLunging)
                {
                    isLunging = true;
                    Debug.LogWarning("CHASE: Distância crítica! MODO DE ATAQUE COM FORÇA BRUTA!", controller.gameObject);
                    controller.Agent.updatePosition = false;
                }

                controller.transform.position += directionToPlayer * controller.lungeSpeed * Time.deltaTime;
            }
            else
            {
                if (!controller.Agent.updatePosition) controller.Agent.updatePosition = true;

                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    controller.Agent.SetDestination(playerPos);
                    pathUpdateTimer = pathUpdateInterval;
                }
            }

            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                targetRotation,
                Time.deltaTime * controller.chaseRotationSpeed
            );

            controller.LastKnownPlayerPosition = playerPos;
            controller.LastKnownPlayerVelocity = controller.PlayerController.CurrentVelocity;
        }
        else
        {
            if (!controller.Senses.playerFlashlight.enabled)
            {
                Debug.LogWarning("CHASE: Perdeu o jogador de vista porque a luz apagou. Mudando para RetreatState.", controller.gameObject);
                controller.ChangeState(new RetreatState());
                return;
            }

            if (heardSound)
            {
                Debug.Log("CHASE: Perdeu de vista, mas ouviu som. Investigando o som.", controller.gameObject);
                controller.ChangeState(new InvestigateState(soundPos, "som no escuro"));
            }
            else
            {
                Debug.LogWarning("CHASE: Perdeu o jogador de vista (bloqueio). Mudando para SearchState.", controller.gameObject);
                controller.ChangeState(new SearchState());
            }
        }
    }

    public override void Exit(AIController controller)
    {
        controller.Agent.updatePosition = true;
        controller.Agent.updateRotation = true;
        Debug.Log("<color=red>CHASE:</color> Fim da perseguição.", controller.gameObject);
    }
}