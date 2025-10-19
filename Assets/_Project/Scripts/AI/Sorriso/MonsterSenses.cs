using UnityEngine; // A "caixa de ferramentas" principal da Unity
using System.Collections.Generic; // Precisamos disso para usar "List<>" (Listas)

/// <summary>
/// Este é o script de "Sentidos" do Monstro.
/// Ele funciona como os "olhos" e "ouvidos".
/// Ele não TOMA decisões, ele só RESPONDE PERGUNTAS que o
/// 'AIController' (o cérebro) faz pra ele.
/// (Ex: "Você está vendo o player? Sim/Não").
/// </summary>
public class MonsterSenses : MonoBehaviour
{
    // --- Variáveis (Campos) ---
    // São as "memórias" e "configurações" dos sentidos.

    [Header("Configurações de Visão")] // Título no Inspector

    // '[Tooltip(...)]' = Mostra uma "dica" quando passamos o mouse por cima no Inspector.
    [Tooltip("Referência principal ao componente 'Light' da lanterna do jogador.")]
    // Esta é a NOSSA ÚNICA referência para a luz do player.
    // Vamos usar ela pra TUDO (checar se tá ligada, pegar o BeamSensor, etc).
    public Light playerFlashlight;
    // (NOTA: A variável duplicada 'playerFlashlightComponent' foi REMOVIDA daqui)

    [Tooltip("O ângulo do 'cone' de visão do monstro (para ver o PLAYER).")]
    public float visionAngle = 90f; // 90 graus (45 pra esq, 45 pra dir)
    [Tooltip("Até onde o monstro enxerga o PLAYER.")]
    public float visionDistance = 20f; // 20 metros

    // 'LayerMask' é um "filtro" de física.
    // A gente define no Inspector o que o monstro considera "parede"
    // (para bloquear a visão).
    public LayerMask visionMask;

    [Header("Configurações de Detecção de Luz")]
    [Tooltip("A que distância o monstro consegue perceber QUALQUER sinal de luz (foco ou facho).")]
    public float lightDetectionDistance = 70f; // <-- Aumentamos esse valor!
    [Tooltip("O ângulo de visão para detectar QUALQUER sinal de luz (pode ser maior que a visão normal).")]
    public float lightDetectionAngle = 120f;

    // (NOTA: O Header "Detecção de Luz Avançada" foi REMOVIDO daqui
    // porque 'lightDetectionRayCount' e 'lightDetectionSpread'
    // eram "código morto" do sistema antigo e não faziam mais nada.)

    // "Caixinha" privada para guardar o link para o script da lanterna (o BeamSensor).
    private FlashlightBeamSensor beamSensor;

    [Header("Configurações de Audição")]
    [Tooltip("O 'raio' de audição base (se o player andar).")]
    public float baseHearingRadius = 15f;
    [Tooltip("Multiplicador da audição se o player CORRER.")]
    public float runningSoundMultiplier = 1.5f; // 1.5x mais longe

    // "Caixinhas" privadas para guardar o Player (pra checar distância, etc)
    private Transform player;
    private PlayerController playerController; // Script do Player (pra saber se ele tá correndo)

    /// <summary>
    /// Método 'Start'. Roda UMA VEZ, no primeiro frame do jogo (depois do Awake).
    /// Usamos para "achar" e "guardar" os links (referências) do player e do BeamSensor.
    /// </summary>
    void Start()
    {
        // 'GameObject.FindGameObjectWithTag("Player")'
        // Procura na cena INTEIRA por um objeto com a etiqueta (Tag) "Player".
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        // Guarda o 'transform' (posição/rotação) do player na nossa "caixinha" 'player'.
        player = playerObject.transform;

        // Guarda o script 'PlayerController' do player na nossa "caixinha".
        playerController = playerObject.GetComponent<PlayerController>();

        // Se a gente arrastou a lanterna lá no Inspector...
        if (playerFlashlight != null)
        {
            // ...vamos tentar pegar o script 'FlashlightBeamSensor' que tá nela.
            beamSensor = playerFlashlight.GetComponent<FlashlightBeamSensor>();

            // Checagem de Segurança: Se não achou o script...
            if (beamSensor == null)
            {
                // ...avisa no Console que deu ruim. (Provavelmente esquecemos de adicionar o script).
                Debug.LogError("MonsterSenses: Não foi encontrado o componente 'FlashlightBeamSensor' no objeto da lanterna do jogador! Verifique a configuração.", this.gameObject);
            }
        }
    }

