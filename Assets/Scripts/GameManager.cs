using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance {get; private set;}

    public bool gameStarted = false;
    public bool gameEnded = false;

    [SerializeField] private GameObject winScreen, loseScreen;

    void Awake()
    {
        instance = this;
    }

    public void LoseGame()
    {
        if (!gameStarted || gameEnded) return;
        gameEnded = true;
        StartCoroutine(Coro_TriggerEnd(false));
    }

    public void WinGame()
    {
        if (!gameStarted || gameEnded) return;
        gameEnded = true;
        StartCoroutine(Coro_TriggerEnd(true));
    }

    public void CheckCells()
    {
        int nonBombsLeft = 0;
        foreach (Cell cell in GridGenerator.instance.cells)
        {
            if (!cell.isBomb && !cell.isTriggered) nonBombsLeft++;
        }

        if (nonBombsLeft == 0) WinGame();
    }

    IEnumerator Coro_TriggerEnd(bool won)
    {
        yield return new WaitForSeconds(2);
        if (won) winScreen.SetActive(true);
        else loseScreen.SetActive(true);
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit!");
    }
}
