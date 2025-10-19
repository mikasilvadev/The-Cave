using UnityEngine; // A "caixa de ferramentas" principal da Unity

/// <summary>
/// Este script é a "Zona de Captura" (ou "Hitbox") do Player.
/// Ele deve ser colocado em um GameObject "filho" do Player,
/// que tenha um Collider (ex: Capsule Collider) com a opção
/// "Is Trigger" MARCADA no Inspector.
///
/// A única função dele é detectar quando o Monstro "encosta"
/// no player e iniciar o Game Over.
/// </summary>
public class PlayerCaptureZone : MonoBehaviour
{
    // --- Variáveis (Campos) ---

    // 'hasBeenTriggered' é a nossa "trava" de segurança.
    // É uma flag (bandeira) 'true'/'false' que começa como 'false'.
    // Ela serve para garantir que o Game Over seja chamado SÓ UMA VEZ.
    private bool hasBeenTriggered = false;

    /// <summary>
    /// Esta é uma função MÁGICA da Unity.
    /// Ela é chamada AUTOMATICAMENTE pelo motor de física da Unity
    /// no exato frame em que OUTRO Collider (marcado como Trigger
    /// ou um Rigidbody) ENTRA no nosso gatilho.
    /// </summary>
    /// <param name="other">
    ///   A Unity nos "entrega" aqui o 'Collider'
    ///   da "outra coisa" que entrou (ex: o Collider do Monstro).
    /// </param>
    void OnTriggerEnter(Collider other)
    {
        // 1. A CHECAGEM DA "TRAVA"
        // "Se a 'trava' 'hasBeenTriggered' já for 'true' (ou seja,
        // o player JÁ foi pego)..."
        if (hasBeenTriggered) return; // ...'return'. PARA a função AQUI.
                                      // Não faz mais nada. (Evita 10 Game Overs)

        // 2. A CHECAGEM DO "QUEM"
        // Se a trava estava 'false' (é a primeira vez),
        // vamos checar O QUE entrou no gatilho.

        // 'other.CompareTag("Monster")'
        // "O 'Collider' da 'outra coisa' tem a Etiqueta (Tag) 'Monster'?"
        // (É o jeito mais rápido e certo de checar).
        if (other.CompareTag("Monster"))
        {
            // Se SIM, o Monstro nos pegou!

            // Manda um AVISO (Warning) pro Console.
            Debug.LogWarning("PLAYER CAPTURE ZONE: O Monstro entrou na zona de captura! FIM DE JOGO!");

            // 3. ATIVA A "TRAVA"
            // "Pronto. O player foi pego. Ativa a trava para 'true'."
            // (Se essa função chamar de novo no próximo frame,
            // ela vai parar lá no 'if' da linha 24).
            hasBeenTriggered = true;

            // 4. AVISA O "CÉREBRO" DO MONSTRO

            // 'other.GetComponent<AIController>()'
            // "Pega o script 'AIController' (o "cérebro")
            // que está no GameObject do 'other' (o Monstro)."
            AIController monsterController = other.GetComponent<AIController>();

            // 5. CHECAGEM DE SEGURANÇA
            // "A gente conseguiu achar o script 'AIController'?"
            if (monsterController != null) // Se 'monsterController' NÃO for 'null' (vazio)...
            {
                // ...DEU BOM!
                // "Cérebro do Monstro, chame a sua função
                // pública 'GameOverAndRestart()'."
                // (O AIController que se vira pra fazer o fade,
                // parar o som, travar o player e recarregar a cena).
                monsterController.GameOverAndRestart();
            }
            else // Se 'monsterController' for 'null' (vazio)...
            {
                // ...DEU RUIM!
                // Isso é um BUG DE CONFIGURAÇÃO (esquecemos de
                // botar o script AIController no Monstro, ou a Tag tá errada).
                // Manda um ERRO (Error) em vermelho pro Console.
                Debug.LogError("O objeto com a tag 'Monster' não possui o script AIController! Não é possível reiniciar o jogo.");
            }
        }
    }
}