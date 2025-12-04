using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance {get; private set;}

    public NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool hasLost = false;

    [SerializeField] private GameObject winScreen, loseScreen, winScreenM, loseScreenM, pauseScreen;
    [SerializeField] private GameObject[] endEarlyButtons;
    [SerializeField] private TMP_Text timerText, finalTimeText, finalTimeText2, flagsLeftText;
    [SerializeField] private Button loseButtonM, winButtonM;

    [SerializeField] private GameObject opponentTilesLeftPrefab, opponentTilesLeftList;

    private float time;

    public NetworkVariable<long> seed = new NetworkVariable<long>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    List<Player> opponents = new List<Player>();
    List<TMP_Text> opponentTileTexts = new List<TMP_Text>();

    InputSystem_Actions inputActions;

    void OnEnable()
    {
        if (inputActions != null) inputActions.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        inputActions = new InputSystem_Actions();

        inputActions.Player.Pause.performed += delegate
        {
            TogglePause(!pauseScreen.activeSelf);
        };

        inputActions.Enable();
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            GameNetworkManager.instance.SpawnPlayers();

            seed.Value = Random.Range(-1000000, 100001);
            GridGenerator.instance.Generate();
            gameStarted.Value = true;
        } else
        {
            if (seed.Value != 0)
            {
                GridGenerator.instance.Generate();
            } else
            {
                seed.OnValueChanged += delegate
                {
                    GridGenerator.instance.Generate();
                };
            }
        }
        if (!IsHost || GameNetworkManager.instance.isSingleplayer)
        {
            foreach (GameObject btn in endEarlyButtons)
            {
                btn.SetActive(false);
            }
        }

        foreach (Player player in GameNetworkManager.instance.players)
        {
            if (player.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            {
                player.TilesLeft.OnValueChanged += OnOpponentTilesChanged;
                opponents.Add(player);

                GameObject txt = Instantiate(opponentTilesLeftPrefab, opponentTilesLeftList.transform);
                opponentTileTexts.Add(txt.GetComponent<TMP_Text>());
            }
        }

        if (GameNetworkManager.instance.isSingleplayer || opponents.Count == 0) opponentTilesLeftList.SetActive(false);
        else OnOpponentTilesChanged(0,0);
        
    }

    private void OnOpponentTilesChanged(int previousValue, int newValue)
    {
        for (int i = 0; i < opponents.Count; i++)
        {
            opponentTileTexts[i].text = $"{opponents[i].PlayerName.Value} Tiles Left: {opponents[i].TilesLeft.Value}";
        }
    }

    public override void OnNetworkDespawn()
    {
        foreach (Player opponent in opponents)
        {
            opponent.TilesLeft.OnValueChanged -= OnOpponentTilesChanged;
        }
    }

    void Update()
    {
        if (gameStarted.Value && !gameEnded.Value && !hasLost) {
            if (!pauseScreen.activeSelf || !GameNetworkManager.instance.isSingleplayer)
                time += Time.deltaTime / 60;
        }
        timerText.text = formatTime(time);
    }

    public void LoseGame()
    {
        if (!gameStarted.Value || gameEnded.Value) return;
        if (GameNetworkManager.instance.isSingleplayer)
        {
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
        }
        hasLost = true;
        StartCoroutine(Coro_TriggerEnd(false));
    }

    public void WinGame()
    {
        if (!gameStarted.Value || gameEnded.Value) return;
        finalTimeText.text = formatTime(time);
        finalTimeText2.text = formatTime(time);
        WinGame_Rpc(AuthenticationService.Instance.PlayerId);
    }

    [Rpc(SendTo.Server)]
    public void WinGame_Rpc(FixedString512Bytes winner)
    {
        if (!gameStarted.Value || gameEnded.Value) return;
        gameEnded.Value = true;
        if (GameNetworkManager.instance.isSingleplayer) StartCoroutine(Coro_TriggerEnd(true));
        BroadcastWin_Rpc(winner);
    }

    [Rpc(SendTo.Everyone)]
    public void BroadcastWin_Rpc(FixedString512Bytes winner)
    {
        if (winner == AuthenticationService.Instance.PlayerId) StartCoroutine(Coro_TriggerEnd(true));
        else StartCoroutine(Coro_TriggerEnd(false));
    }

    public void CheckCells()
    {
        int nonBombsLeft = 0;
        int flagsLeft = GridGenerator.instance.bombCount;
        int cellsTouched = 0;
        foreach (Cell cell in GridGenerator.instance.cells)
        {
            if (!cell.isBomb && !cell.isTriggered) nonBombsLeft++;
            if (cell.isFlagged) flagsLeft--;
            if (cell.isTriggered || cell.isFlagged) cellsTouched++;

            if (cell.curImg == "safe") cell.SetImage("");
        }
        Player.LocalPlayer.FlagsLeft.Value = flagsLeft;
        Player.LocalPlayer.TilesLeft.Value = GridGenerator.instance.map.Length - cellsTouched;

        UpdateFlagCount(flagsLeft);
        if (nonBombsLeft == 0) WinGame();
    }

    public void UpdateFlagCount(int amt)
    {
        flagsLeftText.text = $"{amt}";
    }

    IEnumerator Coro_TriggerEnd(bool won)
    {
        yield return new WaitForSeconds(2);
        if (GameNetworkManager.instance.isSingleplayer)
        {
            if (won) winScreen.SetActive(true);
            else loseScreen.SetActive(true);
        } else
        {
            if (won) winScreenM.SetActive(true);
            else if (gameEnded.Value) loseScreenM.SetActive(true);
            else loseScreen.SetActive(true);
            if (gameEnded.Value)
            {
                if (IsHost)
                {
                    winButtonM.interactable = true;
                    loseButtonM.interactable = true;
                } else
                {
                    winButtonM.interactable = false;
                    loseButtonM.interactable = false;
                }
            }
        }
    }

    public void Reset()
    {
        if (GameNetworkManager.instance.isSingleplayer)
        {
            seed.Value = Random.Range(-1000000, 100001);
            time = 0;
        }

        GridGenerator.instance.Generate();
        UnLose();
    }

    public void ReturnToLobby()
    {
        if (IsHost) GameNetworkManager.instance.SceneManager.LoadScene("s1_Lobby", LoadSceneMode.Single);
    }

    private void UnLose()
    {
        hasLost = false;
        loseScreen.SetActive(false);
    }

    public void QuitGame()
    {
        GameNetworkManager.instance.LeaveGame();
    }

    private string formatTime(float time)
    {
        float curTime = time;
        int hour = Mathf.FloorToInt(curTime);
        if (hour > 12) hour -= 12;
        int minutes = Mathf.FloorToInt(60 * (curTime - Mathf.FloorToInt(curTime)));
        return $"{hour}:{minutes:00}";
    }

    public void TogglePause(bool val)
    {
        pauseScreen.SetActive(val);
    }
}
