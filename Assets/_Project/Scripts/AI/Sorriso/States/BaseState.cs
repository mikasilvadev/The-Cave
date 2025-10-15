public abstract class BaseState
{
    public abstract void Enter(AIController controller);

    public abstract void Execute(AIController controller);

    public abstract void Exit(AIController controller);
}