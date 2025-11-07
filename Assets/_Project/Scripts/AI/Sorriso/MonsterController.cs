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
    public float chasingAnimMultiplier = 3.0f;

    private bool isActivated = false;
    private bool isFrozen = false;

    [Header("Giro Manual (Para não perder o player de vista)")]
    public float manualTurnSpeed = 15f;

    void Awake()
    {
        Movement = GetComponent<AIMovement>();
        Animator = GetComponentInChildren<Animator>();
        AnimSpeedID = Animator.StringToHash("Speed");
        audioSource = GetComponent<AudioSource>();

        if (playerTransform == null)
            Player = GameObject.FindGameObjectWithTag("Player")?.transform;
        else
            Player = playerTransform;

        states.Add(StateType.Chasing, new ChasingState(this));
        states.Add(StateType.DarkMonitoring, new DarkMonitoringState(this));
        GameManager.OnGameOver += HandleGameOver;
    }

    void Start()
    {
        if (audioSource != null) audioSource.playOnAwake = false;
        if (Player != null)
        {
            var pc = Player.GetComponent<PlayerController>();
            if (pc != null && pc.heldFlashlightObject != null)
                playerHeldLight = pc.heldFlashlightObject.GetComponentInChildren<Light>();
        }
        TransitionToState(StateType.DarkMonitoring);
    }

    void Update()
    {
        if (playerHeldLight == null && Player != null)
        {
            var pc = Player.GetComponent<PlayerController>();
            if (pc != null && pc.heldFlashlightObject != null)
                playerHeldLight = pc.heldFlashlightObject.GetComponentInChildren<Light>();
        }
        IsPlayerLightOn = (playerHeldLight != null && playerHeldLight.enabled && playerHeldLight.gameObject.activeInHierarchy);

        if (isFrozen) return;

        if (isActivated && currentState != null)
        {
            currentState.Execute();
        }

        if (isActivated && Movement.Agent != null && Movement.Agent.isOnNavMesh)
        {
            float currentSpeed = Movement.Agent.velocity.magnitude;
            float maxSpeed = Movement.Agent.speed;
            float animIntensity = (maxSpeed > 0) ? (currentSpeed / maxSpeed) : 0f;
            SetAnimSpeed(animIntensity);

            if (currentSpeed > 0.5f && Movement.Agent.velocity != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(Movement.Agent.velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * manualTurnSpeed);
            }
        }
        else
        {
            SetAnimSpeed(0);
        }
    }

    public void TransitionToState(StateType newState)
    {
        if (currentState != null) currentState.Exit();
        if (states.ContainsKey(newState))
        {
            currentState = states[newState];
            IsInChasingState = (newState == StateType.Chasing);
            Animator.speed = IsInChasingState ? chasingAnimMultiplier : animationSpeedMultiplier;
            currentState.Enter();
        }
    }

    public void ActivateMonster()
    {
        if (isActivated) return;
        isActivated = true;
    }

    public void SetMovementAndAnimationSpeed(float realSpeed, float animSpeedBase)
    {
        Movement.SetSpeed(realSpeed);
        Animator.speed = animSpeedBase;
    }

    public void SetAnimSpeed(float value)
    {
        if (Animator != null) Animator.SetFloat(AnimSpeedID, value);
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