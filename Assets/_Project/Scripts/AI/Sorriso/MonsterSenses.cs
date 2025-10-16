using UnityEngine;

public class MonsterSenses : MonoBehaviour
{
    [Header("Configurações de Visão")]
    public Light playerFlashlight;
    public float visionAngle = 90f;
    public float visionDistance = 20f;
    public LayerMask visionMask;

    [Header("Configurações de Detecção de Luz")]
    [Tooltip("A que distância o monstro consegue perceber o foco de luz da lanterna.")]
    public float lightDetectionDistance = 30f;
    [Tooltip("O ângulo de visão do monstro para detectar o foco de luz (pode ser maior que a visão normal).")]
    public float lightDetectionAngle = 120f;

    [Header("Detecção de Luz Avançada")]
    [Tooltip("Quantos raios serão disparados para simular o cone de luz. (Ex: 5 = 1 central + 4 ao redor).")]
    [Range(1, 15)]
    public int lightDetectionRayCount = 7;
    [Tooltip("O ângulo de abertura do cone de raios. DEVE SER IGUAL AO 'Spot Angle' do seu componente de Luz para ser realista.")]
    public float lightDetectionSpread = 40f;

    [Header("Configurações de Audição")]
    public float baseHearingRadius = 15f;
    public float runningSoundMultiplier = 1.5f;

    private Transform player;
    private PlayerController playerController;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject.transform;
        playerController = playerObject.GetComponent<PlayerController>();
    }

    public bool CanSeePlayer(out Vector3 playerPosition)
    {
        playerPosition = Vector3.zero;

        if (!playerFlashlight.enabled) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > visionDistance) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > visionAngle / 2f) return false;

        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, visionDistance, visionMask))
        {
            Debug.DrawRay(transform.position, directionToPlayer * hit.distance, Color.cyan, 1.0f);

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("<color=green>SENSES: Linha de visão direta com o JOGADOR!</color>", this.gameObject);
                playerPosition = player.position;
                return true;
            }
            else
            {
                Debug.Log($"SENSES: Linha de visão bloqueada por <color=red>{hit.collider.name}</color>", this.gameObject);
            }
        }

        return false;
    }

    public bool CanSeeFlashlightBeam(out Vector3 lightHitPosition)
    {
        lightHitPosition = Vector3.zero;

        if (!playerFlashlight.enabled)
        {
            return false;
        }

        Transform flashlightTransform = playerFlashlight.transform;

        for (int i = 0; i < lightDetectionRayCount; i++)
        {
            Vector3 rayDirection;

            if (i == 0)
            {
                rayDirection = flashlightTransform.forward;
            }
            else
            {
                float randomAngle = Random.Range(0f, 360f);
                float randomRadius = Random.Range(0f, 1f);
                float spreadRad = Mathf.Tan(lightDetectionSpread * 0.5f * Mathf.Deg2Rad) * Mathf.Sqrt(randomRadius);

                Vector3 spreadVector = new Vector3(Mathf.Cos(randomAngle) * spreadRad, Mathf.Sin(randomAngle) * spreadRad, 1f);
                rayDirection = flashlightTransform.TransformDirection(spreadVector.normalized);
            }

            if (Physics.Raycast(flashlightTransform.position, rayDirection, out RaycastHit lightHit, 100f, visionMask))
            {
                Vector3 currentHitPoint = lightHit.point;
                Debug.DrawLine(flashlightTransform.position, currentHitPoint, Color.yellow, 0.1f);

                if (IsPointVisibleToMonster(currentHitPoint))
                {
                    lightHitPosition = currentHitPoint;
                    Debug.Log($"<color=cyan>SENSES: FOCO DE LUZ PERIFÉRICO detectado em {lightHitPosition}!</color>", this.gameObject);
                    Debug.DrawLine(transform.position, lightHitPosition, Color.cyan, 1.0f);
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPointVisibleToMonster(Vector3 point)
    {
        float distanceToPoint = Vector3.Distance(transform.position, point);

        if (distanceToPoint > lightDetectionDistance) return false;

        Vector3 directionToPoint = (point - transform.position).normalized;
        float angleToPoint = Vector3.Angle(transform.forward, directionToPoint);

        if (angleToPoint > lightDetectionAngle / 2f) return false;

        if (Physics.Raycast(transform.position, directionToPoint, out RaycastHit monsterViewHit, distanceToPoint - 0.1f))
        {
            Debug.DrawRay(transform.position, directionToPoint * monsterViewHit.distance, Color.magenta, 1.0f);
            return false;
        }

        return true;
    }

    public bool CheckForSound(out Vector3 soundPosition)
    {
        soundPosition = player.position;

        if (playerController == null || !playerController.IsMoving)
        {
            return false;
        }

        float finalHearingRadius = baseHearingRadius;

        if (playerController.IsRunning)
        {
            finalHearingRadius *= runningSoundMultiplier;
        }

        if (Vector3.Distance(transform.position, soundPosition) <= finalHearingRadius)
        {
            Debug.Log($"<color=yellow>SENSES: Som do jogador detectado! Raio: {finalHearingRadius}m</color>", this.gameObject);
            return true;
        }

        return false;
    }
}