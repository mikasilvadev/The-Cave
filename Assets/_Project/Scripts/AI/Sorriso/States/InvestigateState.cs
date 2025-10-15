// Scripts/Monstro/States/InvestigateState.cs

using UnityEngine;

public class InvestigateState : BaseState
{
    private Vector3 soundPosition;

    public InvestigateState(Vector3 position)
    {
        soundPosition = position;
    }

    public override void Enter(AIController controller)
    {
        Debug.Log($"<color=#66d9ef>INVESTIGATE:</color> Ouvido um som! Indo investigar a posição {soundPosition}", controller.gameObject);

        controller.Agent.speed = controller.searchSpeed;
        controller.Agent.SetDestination(soundPosition);
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("INVESTIGATE: Jogador encontrado durante a investigação! Mudando para ChaseState.", controller.gameObject);

            controller.LastKnownPlayerPosition = playerPos;
            controller.ChangeState(new ChaseState());
            return;
        }
        if (!controller.Agent.pathPending && controller.Agent.velocity.sqrMagnitude < 0.1f)
        {
            Debug.Log("INVESTIGATE: Chegou ao local do som, mas não viu nada. Começando a procurar na área (SearchState).", controller.gameObject);

            controller.LastKnownPlayerPosition = soundPosition;
            controller.ChangeState(new SearchState());
        }
    }

    public override void Exit(AIController controller)
    {
        Debug.Log("<color=#66d9ef>INVESTIGATE:</color> Fim da investigação inicial.", controller.gameObject);
    }
}