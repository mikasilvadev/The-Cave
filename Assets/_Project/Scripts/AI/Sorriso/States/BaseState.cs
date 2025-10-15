// Scripts/Monstro/States/BaseState.cs

public abstract class BaseState
{
    // Chamado quando o estado é ativado
    public abstract void Enter(AIController controller);

    // Chamado a cada frame enquanto o estado está ativo
    public abstract void Execute(AIController controller);

    // Chamado quando o estado é desativado
    public abstract void Exit(AIController controller);
}