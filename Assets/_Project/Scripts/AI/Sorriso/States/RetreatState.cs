using UnityEngine;
using UnityEngine.AI;

public class RetreatState : BaseState
{
    private float retreatDistance = 7f;

    public override void Enter(AIController controller)
    {
        Debug.Log("<color=purple>RETREAT:</color> Jogador apagou a luz. Calculando ponto de recuo tático...", controller.gameObject);

        controller.Agent.speed = controller.searchSpeed;
        controller.Agent.autoBraking = true;

        Vector3 retreatPoint = FindBestRetreatPoint(controller);

        if (retreatPoint == Vector3.zero)
        {
            Debug.LogWarning("<color=purple>RETREAT:</color> Não foi possível encontrar um ponto de recuo seguro. Iniciando busca imediatamente.", controller.gameObject);
            controller.ChangeState(new SearchState());
            return;
        }

        Debug.Log($"<color=purple>RETREAT:</color> Recuando para o ponto {retreatPoint}", controller.gameObject);
        controller.Agent.SetDestination(retreatPoint);
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.playerFlashlight.enabled && controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            controller.ChangeState(new ChaseState());
            return;
        }

        if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= 1.0f)
        {
            controller.ChangeState(new SearchState());
        }
    }

    private Vector3 FindBestRetreatPoint(AIController controller)
    {
        Vector3 bestPoint = Vector3.zero;
        float maxDistanceToPlayer = 0f;

        for (int i = 0; i < 3; i++)
        {
            Vector3 directionAwayFromPlayer = (controller.transform.position - controller.Player.position).normalized;

            if (i == 1) directionAwayFromPlayer = Quaternion.Euler(0, 45, 0) * directionAwayFromPlayer; // Trás-direita
            if (i == 2) directionAwayFromPlayer = Quaternion.Euler(0, -45, 0) * directionAwayFromPlayer; // Trás-esquerda

            Vector3 targetPoint = controller.transform.position + directionAwayFromPlayer * retreatDistance;

            if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, controller.Player.position) > retreatDistance - 1f)
                {
                    if (Vector3.Distance(hit.position, controller.Player.position) > maxDistanceToPlayer)
                    {
                        maxDistanceToPlayer = Vector3.Distance(hit.position, controller.Player.position);
                        bestPoint = hit.position;
                    }
                }
            }
        }
        return bestPoint;
    }

    public override void Exit(AIController controller) { }
}