// Scripts/Monstro/States/SearchState.cs

using UnityEngine;
using UnityEngine.AI;

public class SearchState : BaseState
{
    private float searchTimer;
    private float timeToGiveUp = 7f;

    private enum SearchPhase
    {
        MovingToLastSeen,
        MovingToPrediction,
        SearchingOnSpot
    }
    private SearchPhase currentPhase;

    public override void Enter(AIController controller)
    {
        Debug.Log($"<color=yellow>SEARCH:</color> FASE 1: Indo para a última posição vista em {controller.LastKnownPlayerPosition}", controller.gameObject);

        currentPhase = SearchPhase.MovingToLastSeen;
        controller.Agent.speed = controller.searchSpeed;
        controller.Agent.SetDestination(controller.LastKnownPlayerPosition);

        searchTimer = 0f;
    }

    public override void Execute(AIController controller)
    {
        if (controller.Senses.CanSeePlayer(out Vector3 playerPos))
        {
            Debug.LogWarning("SEARCH: Jogador reencontrado durante a busca! Voltando para ChaseState.", controller.gameObject);
            controller.LastKnownPlayerPosition = playerPos;
            controller.ChangeState(new ChaseState());
            return;
        }

        switch (currentPhase)
        {
            case SearchPhase.MovingToLastSeen:
                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    Debug.Log("<color=yellow>SEARCH:</color> Chegou na última posição. FASE 2: Calculando e indo para a posição prevista.", controller.gameObject);

                    Vector3 lastPos = controller.LastKnownPlayerPosition;
                    Vector3 lastVel = controller.LastKnownPlayerVelocity;

                    // --- MUDANÇA CRÍTICA NA FÓRMULA DE PREVISÃO ---
                    // Posição Futura = Posição Atual + (Velocidade * Tempo)
                    // REMOVEMOS o '.normalized' para usar a velocidade real do jogador!
                    Vector3 predictedPosition = lastPos + lastVel * controller.predictionTime;
                    // --- FIM DA MUDANÇA ---

                    NavMesh.SamplePosition(predictedPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas);
                    Vector3 destination = hit.position;

                    Debug.Log($"<color=yellow>SEARCH:</color> Prevendo que o jogador estará em <color=cyan>{destination}</color> daqui a {controller.predictionTime}s", controller.gameObject);

                    controller.Agent.SetDestination(destination);
                    currentPhase = SearchPhase.MovingToPrediction;
                }
                break;

            case SearchPhase.MovingToPrediction:
                if (!controller.Agent.pathPending && controller.Agent.remainingDistance <= controller.Agent.stoppingDistance)
                {
                    Debug.Log("<color=yellow>SEARCH:</color> Chegou ao local previsto. FASE 3: Procurando na área.", controller.gameObject);
                    currentPhase = SearchPhase.SearchingOnSpot;
                }
                break;

            case SearchPhase.SearchingOnSpot:
                controller.transform.Rotate(0, 30f * Time.deltaTime, 0);
                searchTimer += Time.deltaTime;

                if (searchTimer > timeToGiveUp)
                {
                    Debug.Log("SEARCH: Tempo de busca esgotado. Desistindo e voltando a vagar.", controller.gameObject);
                    controller.ChangeState(new WanderState());
                }
                break;
        }
    }

    public override void Exit(AIController controller)
    {
    }
}