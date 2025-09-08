using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum AIState { Patrolling, Wandering, Chasing, Searching, Idle }
public enum SearchPhase { PursuingProjection, InvestigatingHidingSpots, ScanningPerimeter }
public struct PlayerObservation { public Vector3 position; public Vector3 velocity; public float timestamp; }

public class MonstroAI : MonoBehaviour
{
    #region Variáveis Públicas (O Painel de Controle da IA)
    [Header("Referências")]
    [Tooltip("O alvo principal. Define quem a IA irá caçar.")]
    public Transform playerTransform;
    [Tooltip("O 'ponto de vista' da IA. É a origem do seu cone de visão.")]
    public Transform eyeTransform;

    [Header("Configurações de Movimento")]
    [Tooltip("Velocidade durante o AIState 'Patrolling'.")]
    public float patrolSpeed = 4f;
    [Tooltip("Velocidade durante o AIState 'Wandering'.")]
    public float wanderSpeed = 4f;
    [Tooltip("Velocidade durante o AIState 'Chasing'.")]
    public float chaseSpeed = 9f;
    [Tooltip("A agilidade de rotação do monstro (padrão).")]
    [Range(1, 100)] public float turningSensitivity = 50f;

    [Tooltip("Distância em que a IA ativa a 'fixação' reativa, controlando a rotação manualmente.")]
    public float fixationDistance = 5f;
    [Tooltip("A velocidade com que o corpo da IA gira para encarar o jogador no modo de fixação.")]
    public float fixationTurnSpeed = 10f;

    [Header("Configurações de Visão")]
    [Tooltip("O alcance máximo da visão para iniciar o Chasing.")]
    public float sightDistance = 20f;
    [Tooltip("A largura do cone de visão.")]
    public float fieldOfViewAngle = 110f;
    [Tooltip("Define quais objetos (como paredes) bloqueiam a linha de visão.")]
    public LayerMask detectionLayers;

    [Header("Configurações de Audição")]
    [Tooltip("O raio de alcance da audição.")]
    public float hearingDistance = 25f;
    [Tooltip("O gatilho sonoro. Velocidade mínima do jogador para ser ouvido.")]
    public float playerRunningSpeedThreshold = 5f;

    [Header("Comportamento e Timers")]
    [Tooltip("O quão longe a IA explora durante o estado Wandering.")]
    public float wanderRadius = 10f;
    [Tooltip("Tempo de espera em cada ponto de patrulha ou após vaguear.")]
    public float waitTimeAtDestination = 2f;
    [Tooltip("A 'memória imediata' do monstro antes de iniciar o Searching.")]
    public float chaseGracePeriod = 0.5f;
    [Tooltip("Número máximo de 'pontos quentes' que a IA pode memorizar.")]
    public int learnedHotSpotsMemory = 5;

    [Header("Configurações da Busca Inteligente")]
    [Tooltip("Controla o quão 'ousada' é a previsão na fase PursuingProjection.")]
    public float pathProjectionTime = 2.0f;
    [Tooltip("A 'área de interesse' para a fase InvestigatingHidingSpots.")]
    public float hidingSpotSearchRadius = 15f;
    
    [Header("Configurações de Patrulha")]
    [Tooltip("A rota do monstro durante o estado Patrolling. Se vazia, usa Wandering.")]
    public List<Transform> patrolPoints;
    #endregion

    #region Variáveis Privadas (O Cérebro da IA)
    private AIState currentState;
    private NavMeshAgent agent;
    private HeadLookController headController;
    private float loseSightTimer;
    private Vector3 lastKnownPlayerPosition;
    private Queue<Vector3> investigationQueue;
    private List<PlayerObservation> playerMemory;
    private SearchPhase currentSearchPhase;
    private List<Vector3> learnedHotSpots;
    private int currentPatrolIndex = 0;
    private const int memorySize = 10;
    private float memoryTimer = 0f;
    private const float memoryCaptureInterval = 0.1f;
    private Vector3 projectedPathEnd;
    private Vector3 lastPlayerPositionForHearing;
    private float playerSpeed;
    private Coroutine waitCoroutine;
    #endregion

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        headController = GetComponentInChildren<HeadLookController>();
        
        investigationQueue = new Queue<Vector3>();
        playerMemory = new List<PlayerObservation>();
        learnedHotSpots = new List<Vector3>();

