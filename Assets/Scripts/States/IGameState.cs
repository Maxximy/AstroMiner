public interface IGameState
{
    void Enter(GameManager manager);
    void Execute(GameManager manager);
    void Exit(GameManager manager);
}
