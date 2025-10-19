using UnityEngine; // A "caixa de ferramentas" principal da Unity
using System.Collections.Generic; // Precisamos disso para usar "List<>" (Listas)

// [RequireComponent(typeof(Light))]
// É o "cadeado" de segurança. OBRIGA este GameObject a ter
// um componente 'Light' (Luz) junto com ele.
// Isso evita bugs, porque este script PRECISA da luz pra funcionar.
[RequireComponent(typeof(Light))]
public class FlashlightBeamSensor : MonoBehaviour
{
    // --- Variáveis (Campos) ---
    // Estas são as "memórias" e "configurações" do script.

    // "Caixinha" privada para guardar o componente 'Light'
    private Light flashlight;

    // 'LayerMask' é um "filtro". A gente diz no Inspector
    // quais "Layers" (Etiquetas) o nosso "raio laser" (Raycast)
    // deve considerar como um obstáculo (ex: "Paredes", "Chão").
    public LayerMask obstacleMask;

    // --- Configurações do "Leque" de Raios ---
    [Header("Configurações do Sensor")]
    [Tooltip("Quantos raios (linhas) vão sair da lanterna.")]
    public int numRays = 15; // O "leque"
    [Tooltip("Quantos pontos vão ter em CADA raio (linha).")]
    public int pointsPerRay = 10; // A "profundidade"
    [Tooltip("A distância (em metros) entre cada ponto no raio.")]
    public float raySpacing = 2.0f; // O "espaçamento"

    // --- A "NUVEM" DE PONTOS ---

    /// <summary>
    /// Esta é a LISTA PÚBLICA que guarda todos os pontos de luz
    /// que o sensor "vê" no mundo (no ar ou na parede).
    /// </summary>
    // '{ get; private set; }' = É uma "Propriedade" (variável chique).
    // 'get;' = É PÚBLICO. Outros scripts (como o MonsterSenses) podem LER esta lista.
    // 'private set;' = É PRIVADO. SÓ este script (FlashlightBeamSensor) pode MUDAR esta lista
    // (Adicionar, Limpar, etc). É o jeito mais SEGURO de fazer!
    public List<Vector3> VisibleLightPoints { get; private set; } = new List<Vector3>();

    /// <summary>
    /// Método 'Awake'. Roda ANTES de todo mundo (antes do Start).
    /// Perfeito para "pegar" componentes.
    /// </summary>
    void Awake()
    {
        // "Enche a nossa caixinha 'flashlight' com o componente 'Light'
        // que está neste mesmo GameObject."
        flashlight = GetComponent<Light>();
    }

    /// <summary>
    /// Método 'Update'. Roda UMA VEZ A CADA FRAME.
    /// É o "loop" principal do script.
    /// </summary>
    void Update()
    {
        // Checa se a luz (componente) está LIGADA.
        if (flashlight.enabled)
        {
            // Se sim, chama a função que "pinta" o mundo com os pontos.
            UpdateBeamPoints();
        }
        else // Se a luz estiver DESLIGADA...
        {
            // ...a gente precisa "limpar" a nuvem de pontos,
            // pro monstro não ficar vendo "fantasmas" de luz.

            // Checa se a lista JÁ NÃO ESTÁ vazia (Count > 0).
            // (Isso é uma otimização, pra ele não ficar limpando
            // uma lista vazia todo frame).
            if (VisibleLightPoints.Count > 0)
            {
                // Limpa a lista. ZERA tudo.
                VisibleLightPoints.Clear();
            }
        }
    }

