using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance {get; private set;}

    public bool gameStarted = false;
    public bool gameEnded = false;

    [SerializeField] private GameObject winScreen, loseScreen;
    [SerializeField] private TMP_Text timerText, finalTimeText, flagsLeftText;

    private float time;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (gameStarted && !gameEnded) time += Time.deltaTime / 60;
        timerText.text = formatTime(time);
    }

    public void LoseGame()
    {
        if (!gameStarted || gameEnded) return;
        foreach (Cell cell in GridGenerator.instance.cells)
        {
            if (cell.isBomb && !cell.isFlagged)
            {
                cell.SetImage("bomb");
            } else if (!cell.isBomb && cell.isFlagged)
            {
                cell.SetImage("x");
            }
        }
        gameEnded = true;
        StartCoroutine(Coro_TriggerEnd(false));
    }

    public void WinGame()
    {
        if (!gameStarted || gameEnded) return;
        gameEnded = true;
        finalTimeText.text = formatTime(time);
        StartCoroutine(Coro_TriggerEnd(true));
    }

    public void CheckCells()
    {
        int nonBombsLeft = 0;
        int flagsLeft = GridGenerator.instance.bombCount;
        foreach (Cell cell in GridGenerator.instance.cells)
        {
            if (!cell.isBomb && !cell.isTriggered) nonBombsLeft++;
            if (cell.isFlagged) flagsLeft--;
        }

        UpdateFlagCount(flagsLeft);
        if (nonBombsLeft == 0) WinGame();
    }

    public void UpdateFlagCount(int amt)
    {
        flagsLeftText.text = $"Flags Left: {amt}";
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

    private string formatTime(float time)
    {
        float curTime = time;
        int hour = Mathf.FloorToInt(curTime);
        if (hour > 12) hour -= 12;
        int minutes = Mathf.FloorToInt(60 * (curTime - Mathf.FloorToInt(curTime)));
        return $"{hour}:{minutes:00}";
    }
}
