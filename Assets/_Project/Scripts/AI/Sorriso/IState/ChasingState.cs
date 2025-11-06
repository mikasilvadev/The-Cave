using UnityEngine;

public class ChasingState : IState
{
    private MonsterController controller;

    public ChasingState(MonsterController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        Debug.Log("MONSTRO: Entrando no estado Chasing");
        controller.SetMovementAndAnimationSpeed(
            controller.Movement.chasingSpeed,
            controller.chasingAnimMultiplier
        );
        controller.Movement.SetStoppingDistance(0f);
        controller.Movement.FollowTarget(controller.Player);
    }

    public void Execute()
    {
        if (controller.IsPlayerLightOn)
        {
            controller.Movement.FollowTarget(controller.Player);
        }
        else
        {
            Debug.Log("MONSTRO: Player desligou a lanterna. Entrando em DarkMonitoring");
            controller.TransitionToState(StateType.DarkMonitoring);
        }
    }

    public void Exit()
    {
        Debug.Log("MONSTRO: Saindo do estado Chasing");
        controller.Movement.StopMovement();
    }
}