    /// <summary>
    /// PERGUNTA: "O monstro consegue ver o PLAYER?"
    /// (Esta é a "visão direta").
    /// </summary>
    /// <param name="playerPosition">
    ///   'out' é uma "porta de saída". Se a resposta for 'true' (sim),
    ///   a gente "devolve" a posição do player por aqui.
    /// </param>
    /// <returns>Retorna 'true' (sim) ou 'false' (não).</returns>
    public bool CanSeePlayer(out Vector3 playerPosition)
    {
        // Começa "zerando" a posição de saída, por segurança.
        playerPosition = Vector3.zero;

        // --- MECÂNICA DE JOGO PRINCIPAL ---
        // Se a lanterna do player estiver DESLIGADA...
        if (playerFlashlight == null || !playerFlashlight.enabled) return false; // ...o monstro é CEGO. 'return false' na hora.
        // ------------------------------------

        // 1. CHECAGEM DE DISTÂNCIA
        // Calcula a distância entre o monstro e o player.
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Se a distância for MAIOR que a 'visionDistance' (ex: 20m)...
        if (distanceToPlayer > visionDistance) return false; // ...'return false'. Para a função aqui.

        // 2. CHECAGEM DE ÂNGULO (Cone de Visão NORMAL)
        // Calcula a "direção" (um vetor/seta) do monstro PARA o player.
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // Calcula o ângulo (em graus) entre "para onde o monstro tá olhando"
        // ('transform.forward') e a "direção do player".
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        // 'visionAngle / 2f' (ex: 90/2 = 45).
        // Se o ângulo for MAIOR que 45 graus (ou seja, o player tá fora do cone de visão normal)...
        if (angleToPlayer > visionAngle / 2f) return false; // ...'return false'. Para a função aqui.

        // 3. CHECAGEM DE OBSTÁCULO (Raycast)
        // Dispara um "raio laser" invisível do monstro, na direção do player,
        // com o comprimento máximo da 'visionDistance', e filtrando pela 'visionMask'.
        if (Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, visionDistance, visionMask))
        {
            // Se o "laser" BATEU em alguma coisa...
            // ...a gente checa se o que ele bateu tem a Tag "Player".
            if (hit.collider.CompareTag("Player"))
            {
                // Se acertou o player (e não uma parede no meio)...
                Debug.Log("<color=green>SENSES: Linha de visão direta com o JOGADOR!</color>", this.gameObject);

                // "Devolve" a posição do player pela "porta de saída".
                playerPosition = player.position;

                // Retorna 'true' (SIM, ELE VÊ!).
                return true;
            }
        }

