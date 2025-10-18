using UnityEngine;

public class MonsterSenses : MonoBehaviour
{
    [Header("Configurações de Visão")]
    [Tooltip("Referência ao objeto de luz da lanterna do jogador.")]
    public Light playerFlashlight;

    [Tooltip("Arraste aqui o mesmo objeto que contém o componente 'Light' da lanterna. Usado para a detecção de ser iluminado.")]
    public Light playerFlashlightComponent;

    public float visionAngle = 90f;
    public float visionDistance = 20f;
    public LayerMask visionMask;

    [Header("Configurações de Detecção de Luz")]
    [Tooltip("A que distância o monstro consegue perceber o foco de luz da lanterna em uma superfície.")]
    public float lightDetectionDistance = 30f;
    [Tooltip("O ângulo de visão do monstro para detectar o foco de luz (pode ser maior que a visão normal).")]
    public float lightDetectionAngle = 120f;

    [Header("Detecção de Luz Avançada")]
    [Tooltip("Quantos raios serão disparados para simular o cone de luz.")]
    [Range(1, 15)]
    public int lightDetectionRayCount = 7;
    [Tooltip("O ângulo de abertura do cone de raios. DEVE SER IGUAL AO 'Spot Angle' do seu componente de Luz para ser realista.")]
    public float lightDetectionSpread = 40f;
    private FlashlightBeamSensor beamSensor;

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

        if (playerFlashlight != null)
        {
            beamSensor = playerFlashlight.GetComponent<FlashlightBeamSensor>();
            if (beamSensor == null)
            {
                Debug.LogError("MonsterSenses: Não foi encontrado o componente 'FlashlightBeamSensor' no objeto da lanterna do jogador! Verifique a configuração.", this.gameObject);
            }
        }
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
            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("<color=green>SENSES: Linha de visão direta com o JOGADOR!</color>", this.gameObject);
                playerPosition = player.position;
                return true;
            }
        }
        return false;
    }

    public bool IsPlayerShiningLightOnMe(out Vector3 playerPosition)
    {
        playerPosition = player.position;

        if (playerFlashlightComponent == null || !playerFlashlightComponent.enabled)
        {
            return false;
        }

        float distanceToMonster = Vector3.Distance(playerFlashlightComponent.transform.position, transform.position);
        if (distanceToMonster > playerFlashlightComponent.range)
        {
            return false;
        }

        Vector3 directionToMonster = (transform.position - playerFlashlightComponent.transform.position).normalized;
        float angleToMonster = Vector3.Angle(playerFlashlightComponent.transform.forward, directionToMonster);

        if (angleToMonster < playerFlashlightComponent.spotAngle / 2f)
        {
            if (!Physics.Raycast(playerFlashlightComponent.transform.position, directionToMonster, distanceToMonster, visionMask))
            {
                Debug.Log("<color=red>SENSES:</color> ESTOU SENDO ILUMINADO DIRETAMENTE!", this.gameObject);
                return true;
            }
        }

        return false;
    }

    public bool CanSeeFlashlightBeam(out Vector3 lightHitPosition)
    {
        lightHitPosition = Vector3.zero;

        if (beamSensor == null || !playerFlashlight.enabled || beamSensor.VisibleLightPoints.Count == 0)
        {
            return false;
        }

        int pontosParaChecar = 15;
        int step = Mathf.Max(1, beamSensor.VisibleLightPoints.Count / pontosParaChecar);

        for (int i = 0; i < beamSensor.VisibleLightPoints.Count; i += step)
        {
            Vector3 point = beamSensor.VisibleLightPoints[i];

            if (IsPointVisibleToMonster(point))
            {
                lightHitPosition = point;
                Debug.Log($"<color=cyan>SENSES: LUZ AMBIENTE (do Sensor) detectada em {lightHitPosition}!</color>", this.gameObject);
                return true;
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

        if (Physics.Raycast(transform.position, directionToPoint, out RaycastHit monsterViewHit, distanceToPoint - 0.1f, visionMask))
        {
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