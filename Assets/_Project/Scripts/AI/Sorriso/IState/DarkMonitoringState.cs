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
        else
        {
            if (controller.Player != null)
            {
                float distanceToPlayer = Vector3.Distance(controller.transform.position, controller.Player.position);

                if (distanceToPlayer <= 1.0f)
                {
                    Debug.Log("MONSTRO: Captura por proximidade no escuro! (Distância: " + distanceToPlayer + ")");
                    GameManager.Instance.TriggerGameOver();
                }
            }
        }
    }

    public void Exit()
    {
        Debug.Log("MONSTRO: Saindo do estado DarkMonitoring");
    }
}