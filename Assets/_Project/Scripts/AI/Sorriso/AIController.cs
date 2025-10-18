using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MonsterSenses))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(MonsterAudio))]
public class AIController : MonoBehaviour
{
    [Header("Componentes")]
    public NavMeshAgent Agent { get; private set; }
    public MonsterSenses Senses { get; private set; }
    public Transform Player { get; private set; }
    public PlayerController PlayerController { get; private set; }
    public Animator Animator { get; private set; }
    public MonsterAudio MonsterAudio { get; private set; }

    [Header("Componentes de UI")]
    public ScreenFader screenFader;

    [Header("Configurações de Movimento")]
    public float wanderSpeed = 2f;
    public float minWanderIdleTime = 3.0f;
    public float maxWanderIdleTime = 7.0f;
    public float chaseSpeed = 7f;
    public float searchSpeed = 3.5f;
    public float chaseRotationSpeed = 10f;
    public float investigateRotationSpeed = 3f;

    [Header("Configurações de Parada")]
    public float darkStopDistance = 2.0f;
    public float soundStopDistance = 1.0f;

    [Header("Configurações da Investida (Lunge)")]
    public float lungeDistance = 3f;
    public float lungeSpeed = 12f;
    public float lungeAcceleration = 30f;

    [Header("Configurações de Animação")]
    [Tooltip("Multiplicador geral da velocidade da animação para ajustar o visual da corrida.")]
    public float animationSpeedMultiplier = 1.2f;

    [Header("Configurações de Fim de Jogo")]
    public float restartFadeTime = 4.0f;

    [Header("Configurações de Perseguição")]
    public float predictionDistance = 6f;
    [Tooltip("Quantos segundos no futuro o monstro deve 'prever' a posição do jogador.")]
    public float predictionTime = 1.5f;

    [Header("Estado Atual")]
    [SerializeField]
    private string currentStateName;

    private BaseState currentState;
    private bool isGameOver = false;
    public Vector3 LastKnownPlayerVelocity { get; set; }
    public Vector3 LastKnownPlayerPosition { get; set; }

    void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Senses = GetComponent<MonsterSenses>();
        Animator = GetComponent<Animator>();
        MonsterAudio = GetComponent<MonsterAudio>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        Player = playerObject.transform;
        PlayerController = playerObject.GetComponent<PlayerController>();
    }

    void Start()
    {
        ChangeState(new WanderState());
    }

    void Update()
    {
        if (isGameOver)
        {
            return;
        }

        if (!Senses.playerFlashlight.enabled)
        {
            if (Agent.enabled && !Agent.isStopped)
            {
                Agent.isStopped = true;
                Animator.SetFloat("Speed", 0);
                Animator.speed = 1f;
            }
            return;
        }
        else
        {
            if (Agent.enabled && Agent.isStopped)
            {
                Agent.isStopped = false;
            }
        }

        if (Senses.IsPlayerShiningLightOnMe(out Vector3 playerPos))
        {
            Debug.LogWarning("AIController: FUI ILUMINADO DIRETAMENTE! MUDANDO PARA CHASESTATE!", this.gameObject);
            ChangeState(new ChaseState());
            return;
        }

        if (!(currentState is InvestigateState) && !(currentState is ChaseState))
        {
            if (Senses.CheckForSound(out Vector3 soundPos))
            {
                if (Vector3.Distance(transform.position, soundPos) > 1.5f)
                {
                    Debug.Log("AIController: OUVIU SOM! Mudando para InvestigateState.", this.gameObject);
                    ChangeState(new InvestigateState(soundPos, "som"));
                    return;
                }
            }
        }

        if (currentState is WanderState || currentState is SearchState)
        {
            if (Senses.CanSeeFlashlightBeam(out Vector3 lightPos))
            {
                Debug.Log("AIController: VIU O FOCO DE LUZ! Mudando para InvestigateState.", this.gameObject);
                ChangeState(new InvestigateState(lightPos, "foco de luz"));
                return;
            }
        }

        if (currentState != null)
        {
            currentState.Execute(this);
        }
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (Agent.enabled && Animator != null)
        {
            float worldSpeed = Agent.velocity.magnitude;
            float maxSpeed = Agent.speed;
            float normalizedSpeed = maxSpeed > 0 ? worldSpeed / maxSpeed : 0;
            Animator.SetFloat("Speed", normalizedSpeed);
            Animator.speed = (worldSpeed > 0.1f) ? animationSpeedMultiplier : 1f;
        }
    }

    public void ChangeState(BaseState newState)
    {
        Debug.Log($"<color=orange>STATE CHANGE:</color> De <color=yellow>{currentState?.GetType().Name ?? "NULL"}</color> para <color=green>{newState.GetType().Name}</color>", this.gameObject);
        if (currentState != null) currentState.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    public void GameOverAndRestart()
    {
        isGameOver = true;
        Debug.Log("Iniciando GameOverAndRestart...");
        if (screenFader != null)
        {
            screenFader.FadeToBlackInstant();
        }
        MonsterAudio.StopAllSounds();
        Animator.enabled = false;
        if (Agent.enabled)
        {
            Agent.isStopped = true;
            Agent.enabled = false;
        }
        if (PlayerController != null)
        {
            PlayerController.podeMover = false;
        }
        StartCoroutine(FadeAndReloadScene(restartFadeTime));
    }

    private IEnumerator FadeAndReloadScene(float delay)
    {
        Debug.Log($"GAME OVER! Iniciando fade ({delay}s) antes de recarregar.");
        yield return new WaitForSeconds(delay);
        string sceneToLoad = SceneManager.GetActiveScene().name;
        Debug.Log($"Recarregando a cena: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}