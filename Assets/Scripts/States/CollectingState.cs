using UnityEngine;

public class CollectingState : IGameState
{
    public void Enter(GameManager manager)
    {
        // Per user decision: gameplay view stays visible during collecting
        Debug.Log("Entering Collecting state");
    }

    public void Execute(GameManager manager)
    {
        // No-op for Phase 1. In future phases, this waits for minerals to fly to ship.
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting Collecting state");
    }
}
