using UnityEngine;

public class PlayerCaptureZone : MonoBehaviour
{
    private bool hasBeenTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered) return;
        if (other.CompareTag("Monster") && other.TryGetComponent(out MonsterController monster))
        {
            if (monster.IsInChasingState)
            {
                Debug.LogWarning("PLAYER CAPTURE ZONE: O Monstro te pegou");
                hasBeenTriggered = true;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
                else
                {
                    Debug.LogError("PlayerCaptureZone: GameManager não encontrado");
                }
            }
            else
            {
                Debug.Log("Contato com monstro (Safe State)");
            }
        }
    }
}