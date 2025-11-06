using UnityEngine;

public class DarkMonitoringState : IState
{
    private MonsterController controller;

    public DarkMonitoringState(MonsterController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        Debug.Log("MONSTRO: Entrando no estado DarkMonitoring (PARADO)");
        controller.Movement.StopMovement();
        controller.SetAnimSpeed(0f);
        controller.SetMovementAndAnimationSpeed(
            controller.Movement.monitoringSpeed,
            controller.animationSpeedMultiplier
        );
    }

    public void Execute()
    {
        if (controller.IsPlayerLightOn)
        {
            Debug.Log("MONSTRO: Player ligou a lanterna, voltando ao Chasing");
            controller.TransitionToState(StateType.Chasing);
        }
    }

    public void Exit()
    {
        Debug.Log("MONSTRO: Saindo do estado DarkMonitoring");
    }
}