// Scripts/AI/Sorriso/AIController.cs

using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(MonsterSenses))]
public class AIController : MonoBehaviour
{
    [Header("Componentes")]
    public NavMeshAgent Agent { get; private set; }
    public MonsterSenses Senses { get; private set; }
    public Transform Player { get; private set; }
    public PlayerController PlayerController { get; private set; }

    [Header("Configurações de Movimento")]
    public float wanderSpeed = 2f;
    public float chaseSpeed = 5f;
    public float searchSpeed = 3.5f;
    public float chaseRotationSpeed = 5f;

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
        if (currentState != null)
        {
            currentState.Execute(this);
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
    }
}