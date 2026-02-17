using UnityEngine;

public class UpgradingState : IGameState
{
    public void Enter(GameManager manager)
    {
        // Per user decision: full-screen takeover (not overlay)
        Debug.Log("Entering Upgrading state");
    }

    public void Execute(GameManager manager)
    {
        // No-op for Phase 1
    }

    public void Exit(GameManager manager)
    {
        Debug.Log("Exiting Upgrading state");
    }
}
