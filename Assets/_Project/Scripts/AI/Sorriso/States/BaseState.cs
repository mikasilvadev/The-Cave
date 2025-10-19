/// <summary>
/// Este é o "molde" (contrato) para todos os estados da IA.
/// É uma classe abstrata, o que significa que ela não pode ser usada sozinha,
/// ela só serve para forçar outros scripts a terem os métodos que eu definir aqui.
/// Isso organiza a IA, separando o que ela faz (Wander, Chase, Search) em blocos.
/// </summary>
public abstract class BaseState
{
    /// <summary>
    /// Método chamado UMA VEZ quando a IA entra neste estado.
    /// É aqui onde configuro as coisas:
    /// - Setar a velocidade no NavMeshAgent (ex: "correndo" ou "andando").
    /// - Iniciar um timer.
    /// - Definir o primeiro destino.
    /// </summary>
    /// <param name="controller">A referência principal da IA, pra eu poder mexer no Agent, Senses, etc.</param>
    public abstract void Enter(AIController controller);

    /// <summary>
    /// Este é o "Update" do estado. Roda a cada frame enquanto a IA ESTIVER neste estado.
    /// É aqui que fica a lógica principal:
    /// - "Já cheguei no destino?"
    /// - "Estou vendo o player?"
    /// - "Meu timer de espera acabou?"
    /// </summary>
    /// <param name="controller">A referência principal da IA.</param>
    public abstract void Execute(AIController controller);

    /// <summary>
    /// Método chamado UMA VEZ quando a IA sai deste estado (antes de entrar no próximo).
    /// Serve pra "limpar a sujeira":
    /// - Garantir que o Agent possa se mover (se eu mandei ele parar).
    /// - Resetar o caminho (Agent.ResetPath()).
    /// - Parar alguma corrotina, se precisar.
    /// </summary>
    /// <param name="controller">A referência principal da IA.</param>
    public abstract void Exit(AIController controller);
}