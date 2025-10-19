using UnityEngine; // A "caixa de ferramentas" principal da Unity

/// <summary>
/// Este script é um "Controle Remoto" para o Player.
/// A única função dele é ter "botões" (funções públicas) que
/// a TIMELINE da Unity possa "apertar" durante uma cutscene.
/// Ex: No começo da cutscene, a Timeline "aperta" o botão DesativarControleJogador.
/// No final, ela "aperta" o AtivarControleJogador.
/// </summary>
public class CutsceneEvents : MonoBehaviour
{
    // --- Variáveis (Campos) ---
    // Estas são as "memórias" do script.
    // Elas são 'public' (públicas) para que a gente possa
    // "arrastar e soltar" o Player nelas lá no Inspector da Unity.

    // "Caixinha" para guardar o SCRIPT de controle do jogador.
    public PlayerController scriptDoJogador;

    // "Caixinha" para guardar o COMPONENTE de física do jogador.
    public CharacterController characterControllerDoJogador;


    /// <summary>
    /// Este é o "botão" de DESLIGAR o player.
    /// 'public void' significa que é um "botão" (função)
    /// que outros scripts (e a Timeline!) podem ver e "apertar".
    /// </summary>
    public void DesativarControleJogador()
    {
        // 1. CHECAGEM DE SEGURANÇA
        // 'if (scriptDoJogador != null)'
        // "Se a caixinha 'scriptDoJogador' NÃO (!) estiver vazia (null)..."
        // (Isso evita um erro caso a gente esqueça de arrastar o script lá no Inspector)
        if (scriptDoJogador != null)
        {
            // "...então, avisa o script do jogador que ele não 'podeMover' mais."
            // 'podeMover' é uma flag (bandeira) 'true'/'false'
            // que o próprio PlayerController usa pra saber se deve obedecer o teclado.
            scriptDoJogador.podeMover = false;
        }

        // 2. CHECAGEM DE SEGURANÇA (para o componente de física)
        // "Se a caixinha 'characterControllerDoJogador' NÃO estiver vazia..."
        if (characterControllerDoJogador != null)
        {
            // "...então, DESLIGA o componente CharacterController."
            // '.enabled = false' é como "desmarcar" a caixinha de
            // um componente no Inspector.
            // Isso "congela" o player no lugar e desliga a física/gravidade dele.
            // É ESSENCIAL para a cutscene!
            characterControllerDoJogador.enabled = false;
        }
    }

    /// <summary>
    /// Este é o "botão" de LIGAR o player de volta.
    /// (Chamado no final da cutscene).
    /// </summary>
    public void AtivarControleJogador()
    {
        // --- ORDEM DE ATIVAÇÃO (IMPORTANTE!) ---
        // A gente liga as coisas na ordem INVERSA que a gente desligou.
        // É mais seguro "descongelar" a física ANTES de tentar mover ela.

        // 1. LIGA A FÍSICA PRIMEIRO
        // "Se a caixinha do CharacterController não estiver vazia..."
        if (characterControllerDoJogador != null)
        {
            // "...então, LIGA o componente ('enabled = true')."
            // O player "descongela" e a gravidade volta a funcionar.
            characterControllerDoJogador.enabled = true;
        }

        // 2. LIGA A LÓGICA DEPOIS
        // "Se a caixinha do script do player não estiver vazia..."
        if (scriptDoJogador != null)
        {
            // "...então, avisa o script que ele 'podeMover' de novo."
            // O player agora volta a obedecer os comandos do teclado.
            scriptDoJogador.podeMover = true;
        }
    }
}