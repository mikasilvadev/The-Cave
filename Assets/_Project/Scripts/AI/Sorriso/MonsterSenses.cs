// Scripts/AI/Sorriso/MonsterSenses.cs

using UnityEngine;

public class MonsterSenses : MonoBehaviour
{
    [Header("Configurações de Visão")]
    public Light playerFlashlight;
    public float visionAngle = 90f;
    public float visionDistance = 20f;
    public LayerMask visionMask;

    [Header("Configurações de Audição")]
    public float baseHearingRadius = 15f;
    public float runningSoundMultiplier = 1.5f;

    private Transform player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
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
            Debug.DrawRay(transform.position, directionToPlayer * hit.distance, Color.cyan, 1.0f); // Desenha o raio na cena

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

    public bool CanHearSound(Vector3 soundOrigin, float soundRadius)
    {
        return Vector3.Distance(transform.position, soundOrigin) <= soundRadius;
    }
}