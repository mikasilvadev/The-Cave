// Scripts/AI/Sorriso/States/WanderState.cs

using UnityEngine;
using UnityEngine.AI;

public class WanderState : BaseState
{
    private float wanderRadius = 20f;
    private float wanderTimer = 5f;
    private float timer;

    public override void Enter(AIController controller)
    {
        Debug.Log("WANDER: Iniciando estado de vagar.", controller.gameObject);

        controller.Agent.speed = controller.wanderSpeed;
        timer = wanderTimer;
        SetRandomDestination(controller);
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("WANDER: Jogador detectado! Mudando para ChaseState.", controller.gameObject);

            controller.LastKnownPlayerPosition = playerPos;
            controller.ChangeState(new ChaseState());
            return;
        }

        timer += Time.deltaTime;

        if (timer >= wanderTimer || controller.Agent.remainingDistance < 1f)
        {
            SetRandomDestination(controller);
            timer = 0;
        }
    }

    public override void Exit(AIController controller)
    {
        controller.Agent.ResetPath();
    }

    private void SetRandomDestination(AIController controller)
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += controller.transform.position;
        NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas);
        controller.Agent.SetDestination(hit.position);

        Debug.Log($"WANDER: Novo destino aleatório definido para {hit.position}", controller.gameObject);
    }
}