        // Se passou por tudo e não acertou o player (ex: acertou uma parede),
        // ou se não acertou nada (player no escuro), retorna 'false'.
        return false;
    }

    /// <summary>
    /// PERGUNTA: "O player está me ILUMINANDO DIRETAMENTE?"
    /// (O monstro pergunta "Eu estou no cone de luz do player?").
    /// </summary>
    public bool IsPlayerShiningLightOnMe(out Vector3 playerPosition)
    {
        // "Devolve" a posição do player (útil pro AIController).
        playerPosition = player.position;

        // 1. CHECAGEM BÁSICA: Se a lanterna não existe ou tá desligada...
        if (playerFlashlight == null || !playerFlashlight.enabled)
        {
            return false; // ...'return false'.
        }

        // 2. CHECAGEM DE DISTÂNCIA
        // Calcula a distância da LANTERNA até o MONSTRO.
        float distanceToMonster = Vector3.Distance(playerFlashlight.transform.position, transform.position);

        // Se a distância for MAIOR que o 'Range' (alcance) da luz...
        if (distanceToMonster > playerFlashlight.range)
        {
            return false; // ...'return false' (o monstro tá longe demais).
        }

        // 3. CHECAGEM DE ÂNGULO (Cone de Luz da LANTERNA)
        // Calcula a "direção" (seta) da LANTERNA PARA o MONSTRO.
        Vector3 directionToMonster = (transform.position - playerFlashlight.transform.position).normalized;

        // Calcula o ângulo entre "para onde a LUZ aponta"
        // ('playerFlashlight.transform.forward') e a "direção do monstro".
        float angleToMonster = Vector3.Angle(playerFlashlight.transform.forward, directionToMonster);

        // 'playerFlashlight.spotAngle / 2f' (ex: 62/2 = 31).
        // Se o ângulo for MENOR que 31 graus (ou seja, o monstro TÁ DENTRO do cone da lanterna)...
        if (angleToMonster < playerFlashlight.spotAngle / 2f)
        {
            // 4. CHECAGEM DE OBSTÁCULO (Raycast DA LANTERNA)
            // Dispara um "laser" da LANTERNA para o MONSTRO.
            // Se NÃO (!) acertar nada (nenhuma parede no meio)...
            if (!Physics.Raycast(playerFlashlight.transform.position, directionToMonster, distanceToMonster, visionMask))
            {
                // ...significa que a luz BATEU no monstro.
                Debug.Log("<color=red>SENSES:</color> ESTOU SENDO ILUMINADO DIRETAMENTE!", this.gameObject);
                return true; // SIM, ESTÁ!
            }
        }

        // Se falhou em qualquer checagem, retorna 'false'.
        return false;
    }

    /// <summary>
    /// PERGUNTA: "O monstro consegue ver o FOCO de luz na parede/chão?"
    /// (Checa os PONTOS FINAIS da nuvem de luz do BeamSensor).
    /// </summary>
    public bool CanSeeFlashlightBeam(out Vector3 lightHitPosition)
    {
        // Zera a posição de saída.
        lightHitPosition = Vector3.zero;

        // 1. CHECAGEM BÁSICA
        // Se o 'beamSensor' não existe, ou a luz tá desligada,
        // ou a "nuvem de pontos" ('VisibleLightPoints') tá vazia...
        if (beamSensor == null || !playerFlashlight.enabled || beamSensor.VisibleLightPoints.Count == 0)
        {
            return false; // ...'return false'.
        }

        // 2. LÓGICA DE OTIMIZAÇÃO
        // Vamos checar só alguns pontos da nuvem (ex: 15), pra não pesar o jogo.
        int pontosParaChecar = 15;
        int step = Mathf.Max(1, beamSensor.VisibleLightPoints.Count / pontosParaChecar); // Calcula o "pulo"

        // 3. LOOP DE CHECAGEM (pulando a lista)
        for (int i = 0; i < beamSensor.VisibleLightPoints.Count; i += step)
        {
            // Pega o ponto de luz FINAL da vez.
            Vector3 point = beamSensor.VisibleLightPoints[i];

            // CHAMA A FUNÇÃO "AJUDANTE" (lá embaixo).
            // "Monstro, você consegue ver (linha de visão + ângulo + distância) ESTE ponto específico?"
            if (IsPointVisibleToMonster(point))
            {
                // Se a resposta for 'sim' para QUALQUER UM dos pontos checados...
                lightHitPosition = point; // "Devolve" a posição desse ponto.
                Debug.Log($"<color=cyan>SENSES: FOCO DE LUZ (Sensor) detectado em {lightHitPosition}!</color>", this.gameObject);
                return true; // SIM, ELE VÊ! Para o loop e retorna.
            }
        }

        // Se o loop terminou e ele não viu NENHUM dos pontos checados, retorna 'false'.
        return false;
    }

    /// <summary>
    /// **NOVA FUNÇÃO!**
    /// PERGUNTA: "O monstro percebe algum FACHO de luz CRUZANDO seu campo de visão?"
    /// (Checa PONTOS INTERMEDIÁRIOS dos raios de luz, "no ar").
    /// </summary>
    public bool CanDetectCrossingBeam(out Vector3 detectionPoint)
    {
        // Zera o ponto de saída.
        detectionPoint = Vector3.zero;

        // 1. CHECAGEM BÁSICA
        if (beamSensor == null || !playerFlashlight.enabled || beamSensor.VisibleLightPoints.Count == 0)
        {
            return false;
        }

        // 2. PEGA A POSIÇÃO DA LANTERNA (precisamos dela)
        Vector3 flashlightPos = playerFlashlight.transform.position;

        // 3. LOOP PELOS PONTOS *FINAIS* DA NUVEM
        // Usamos os pontos finais SÓ pra saber a direção/distância de cada raio.
        // (Otimização: checar só alguns raios, não todos?)
        int step = Mathf.Max(1, beamSensor.VisibleLightPoints.Count / 15); // Checa ~15 raios
        for (int i = 0; i < beamSensor.VisibleLightPoints.Count; i += step)
        {
            // Pega o ponto FINAL do raio da vez.
            Vector3 visiblePoint = beamSensor.VisibleLightPoints[i];

            // Calcula a direção e a distância TOTAL deste raio.
            Vector3 directionFromLight = (visiblePoint - flashlightPos).normalized;
            float distanceToVisiblePoint = Vector3.Distance(flashlightPos, visiblePoint);

            // Se o raio for muito curto, não tem o que checar no meio. Pula pro próximo.
            if (distanceToVisiblePoint < (beamSensor.raySpacing * 2)) continue; // Usa 'raySpacing' do BeamSensor

            // 4. LOOP PELOS PONTOS *INTERMEDIÁRIOS* DESTE RAIO
            // Quantos pontos "no meio do caminho" vamos testar? Ex: 5.
            int numChecks = 5;
            for (int k = 1; k <= numChecks; k++)
            {
                // Calcula a posição de um ponto no meio do raio.
                // (k / (float)(numChecks + 1)) = Pega frações da distância total (1/6, 2/6, ... 5/6).
                float fraction = k / (float)(numChecks + 1);
                Vector3 intermediatePoint = flashlightPos + directionFromLight * (distanceToVisiblePoint * fraction);

                // *** A MÁGICA: ***
                // CHAMA A NOSSA NOVA FUNÇÃO AJUDANTE!
                // "Este ponto INTERMEDIÁRIO está DENTRO do cone de visão de LUZ do monstro?"
                // (Essa função só checa ângulo e distância, ignora paredes).
                if (IsPointInViewCone(intermediatePoint))
                {
                    // SE SIM! Ele percebeu o facho cruzando!
                    detectionPoint = intermediatePoint; // Guarda ONDE ele percebeu
                    Debug.Log($"<color=magenta>SENSES: FACHO DE LUZ cruzou a visão perto de {detectionPoint}!</color>", this.gameObject);
                    return true; // Retorna SIM! Para tudo.
                }
            }
        }

        // Se passou por todos os raios e todos os pontos intermediários e não achou nada...
        return false; // Retorna NÃO.
    }


    /// <summary>
    /// FUNÇÃO "AJUDANTE" (privada) para 'CanSeeFlashlightBeam'.
    /// PERGUNTA: "O monstro consegue ver (linha de visão LIMPA + ângulo + distância) ESTE PONTO DE LUZ específico?"
    /// </summary>
    /// <returns>Retorna 'true' (sim) ou 'false' (não).</returns>
    private bool IsPointVisibleToMonster(Vector3 point)
    {
        // 1. CHECAGEM DE DISTÂNCIA (para LUZ)
        float distanceToPoint = Vector3.Distance(transform.position, point);
        if (distanceToPoint > lightDetectionDistance) return false;

        // 2. CHECAGEM DE ÂNGULO (para LUZ)
        Vector3 directionToPoint = (point - transform.position).normalized;
        float angleToPoint = Vector3.Angle(transform.forward, directionToPoint);
        if (angleToPoint > lightDetectionAngle / 2f) return false;

        // 3. CHECAGEM DE OBSTÁCULO (Raycast do Monstro pro Ponto)
        // O truque 'distanceToPoint - 0.1f' evita que o laser bata
        // na PRÓPRIA parede onde o ponto de luz está.
        if (Physics.Raycast(transform.position, directionToPoint, out RaycastHit monsterViewHit, distanceToPoint - 0.1f, visionMask))
        {
            // Se bateu em algo (uma pilastra no meio, etc)...
            return false; // ...'return false'. Visão bloqueada.
        }

        // Se passou em tudo, 'return true' (SIM, ele vê o ponto de luz).
        return true;
    }

    /// <summary>
    /// **NOVA FUNÇÃO AJUDANTE** (privada) para 'CanDetectCrossingBeam'.
    /// PERGUNTA: "Este ponto no espaço está DENTRO do cone de visão de LUZ do monstro?"
    /// (IGNORA obstáculos, só checa geometria: distância e ângulo).
    /// </summary>
    /// <returns>Retorna 'true' (sim) ou 'false' (não).</returns>
    private bool IsPointInViewCone(Vector3 point)
    {
        // 1. CHECAGEM DE DISTÂNCIA (para LUZ)
        float distanceToPoint = Vector3.Distance(transform.position, point);
        if (distanceToPoint > lightDetectionDistance) return false;

        // (Opcional: Adicionar distância mínima? Ex: ignora se muito perto)
        // if (distanceToPoint < 1.0f) return false;

        // 2. CHECAGEM DE ÂNGULO (para LUZ)
        Vector3 directionToPoint = (point - transform.position).normalized;
        float angleToPoint = Vector3.Angle(transform.forward, directionToPoint);
        if (angleToPoint > lightDetectionAngle / 2f) return false;

        // 3. SEM RAYCAST!
        // Se passou na distância e no ângulo, está DENTRO do cone geométrico.
        return true;
    }


    /// <summary>
    /// PERGUNTA: "O monstro consegue OUVIR o player?"
    /// </summary>
    public bool CheckForSound(out Vector3 soundPosition)
    {
        // "Devolve" a posição do player (que é a fonte do som).
        soundPosition = player.position;

        // 1. CHECAGEM BÁSICA
        // Se o script do player não existe OU se o player NÃO ESTÁ SE MOVENDO...
        if (playerController == null || !playerController.IsMoving)
        {
            return false; // ...'return false'. (Player parado não faz barulho).
        }

        // 2. CÁLCULO DO RAIO DE AUDIÇÃO
        // Começa com o raio base (de andar).
        float finalHearingRadius = baseHearingRadius;

        // Se o script do player avisar que ele está CORRENDO ('IsRunning')...
        if (playerController.IsRunning)
        {
            // ...o raio de audição AUMENTA (multiplica).
            finalHearingRadius *= runningSoundMultiplier;
        }

        // 3. CHECAGEM DE DISTÂNCIA
        // Se a distância do monstro até o player for MENOR ou IGUAL
        // ao 'raio de audição' final...
        if (Vector3.Distance(transform.position, soundPosition) <= finalHearingRadius)
        {
            // ...o monstro ouviu!
            Debug.Log($"<color=yellow>SENSES: Som do jogador detectado! Raio: {finalHearingRadius}m</color>", this.gameObject);
            return true; // SIM!
        }

        // Se o player estiver muito longe, 'return false'.
        return false;
    }
}