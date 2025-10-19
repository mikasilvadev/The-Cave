using UnityEngine; // A "caixa de ferramentas" principal da Unity

// [RequireComponent(typeof(AudioSource))]
// Isso é um "cadeado" ou "exigência". Ele diz para a Unity:
// "Qualquer GameObject que usar ESTE script (MonsterAudio)
// é OBRIGADO a ter também um componente 'AudioSource' (a caixa de som)".
// Isso evita bugs da gente esquecer de adicionar o AudioSource.
[RequireComponent(typeof(AudioSource))]
public class MonsterAudio : MonoBehaviour
{
    // --- Variáveis (Campos) ---
    // Estas são as "memórias" do script.

    // 'audioSource' é a nossa "caixinha" privada para guardar
    // o componente AudioSource (a caixa de som) do monstro.
    private AudioSource audioSource;

    // '[Header("...")]' cria um título bonito no Inspector da Unity.
    [Header("Sons de Passos")]
    // 'public AudioClip[]' cria uma "lista" (array) pública no Inspector
    // onde a gente pode arrastar vários arquivos de áudio (os sons de passo).
    public AudioClip[] footstepClips;

    [Header("Som de Movimento Contínuo")]
    // 'public AudioClip' cria um "espaço" público para UM arquivo de áudio.
    // (Tipo um som de respiração, ou um rosnado baixo contínuo).
    public AudioClip movingClip;

    /// <summary>
    /// Método 'Awake'. Roda ANTES de todo mundo (antes do Start).
    /// É o lugar perfeito para "pegar" (GetComponent) os componentes
    /// e configurar as coisas.
    /// </summary>
    void Awake()
    {
        // "Enche a nossa caixinha 'audioSource' com o componente AudioSource
        // que está neste mesmo GameObject."
        audioSource = GetComponent<AudioSource>();

        // --- CONFIGURAÇÕES INICIAIS DA "CAIXA DE SOM" ---

        // 'spatialBlend = 1f' = Isso é o MAIS IMPORTANTE.
        // 1f (ou 100%) faz o som ser totalmente 3D.
        // O player vai ouvir se o som vem da esquerda, direita, de perto ou de longe.
        // (Se fosse 0f, seria 2D, e o som tocaria igual nos dois fones, sem direção).
        audioSource.spatialBlend = 1f;

        // 'loop = false' = O som não vai ficar repetindo sozinho (default).
        audioSource.loop = false;

        // 'playOnAwake = false' = O som NÃO vai tocar sozinho
        // assim que o jogo começar. Ele fica quieto até a gente mandar.
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Esta é uma função 'public', então outros scripts (ou Eventos de Animação)
    /// podem "apertar esse botão".
    /// O trabalho dela é tocar UM som de passo aleatório.
    /// (Perfeita para ser chamada num Evento de Animação, no frame que o pé toca o chão).
    /// </summary>
    public void TocarSomDePasso()
    {
        // 1. Checagem de Segurança: Se a "caixa de som" estiver desligada,
        // (se 'audioSource.enabled' for 'false'), não faz nada.
        if (!audioSource.enabled) return; // 'return' para a função.

        // 2. Checagem de Segurança: Se a nossa lista 'footstepClips' estiver
        // vazia (Length == 0), não faz nada. (Evita dar erro).
        if (footstepClips.Length == 0) return;

        // 3. Escolhe um clipe:
        // Pega um clipe ALEATÓRIO de dentro da nossa lista 'footstepClips'.
        // 'Random.Range(0, footstepClips.Length)' sorteia um número
        // (ex: 0, 1, 2... até o tamanho da lista).
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        // 4. Toca o som:
        // 'PlayOneShot' é o jeito CERTO de tocar sons curtos (passos, tiros).
        // Ele "dispara" o som uma vez e não para outros sons que
        // possam estar tocando.
        audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// "Botão" público para PARAR TODOS os sons e DESLIGAR a caixa de som.
    /// É o "botão de pânico" do áudio.
    /// Perfeito para o 'GameOverAndRestart' chamar.
    /// </summary>
    public void StopAllSounds()
    {
        audioSource.Stop(); // Para qualquer som que estiver tocando.
        audioSource.enabled = false; // Desliga o componente (ele não pode mais tocar nada).
    }

    /// <summary>
    /// "Botão" público para começar a tocar o SOM CONTÍNUO (ex: respiração).
    /// </summary>
    public void StartMovingSound()
    {
        // 1. Checagem de Segurança:
        // Se a gente arrastou um áudio para o 'movingClip' E
        // se a caixa de som NÃO estiver tocando nada agora...
        if (movingClip != null && !audioSource.isPlaying)
        {
            // ...então a gente configura e toca.

            // "Prepara" o som contínuo na caixa de som.
            audioSource.clip = movingClip;

            // Manda ele ficar em LOOP (repetindo).
            audioSource.loop = true;

            // Toca!
            audioSource.Play();
        }
    }

    /// <summary>
    /// "Botão" público para PARAR o som contínuo.
    /// </summary>
    public void StopMovingSound()
    {
        // Checagem de Segurança:
        // "Se o som que está tocando AGORA ('audioSource.clip')
        // for o mesmo som do 'movingClip'..."
        // (Isso evita que essa função pare um "som de passo" sem querer).
        if (audioSource.clip == movingClip)
        {
            // ...então pode parar.
            audioSource.Stop();
        }
    }

    /// <summary>
    /// "Botão" público para LIGAR o componente (se ele foi desligado).
    /// </summary>
    public void EnableSound()
    {
        audioSource.enabled = true;
    }

    /// <summary>
    /// "Botão" público para DESLIGAR o componente.
    /// </summary>
    public void DisableSound()
    {
        // Checagem extra: se ele for desligado ENQUANTO
        // tocava o som contínuo, manda parar o som primeiro.
        if (audioSource.clip == movingClip && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Desliga o componente.
        audioSource.enabled = false;
    }
}