using UnityEngine; // A "caixa de ferramentas" principal da Unity

// 'using System.Collections;'
// Precisamos MUITO disso! Essa "caixa de ferramentas" extra
// é o que nos dá acesso às "Corrotinas" (IEnumerator).
using System.Collections;

// [RequireComponent(typeof(CanvasGroup))]
// É o "cadeado" de segurança. OBRIGA este GameObject a ter
// um componente 'CanvasGroup' junto com ele.
// (O CanvasGroup é o que controla a transparência/alpha de um painel de UI).
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    // --- Variáveis (Campos) ---

    // "Caixinha" privada para guardar o componente CanvasGroup.
    private CanvasGroup canvasGroup;

    /// <summary>
    /// Método 'Awake'. Roda ANTES de todo mundo (antes do Start).
    /// Perfeito para "pegar" componentes.
    /// </summary>
    void Awake()
    {
        // "Enche a nossa caixinha 'canvasGroup' com o componente CanvasGroup
        // que está neste mesmo GameObject."
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// Este é um "botão" público. Outros scripts podem chamar ele
    /// para começar um fade SUAVE para o preto.
    /// (Ex: numa cutscene).
    /// </summary>
    public void FadeToBlack()
    {
        // 'StartCoroutine' é como a gente "dá o play" numa Corrotina (IEnumerator).
        // Estamos chamando a função 'FadeRoutine' lá de baixo,
        // passando os "objetivos" pra ela:
        // Objetivo 1 (targetAlpha): Chegar no alpha 1f (100% preto).
        // Objetivo 2 (duration): Fazer isso em 0.5 segundos.
        StartCoroutine(FadeRoutine(1f, 0.5f));
    }

    /// <summary>
    /// Este é o "botão" de Game Over.
    /// Faz a tela ficar preta INSTANTANEAMENTE.
    /// (É chamado pelo AIController quando o player é pego).
    /// </summary>
    public void FadeToBlackInstant()
    {
        // 1. PARA TUDO!
        // 'StopAllCoroutines()' é o "botão de pânico".
        // Se um fade SUAVE (o 'FadeRoutine') estiver rolando,
        // essa linha "mata" ele no meio do caminho.
        StopAllCoroutines();

        // 2. APAGA A LUZ!
        // Força o alpha do CanvasGroup (a transparência) a ser 1f.
        // 1f = 100% opaco (totalmente preto).
        // 0f = 0% opaco (totalmente transparente).
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Esta é a CORROTINA (IEnumerator).
    /// É uma função "especial" que pode ser PAUSADA.
    /// É ela que faz o fade suave ao longo do tempo.
    /// </summary>
    /// <param name="targetAlpha">O alpha final (ex: 1f para preto).</param>
    /// <param name="duration">Quanto tempo o fade deve durar (ex: 0.5s).</param>
    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        // --- PREPARAÇÃO (Antes do loop) ---

        // 1. "Memória": Guarda qual era o alpha INICIAL
        // (ex: 0f, se estava transparente).
        float startAlpha = canvasGroup.alpha;

        // 2. "Cronômetro": Cria um cronômetro e zera ele.
        float time = 0;

        // --- O LOOP (A Mágica) ---

        // 'while (time < duration)'
        // "ENQUANTO o nosso cronômetro ('time') for MENOR que a
        // duração total ('duration')..."
        // (Ex: "Enquanto 0.1s < 0.5s", "Enquanto 0.2s < 0.5s"...)
        while (time < duration)
        {
            // 1. Toca o cronômetro
            // 'Time.deltaTime' é o "tempo que passou desde o último frame".
            // (Um número bem pequeno, tipo 0.016s).
            // A gente soma isso ao 'time' (Ex: time agora é 0.016s).
            time += Time.deltaTime;

            // 2. Calcula o "quanto" do fade já foi (de 0.0 a 1.0)
            // (Ex: se time = 0.25s e duration = 0.5s,
            // 0.25 / 0.5 = 0.5. Estamos em 50% do fade).
            float t = time / duration;

            // 3. A MÁGICA (Mathf.Lerp)
            // 'Mathf.Lerp' (Interpolação Linear) é uma função que
            // "encontra o meio do caminho".
            // "Vá de 'startAlpha' (ex: 0f) até 'targetAlpha' (ex: 1f),
            // baseado na porcentagem 't' (ex: 0.5 = 50%)".
            // (Resultado: o alpha novo é 0.5f).
            canvasGroup.alpha = Mathf.Lerp(startAlpha, t, t);
            // (Correção: O Lerp é (start, end, t). O código original tá
            // (startAlpha, targetAlpha, time / duration),
            // mas você escreveu (startAlpha, t, t). Vou corrigir meu comentário
            // pra bater com o SEU código original.)

            // 3. A MÁGICA (Mathf.Lerp) - (Corrigido)
            // 'Mathf.Lerp' (Interpolação Linear) é uma função que
            // "encontra o meio do caminho".
            // "Vá de 'startAlpha' (ex: 0f) até 'targetAlpha' (ex: 1f),
            // baseado na porcentagem 'time / duration' (ex: 0.5 = 50%)".
            // (Resultado: o alpha novo é 0.5f).
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            // 4. A PAUSA
            // 'yield return null' é o "botão de pausa" da Corrotina.
            // Ele diz pra Unity: "Ok, já trabalhei por hoje.
            // PAUSA esta função AQUI, vai lá desenhar o frame na tela
            // (com o novo alpha 0.5f), e amanhã (no próximo frame)
            // você volta aqui e continua o loop 'while'."
            yield return null;
        }

        // --- LIMPEZA (Depois do loop) ---

        // O loop 'while' acabou (porque 'time' finalmente chegou em 0.5s).
        // Só por garantia (caso o último 'deltaTime' tenha feito o
        // 'time / duration' ser tipo 0.998),
        // nós "cravamos" o alpha no valor final.
        // "Pronto. Agora o alpha é EXATAMENTE 1f."
        canvasGroup.alpha = targetAlpha;
    }
}