    /// <summary>
    /// Esta é a função "Cérebro" do script.
    /// Ela dispara os raios e cria a "nuvem" de pontos.
    /// </summary>
    void UpdateBeamPoints()
    {
        // 1. LIMPA a lista. Começa do zero todo frame.
        // (Porque o player e a lanterna se movem).
        VisibleLightPoints.Clear();

        // 2. MATEMÁTICA do "Leque"
        // Calcula o "espaço" (em graus) entre cada raio.
        // 'spotAngle' = O ângulo do cone da nossa 'Light' (ex: 62 graus).
        // '(numRays > 1 ? numRays - 1 : 1)' = Um "if" rápido (ternário).
        // "Se 'numRays' for maior que 1, divide por (numRays - 1). Senão, divide por 1."
        // (Isso evita divisão por zero se 'numRays' for 1).
        float angleStep = flashlight.spotAngle / (numRays > 1 ? numRays - 1 : 1);

        // Calcula o ângulo inicial (metade pra esquerda).
        float startAngle = -flashlight.spotAngle / 2f; // Ex: -31 graus

        // 3. LOOP "DE FORA" (Os Raios do Leque)
        // 'for (int i = 0; ...)' = Roda 'numRays' vezes (ex: 15 vezes).
        for (int i = 0; i < numRays; i++)
        {
            // --- CALCULA A DIREÇÃO DE CADA RAIO ---

            // 'spreadRotation' = Gira o raio "para os lados" (horizontal).
            // 'transform.up' = Gira em volta do eixo Y (para os lados).
            Quaternion spreadRotation = Quaternion.AngleAxis(startAngle + (i * angleStep), transform.up);

            // 'tiltRotation' = O "toque especial". Gira o raio um POUQUINHO
            // pra cima/baixo aleatoriamente. Faz o cone de luz parecer "irregular".
            Quaternion tiltRotation = Quaternion.AngleAxis(Random.Range(-flashlight.spotAngle / 4, flashlight.spotAngle / 4), transform.right);

            // 'rayDirection' = A direção final do "raio laser" (combinando o giro
            // horizontal e o giro vertical).
            Vector3 rayDirection = tiltRotation * spreadRotation * transform.forward;

            // 4. LOOP "DE DENTRO" (Os Pontos em cada Raio)
            // 'for (int j = 1; ...)' = Roda 'pointsPerRay' vezes (ex: 10 vezes).
            // Este loop "caminha" pela linha do raio.
            for (int j = 1; j <= pointsPerRay; j++)
            {
                // Calcula o próximo ponto no "caminho" do raio.
                // (Ex: Ponto 1 = a 2m, Ponto 2 = a 4m, Ponto 3 = a 6m...).
                Vector3 checkPoint = transform.position + rayDirection * (j * raySpacing);

                // Calcula a distância do "checkPoint" até a lanterna.
                float distanceToCheck = Vector3.Distance(transform.position, checkPoint);

                // CHECAGEM DE SINCRONIA:
                // Se a distância desse ponto (ex: 72m) for MAIOR que
                // o 'Range' da nossa luz (ex: 70m)...
                if (distanceToCheck > flashlight.range) break; // ...'break' (para) este loop 'j'.
                                                               // (Não adianta desenhar pontos onde a luz real não chega).

                // --- OTIMIZAÇÃO DE RAYCAST (O BUG CORRIGIDO) ---
                // A gente só dispara UM "raio laser" por ponto.

                // Dispara o "laser" da lanterna ('transform.position'),
                // na direção ('rayDirection'),
                // com a distância MÁXIMA ('distanceToCheck'),
                // e filtrando pela máscara ('obstacleMask').
                // 'out RaycastHit hit' = Se ele bater, ele "devolve" a informação
                // do que ele bateu na "caixinha" 'hit'.
                if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit, distanceToCheck, obstacleMask))
                {
                    // SE BATEU EM ALGO (ex: uma parede a 5m):

                    // Adiciona o ponto EXATO da colisão ('hit.point') na lista.
                    VisibleLightPoints.Add(hit.point);

                    // 'break' = Para este loop 'j' (o "de dentro").
                    // Não adianta checar os pontos 6m, 8m, 10m...
                    // O raio já foi bloqueado pela parede.
                    break;
                }
                else
                {
                    // SE NÃO BATEU EM NADA (caminho livre):

                    // Adiciona o 'checkPoint' (o ponto no "ar") na lista.
                    VisibleLightPoints.Add(checkPoint);
                }
                // (FIM DA OTIMIZAÇÃO)
            }
        }
    }

    /// <summary>
    /// Método 'OnDrawGizmos'. É uma função especial da Unity
    /// que desenha coisas na tela do EDITOR (Scene View).
    /// Perfeito para "debugar" (ver o que o script tá fazendo).
    /// </summary>
    void OnDrawGizmos()
    {
        // Se a gente tiver uma luz, E ela estiver ligada, E a lista não for nula...
        if (flashlight != null && flashlight.enabled && VisibleLightPoints != null)
        {
            // ...vamos desenhar!

            // Define a cor do Gizmo (amarelo, meio transparente).
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);

            // 'foreach' = Um loop que "visita" CADA 'Vector3 point'
            // que estiver dentro da nossa lista 'VisibleLightPoints'.
            foreach (Vector3 point in VisibleLightPoints)
            {
                // Desenha uma esfera BEM PEQUENA (0.1f) em cada ponto.
                Gizmos.DrawSphere(point, 0.1f);

                // Desenha uma linha da lanterna ('transform.position')
                // ATÉ o ponto.
                Gizmos.DrawLine(transform.position, point);
            }
        }
    }
}