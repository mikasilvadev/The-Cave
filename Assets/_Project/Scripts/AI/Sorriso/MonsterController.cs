using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AIMovement))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class MonsterController : MonoBehaviour
{
    public AIMovement Movement { get; private set; }
    public Transform Player { get; private set; }
    public Animator Animator { get; private set; }
    public int AnimSpeedID { get; private set; }
    public bool IsInChasingState { get; private set; } = false;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    private Light playerHeldLight;
    public bool IsPlayerLightOn { get; private set; }

    private AudioSource audioSource;
    [Header("Sons da Animação")]
    public AudioClip[] footstepSounds;
    private int nextFootstepIndex = 0;

    private IState currentState;
    private Dictionary<StateType, IState> states = new Dictionary<StateType, IState>();

    [Header("Configurações de Animação")]
    public float animationSpeedMultiplier = 1.0f;
    public float chasingAnimMultiplier = 2.0f;

    private bool isActivated = false;
    private bool isFrozen = false;

    [Header("Configurações de Agilidade")]
    [Tooltip("Velocidade (em graus/s) que o monstro gira para ENCARAR o player")]
    public float fixedTurnSpeed = 720f;



    void Awake()
    {
        Movement = GetComponent<AIMovement>();
        Animator = GetComponentInChildren<Animator>();
        AnimSpeedID = Animator.StringToHash("Speed");
        audioSource = GetComponent<AudioSource>();

        if (Animator == null)
            Debug.LogError("MonsterController: Animator não encontrado", gameObject);
        if (audioSource == null)
            Debug.LogError("MonsterController: AudioSource não encontrado", gameObject);

        if (playerTransform == null)
            Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        else
            Player = playerTransform;

        if (Player == null)
            Debug.LogError("MonsterController: Player não encontrado", gameObject);
        else
        {
            var playerController = Player.GetComponent<PlayerController>();
            if (playerController != null && playerController.heldFlashlightObject != null)
                playerHeldLight = playerController.heldFlashlightObject.GetComponentInChildren<Light>();
            if (playerHeldLight == null)
                Debug.LogWarning("MonsterController: Lanterna do Player ainda não encontrada no Awake");
        }
        states.Add(StateType.Chasing, new ChasingState(this));
        states.Add(StateType.DarkMonitoring, new DarkMonitoringState(this));
        GameManager.OnGameOver += HandleGameOver;
    }

    void Start()
    {
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }

        TransitionToState(StateType.DarkMonitoring);
    }

    void Update()
    {
        if (playerHeldLight == null && Player != null)
        {
            var playerController = Player.GetComponent<PlayerController>();
            if (playerController != null && playerController.heldFlashlightObject != null)
            {
                playerHeldLight = playerController.heldFlashlightObject.GetComponentInChildren<Light>();
            }
        }

        if (playerHeldLight != null)
            IsPlayerLightOn = playerHeldLight.enabled && playerHeldLight.gameObject.activeInHierarchy;
        else
            IsPlayerLightOn = false;
        bool shouldExecuteState = isActivated;
        if (isActivated && Player != null)
        {
            Vector3 lookDirection = Player.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    fixedTurnSpeed * Time.deltaTime
                );
            }
        }
        if (currentState != null && shouldExecuteState)
        {
            currentState.Execute();
        }
        if (isActivated && Movement.Agent != null)
        {
            float currentAgentSpeed = Movement.Agent.velocity.magnitude;
            float maxAgentSpeed = Movement.Agent.speed;
            float normalizedSpeed = (maxAgentSpeed > 0) ? (currentAgentSpeed / maxAgentSpeed) : 0f;
            SetAnimSpeed(normalizedSpeed);
        }
        else if (!isActivated)
        {
            SetAnimSpeed(0);
        }
    }

    public void TransitionToState(StateType newState)
    {
        if (currentState != null)
            currentState.Exit();
        if (states.ContainsKey(newState))
        {
            currentState = states[newState];
            IsInChasingState = (newState == StateType.Chasing);
            Animator.speed = animationSpeedMultiplier;
            currentState.Enter();
        }
        else
        {
            Debug.LogError($"MonsterController: Tentou transicionar para um estado inválido: {newState}");
        }
    }

    public void ActivateMonster()
    {
        if (isActivated) return;

        isActivated = true;
        Debug.Log("MONSTRO: Player pegou a lanterna");
    }

    public void SetMovementAndAnimationSpeed(float realSpeed, float animSpeed)
    {
        Movement.SetSpeed(realSpeed);
        Animator.speed = animSpeed;
    }

    public void SetAnimSpeed(float value)
    {
        if (Animator != null)
        {
            Animator.SetFloat(AnimSpeedID, value);
        }
    }

    public void TocarSomDePasso()
    {
        if (Animator != null && Animator.GetFloat(AnimSpeedID) < 0.1f)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            return;
        }

        if (audioSource != null && footstepSounds != null && footstepSounds.Length > 0)
        {
            nextFootstepIndex %= footstepSounds.Length;
            AudioClip clipToPlay = footstepSounds[nextFootstepIndex];

            if (clipToPlay != null)
                audioSource.PlayOneShot(clipToPlay);
            else
                DebugLogWarningOnce($"MonsterController: AudioClip no índice {nextFootstepIndex} é nulo.", this);

            nextFootstepIndex++;
        }
    }

    private HashSet<string> loggedWarnings = new HashSet<string>();
    public void DebugLogWarningOnce(string message, Object context)
    {
        if (!loggedWarnings.Contains(message))
        {
            Debug.LogWarning(message, context);
            loggedWarnings.Add(message);
        }
    }

    private void HandleGameOver()
    {
        if (isFrozen) return;
        isFrozen = true;
        Debug.Log("MONSTRO: Game Over, congelando");
        Movement.StopMovement();
        Animator.SetFloat(AnimSpeedID, 0);
        Movement.enabled = false;
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.enabled = false;
        }
        enabled = false;
    }

    void OnDestroy()
    {
        GameManager.OnGameOver -= HandleGameOver;
    }
}