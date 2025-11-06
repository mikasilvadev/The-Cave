using UnityEngine;

public class PlayerCaptureZone : MonoBehaviour
{
    private bool hasBeenTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered) return;

        if (other.CompareTag("Monster"))
        {
            MonsterController monsterController = other.GetComponent<MonsterController>();
            if (monsterController != null && monsterController.IsInChasingState)
            {
                Debug.LogWarning("PLAYER CAPTURE ZONE: O Monstro te pegou no estado Chasing");

                hasBeenTriggered = true;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver();
                }
                else
                {
                    Debug.LogError("PlayerCaptureZone: Não encontrou o GameManager.Instance", this.gameObject);
                }
            }
            else if (monsterController != null && !monsterController.IsInChasingState)
            {
                Debug.Log("PLAYER CAPTURE ZONE: O Monstro te tocou, mas estava no modo DarkMonitoring");
            }
        }
    }
}