using UnityEngine;

public class GameOverState : IGameState
{
    public void Enter(GameManager manager)
    {
        Debug.Log("Entering GameOver state");
    }

    public void Execute(GameManager manager)
    {
        // No-op for Phase 1
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting GameOver state");
    }
}
