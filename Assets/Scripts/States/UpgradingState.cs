using UnityEngine;

public class UpgradingState : IGameState
{
    public void Enter(GameManager manager)
    {
        Debug.Log("Entering Upgrading state");

        var upgradeScreen = Object.FindAnyObjectByType<UpgradeScreen>(FindObjectsInactive.Include);
        if (upgradeScreen != null)
        {
            upgradeScreen.Show();
        }
        else
        {
            Debug.LogWarning("UpgradingState: UpgradeScreen not found");
        }
    }

    public void Execute(GameManager manager)
    {
        // No-op: waiting for UI button (Start Run)
    }

    public void Exit(GameManager manager)
    {
        var upgradeScreen = Object.FindAnyObjectByType<UpgradeScreen>(FindObjectsInactive.Include);
        if (upgradeScreen != null)
        {
            upgradeScreen.Hide();
        }

        Debug.Log("Exiting Upgrading state");
    }
}
