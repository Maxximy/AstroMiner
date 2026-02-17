using UnityEngine;

public class GameOverState : IGameState
{
    public void Enter(GameManager manager)
    {
        Debug.Log("Entering GameOver state");

        // Auto-save credits at end of every run (SAVE-02)
        SaveManager.Instance?.AutoSave();

        var resultsScreen = Object.FindAnyObjectByType<ResultsScreen>(FindObjectsInactive.Include);
        if (resultsScreen != null)
        {
            resultsScreen.Show();
        }
        else
        {
            Debug.LogWarning("GameOverState: ResultsScreen not found");
        }
    }

    public void Execute(GameManager manager)
    {
        // No-op: waiting for UI button (Continue)
    }

    public void Exit(GameManager manager)
    {
        var resultsScreen = Object.FindAnyObjectByType<ResultsScreen>(FindObjectsInactive.Include);
        if (resultsScreen != null)
        {
            resultsScreen.Hide();
        }

        Debug.Log("Exiting GameOver state");
    }
}
