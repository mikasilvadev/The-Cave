using UnityEngine;

public class PlayerCaptureZone : MonoBehaviour
{
    private bool hasBeenTriggered = false;

    // Esta função é chamada automaticamente pela Unity quando algo entra no gatilho deste objeto.
    void OnTriggerEnter(Collider other)
    {
        // Se já fomos pegos, não faz mais nada.
        if (hasBeenTriggered) return;

        // Verificamos se quem entrou no nosso "campo de força" foi o Monstro.
        // Usaremos a tag "Monster" para isso.
        if (other.CompareTag("Monster"))
        {
            Debug.LogWarning("PLAYER CAPTURE ZONE: O Monstro entrou na zona de captura! FIM DE JOGO!");

            // Impede que o gatilho seja acionado várias vezes.
            hasBeenTriggered = true;

            // Tenta encontrar o AIController no objeto que entrou na zona.
            AIController monsterController = other.GetComponent<AIController>();
            if (monsterController != null)
            {
                // Se encontrou, inicia a sequência de Game Over.
                monsterController.GameOverAndRestart();
            }
            else
            {
                Debug.LogError("O objeto com a tag 'Monster' não possui o script AIController! Não é possível reiniciar o jogo.");
            }
        }
    }
}