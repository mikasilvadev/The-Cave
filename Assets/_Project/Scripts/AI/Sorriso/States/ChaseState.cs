// Scripts/AI/Sorriso/States/ChaseState.cs

using UnityEngine;

public class ChaseState : BaseState
{
    private float catchDistance = 1.5f;

    public override void Enter(AIController controller)
    {
        Debug.Log("<color=red>CHASE:</color> Iniciando perseguição!", controller.gameObject);
        controller.Agent.speed = controller.chaseSpeed;

        // --- NOVO: Assumimos o controle da rotação ---
        // Desligamos a rotação automática do NavMeshAgent. Nós vamos controlá-la.
        controller.Agent.updateRotation = false;
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            // A lógica de movimento continua a mesma: vá para a posição do jogador.
            controller.Agent.SetDestination(playerPos);
            controller.LastKnownPlayerPosition = playerPos;
            controller.LastKnownPlayerVelocity = controller.PlayerController.CurrentVelocity;

            // --- LÓGICA DE ROTAÇÃO SUAVE ---
            // 1. Calcula a direção para o jogador, ignorando a altura (eixo Y).
            Vector3 directionToPlayer = (playerPos - controller.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));

            // 2. Aplica a rotação de forma suave ao longo do tempo.
            // O monstro vai virar gradualmente para encarar o jogador enquanto se move.
            controller.transform.rotation = Quaternion.Slerp(
                controller.transform.rotation,
                targetRotation,
                Time.deltaTime * controller.chaseRotationSpeed
            );
            // --- FIM DA LÓGICA DE ROTAÇÃO ---

            if (Vector3.Distance(controller.transform.position, playerPos) < catchDistance)
            {
                Debug.LogError("CHASE: O JOGADOR FOI PEGO!", controller.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("CHASE: Perdeu o jogador de vista. Mudando para SearchState.", controller.gameObject);
            controller.ChangeState(new SearchState());
        }
    }

    public override void Exit(AIController controller)
    {
        // --- NOVO: Devolvemos o controle da rotação ---
        // Ao sair do estado de perseguição, reativamos a rotação padrão do agente.
        controller.Agent.updateRotation = true;
    }
}