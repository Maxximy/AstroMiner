using UnityEngine;

public class PlayingState : IGameState
{
    public void Enter(GameManager manager)
    {
        Debug.Log("Entering Playing state");
    }

    public void Execute(GameManager manager)
    {
        // No-op for Phase 1 (gameplay systems not yet built)
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting Playing state");
    }
}