        agent.angularSpeed = turningSensitivity * 20f;
        if (headController != null) { headController.lookSpeed = turningSensitivity / 5f; }
        
        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) { 
                playerTransform = playerObject.transform;
                lastPlayerPositionForHearing = playerTransform.position;
            } else { Debug.LogError("ERRO: Player não encontrado. Certifique-se de que o jogador tem a tag 'Player'."); }
        }
        
        if (eyeTransform == null) { eyeTransform = this.transform; }

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            TransitionToState(AIState.Patrolling);
        }
        else
        {
            TransitionToState(AIState.Wandering);
        }
    }

    void Update()
    {
        bool playerIsVisible = IsPlayerVisible();
        HandleHearing();

        if (playerIsVisible)
        {
            if (currentState == AIState.Searching)
            {
                Debug.LogWarning("IA REENCONTROU O JOGADOR DURANTE A BUSCA! Aprendendo Ponto Quente Tático em: " + playerTransform.position);
                LearnHotSpot(playerTransform.position);
            }
            TransitionToState(AIState.Chasing);
        }

        if (currentState == AIState.Idle) return;

        // --- CORREÇÃO: O 'switch' foi movido para cá, para dentro do Update ---
        switch (currentState)
        {
            case AIState.Patrolling: 
                HandlePatrolling(playerIsVisible); 
                break;
            case AIState.Wandering: 
                HandleWandering(playerIsVisible); 
                break;
            case AIState.Chasing: 
                HandleChasing(playerIsVisible); 
                break;
            case AIState.Searching: 
                HandleSearching(playerIsVisible); 
                break;
        }
    }

    #region Máquina de Estados e Handlers
    private void TransitionToState(AIState newState)
    {
        if (currentState == newState) return;

        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);
            waitCoroutine = null;
        }

        Debug.Log($"Monstro mudou de estado: {currentState} -> {newState}");

        currentState = newState;
        OnStateEnter();
    }

    private void OnStateEnter()
    {
        if (currentState != AIState.Chasing) { playerMemory.Clear(); }
        switch (currentState)
        {
            case AIState.Patrolling:
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                headController?.StopTracking();
                if (patrolPoints != null && patrolPoints.Count > 0)
                {
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                break;
            case AIState.Wandering:
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.speed = wanderSpeed;
                headController?.StopTracking();
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                agent.SetDestination(newPos);
                break;
            case AIState.Chasing:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                headController?.StartTracking(playerTransform);
                loseSightTimer = 0f;
                if (playerTransform != null) agent.SetDestination(playerTransform.position);
                break;
            case AIState.Searching:
                agent.updateRotation = true;
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                headController?.LookAtPosition(lastKnownPlayerPosition);
                currentSearchPhase = SearchPhase.PursuingProjection;
                HandleSearching(false);
                break;
            case AIState.Idle:
                agent.isStopped = true;
                break;
        }
    }

    private void HandlePatrolling(bool playerIsVisible)
    {
        if (playerIsVisible) { TransitionToState(AIState.Chasing); return; }
        if (HasReachedDestination())
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            waitCoroutine = StartCoroutine(WaitAndSetDestination(patrolPoints[currentPatrolIndex].position, AIState.Patrolling));
        }
    }
    
    private void HandleWandering(bool playerIsVisible)
    {
        if (playerIsVisible) { TransitionToState(AIState.Chasing); return; }
        if (HasReachedDestination())
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            waitCoroutine = StartCoroutine(WaitAndSetDestination(newPos, AIState.Wandering));
        }
    }

    private void HandleChasing(bool playerIsVisible)
    {
        if (playerIsVisible)
        {
            loseSightTimer = 0f;
            lastKnownPlayerPosition = playerTransform.position;
            agent.SetDestination(lastKnownPlayerPosition);

            float distanceToPlayer = Vector3.Distance(transform.position, lastKnownPlayerPosition);

            if (distanceToPlayer <= fixationDistance)
            {
                agent.updateRotation = false;

                Vector3 directionToPlayer = lastKnownPlayerPosition - transform.position;
                directionToPlayer.y = 0;
                if(directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, fixationTurnSpeed * Time.deltaTime);
                }
            }
            else
            {
                agent.updateRotation = true;
            }

            memoryTimer += Time.deltaTime;
            if (memoryTimer >= memoryCaptureInterval)
            {
                memoryTimer = 0f;
                RecordPlayerObservation();
            }
        }
        else
        {
            agent.updateRotation = true;
            loseSightTimer += Time.deltaTime;
            if (loseSightTimer >= chaseGracePeriod)
            {
                TransitionToState(AIState.Searching);
            }
        }
    }

    private void HandleSearching(bool playerIsVisible)
    {
        if (playerIsVisible) { TransitionToState(AIState.Chasing); return; }
        
        if (HasReachedDestination() && investigationQueue.Count == 0)
        {
            switch (currentSearchPhase)
            {
                case SearchPhase.PursuingProjection:
                    currentSearchPhase = SearchPhase.InvestigatingHidingSpots;
                    FindAndQueueHidingSpots();
                    break;
                case SearchPhase.InvestigatingHidingSpots:
                    currentSearchPhase = SearchPhase.ScanningPerimeter;
                    QueueRandomPointsInPerimeter();
                    break;
                case SearchPhase.ScanningPerimeter:
                    if (patrolPoints != null && patrolPoints.Count > 0) TransitionToState(AIState.Patrolling);
                    else TransitionToState(AIState.Wandering);
                    return;
            }
        }

        if (HasReachedDestination() && investigationQueue.Count > 0)
        {
            agent.SetDestination(investigationQueue.Dequeue());
        }
    }
    #endregion

    #region Lógica de Percepção e Busca
    private void FindAndQueueHidingSpots()
    {
        investigationQueue.Clear();
        var nearbyHotSpots = learnedHotSpots
            .Where(spot => Vector3.Distance(lastKnownPlayerPosition, spot) < hidingSpotSearchRadius)
            .OrderBy(spot => Vector3.Distance(lastKnownPlayerPosition, spot))
            .ToList();
        
        foreach (var point in nearbyHotSpots)
        {
            investigationQueue.Enqueue(point);
        }

        if (investigationQueue.Count > 0)
        {
            agent.SetDestination(investigationQueue.Dequeue());
        }
        else
        {
            currentSearchPhase = SearchPhase.ScanningPerimeter;
            QueueRandomPointsInPerimeter();
        }
    }
    
    private void QueueRandomPointsInPerimeter()
    {
        investigationQueue.Clear();
        for (int i = 0; i < 4; i++)
        {
            Vector3 randomPoint = RandomNavSphere(lastKnownPlayerPosition, hidingSpotSearchRadius, -1);
            investigationQueue.Enqueue(randomPoint);
        }
        if (investigationQueue.Count > 0)
        {
            agent.SetDestination(investigationQueue.Dequeue());
        }
    }

    private void HandleHearing()
    {
        if (playerTransform == null) return;
        
        if (Time.deltaTime > 0)
        {
            playerSpeed = Vector3.Distance(lastPlayerPositionForHearing, playerTransform.position) / Time.deltaTime;
        }
        lastPlayerPositionForHearing = playerTransform.position;

        if (playerSpeed > playerRunningSpeedThreshold && Vector3.Distance(transform.position, playerTransform.position) < hearingDistance)
        {
            if (currentState != AIState.Chasing && currentState != AIState.Searching)
            {
                Debug.Log("Monstro OUVIU seus passos! Iniciando busca...");
                lastKnownPlayerPosition = playerTransform.position;
                TransitionToState(AIState.Searching);
            }
        }
    }

    private void RecordPlayerObservation()
    {
        if (playerTransform == null) return;
        Vector3 currentVelocity = Vector3.zero;
        if (playerMemory.Count > 0)
        {
            var lastObservation = playerMemory.Last();
            float timeDiff = Time.time - lastObservation.timestamp;
            if (timeDiff > 0) { currentVelocity = (playerTransform.position - lastObservation.position) / timeDiff; }
        }
        playerMemory.Add(new PlayerObservation { position = playerTransform.position, velocity = currentVelocity, timestamp = Time.time });
        if (playerMemory.Count > memorySize) { playerMemory.RemoveAt(0); }
    }

    private bool IsPlayerVisible()
    {
        if (playerTransform == null) return false;
        Vector3 directionToPlayer = playerTransform.position - eyeTransform.position;
        if (directionToPlayer.magnitude > sightDistance) return false;
        if (Vector3.Angle(eyeTransform.forward, directionToPlayer.normalized) > fieldOfViewAngle / 2) return false;
        if (Physics.Raycast(eyeTransform.position, directionToPlayer.normalized, out RaycastHit hit, sightDistance, detectionLayers))
        {
            return hit.transform.CompareTag("Player");
        }
        return false;
    }

    private void LearnHotSpot(Vector3 spotPosition)
    {
        //Debug.Log("IA aprendeu um novo Ponto Quente em: " + spotPosition);
        learnedHotSpots.Add(spotPosition);
        while (learnedHotSpots.Count > learnedHotSpotsMemory)
        {
            learnedHotSpots.RemoveAt(0);
        }
    }
    #endregion

    #region Ferramentas de Depuração e Auxiliares
    private bool HasReachedDestination() { return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !agent.isStopped; }
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) { NavMesh.SamplePosition(origin + Random.insideUnitSphere * dist, out NavMeshHit navHit, dist, layermask); return navHit.position; }

    private IEnumerator WaitAndSetDestination(Vector3 destination, AIState nextState)
    {
        TransitionToState(AIState.Idle);
        yield return new WaitForSeconds(waitTimeAtDestination);
        TransitionToState(nextState);
        agent.SetDestination(destination);
    }

    void OnDrawGizmosSelected()
    {
        if (eyeTransform == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(eyeTransform.position, sightDistance);
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * eyeTransform.forward * sightDistance;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * eyeTransform.forward * sightDistance;
        Gizmos.DrawRay(eyeTransform.position, fovLine1);
        Gizmos.DrawRay(eyeTransform.position, fovLine2);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingDistance);

        if(currentState == AIState.Chasing)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.75f);
            Gizmos.DrawWireSphere(transform.position, fixationDistance);
        }

        if (currentState == AIState.Searching)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, hidingSpotSearchRadius);
            Gizmos.color = Color.magenta;
            if(learnedHotSpots != null)
            {
                foreach(var spot in learnedHotSpots) { Gizmos.DrawSphere(spot, 0.75f); }
            }
            if (investigationQueue != null && investigationQueue.Count > 0)
            {
                Gizmos.color = Color.cyan;
                Vector3 lastPoint = agent.transform.position;
                foreach (var point in investigationQueue)
                {
                    Gizmos.DrawSphere(point, 0.5f);
                    Gizmos.DrawLine(lastPoint, point);
                    lastPoint = point;
                }
            }
        }
    }
    #endregion
}