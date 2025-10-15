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

    [Header("Configurações de Movimento")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 5f;
    public float searchSpeed = 3.5f;
    public float chaseRotationSpeed = 5f;

    public float investigateRotationSpeed = 3f;

    [Header("Configurações de Parada")]
    [Tooltip("Distância de parada no modo Chase ao perder a visão no escuro.")]
    public float darkStopDistance = 2.0f;

    [Tooltip("Distância de parada ao investigar a origem de um som.")]
    public float soundStopDistance = 1.0f;

    [Header("Velocidades da Animação")]
    public float wanderAnimSpeed = 1.0f;
    public float chaseAnimSpeed = 1.5f;
    public float searchAnimSpeed = 1.2f;

    [Header("Configurações de Fim de Jogo")]
    [Tooltip("Tempo que a tela fica preta/fadeout antes de recarregar a cena (simula cutscene).")]
    public float restartFadeTime = 2.0f;

    [Header("Configurações de Perseguição")]
    public float predictionDistance = 6f;

    [Tooltip("Quantos segundos no futuro o monstro deve 'prever' a posição do jogador.")]
    public float predictionTime = 1.5f;

    [Header("Estado Atual")]
    [SerializeField]
    private string currentStateName;

    private BaseState currentState;

    public Vector3 LastKnownPlayerVelocity { get; set; }
    public Vector3 LastKnownPlayerPosition { get; set; }
    public Vector3 LastHeardSoundPosition { get; set; }

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
        if (Senses.playerFlashlight != null && MonsterAudio != null)
        {
            if (Senses.playerFlashlight.enabled)
            {
                MonsterAudio.EnableSound();
            }
            else
            {
                MonsterAudio.DisableSound();
            }
        }

        if (!(currentState is InvestigateState))
        {
            if (Senses.CheckForSound(out Vector3 soundPos))
            {
                if (Vector3.Distance(transform.position, soundPos) > 1.5f)
                {
                    Debug.Log("AIController: OUVIU SOM! Mudando para InvestigateState.", this.gameObject);
                    LastHeardSoundPosition = soundPos;

                    ChangeState(new InvestigateState(soundPos));
                    return;
                }
            }
        }

        if (currentState != null)
        {
            currentState.Execute(this);
        }

        if (Agent.enabled && Animator != null)
        {
            float currentVelocity = Agent.velocity.magnitude;
            Animator.SetFloat("Speed", currentVelocity);
        }
    }

    public void ChangeState(BaseState newState)
    {
        Debug.Log($"<color=orange>STATE CHANGE:</color> De <color=yellow>{currentState?.GetType().Name ?? "NULL"}</color> para <color=green>{newState.GetType().Name}</color>", this.gameObject);

        if (currentState != null)
        {
            currentState.Exit(this);
        }

        currentState = newState;
        currentStateName = newState.GetType().Name;

        currentState.Enter(this);

        if (newState is WanderState)
        {
            Animator.speed = wanderAnimSpeed;
        }
        else if (newState is ChaseState)
        {
            Animator.speed = chaseAnimSpeed;
        }
        else if (newState is SearchState || newState is InvestigateState)
        {
            Animator.speed = searchAnimSpeed;
        }
    }

    public void GameOverAndRestart()
    {
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