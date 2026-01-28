// using System;
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
    public NetworkVariable<long> gameStartTime = new NetworkVariable<long>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public bool hasLost = false;

    [SerializeField] private GameObject winScreen, loseScreen, winScreenM, loseScreenM, pauseScreen, waitingForPlayersScreen, countdownScreen;
    [SerializeField] private GameObject[] endEarlyButtons;
    [SerializeField] private TMP_Text timerText, finalTimeText, finalTimeText2, flagsLeftText, countdownText, winnerText;
    [SerializeField] private Button loseButtonM, winButtonM;

    [SerializeField] private GameObject opponentTilesLeftPrefab, opponentTilesLeftList;

    private float time;

    public NetworkVariable<long> seed = new NetworkVariable<long>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    List<Player> opponents = new List<Player>();
    List<TMP_Text> opponentTileTexts = new List<TMP_Text>();

    InputSystem_Actions inputActions;

    bool resetTriggeredByKey = false;
    public bool chording = false;

    private bool leftClickHeld, rightClickHeld;

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
            if (!gameStarted.Value) return;
            TogglePause(!pauseScreen.activeSelf);
        };
        inputActions.Player.Reset.performed += delegate
        {
            if (!gameStarted.Value) return;
            resetTriggeredByKey = true;
            Reset();
        };

        inputActions.Player.Attack.performed += delegate
        {
            leftClickHeld = true;
            if (rightClickHeld)
            {
                chording = true;
                CheckChord();
            }
        };
        inputActions.Player.Attack.canceled += delegate
        {
            leftClickHeld = false;
            chording = false;
        };

        inputActions.Player.ADS.performed += delegate
        {
            rightClickHeld = true;
            if (leftClickHeld)
            {
                chording = true;
                CheckChord();
            }
        };
        inputActions.Player.ADS.canceled += delegate
        {
            rightClickHeld = false;
            chording = false;
        };

        inputActions.Enable();
    }

    private void CheckChord()
    {
        foreach (Cell cell in GridGenerator.instance.cells)
        {
            if (cell.hovering)
            {
                cell.CheckChordSurroundings();
                break;
            }
        }
    }

    public void CellHoverUpdate()
    {
        if (chording)
        {
            foreach (Cell cell in GridGenerator.instance.cells)
            {
                bool foundChording = false;
                List<Cell> flaggedSurrounding = new List<Cell>();
                List<Cell> surrounding = new List<Cell>();

                for (int x = cell.coord.x - 1; x <= cell.coord.x + 1; x++)
                {
                    for (int y = cell.coord.y - 1; y <= cell.coord.y + 1; y++)
                    {
                        Cell cell2 = GridGenerator.instance.getCellAtPosition(x, y);
                        if (cell2)
                        {
                            if (cell2.hovering) foundChording = true;
                            if (cell2.isFlagged) flaggedSurrounding.Add(cell2);
                            surrounding.Add(cell2);
                        }
                    }
                }

                if (foundChording)
                {
                    cell.HoverFX();
                }
                else if (!cell.hovering) cell.UnhoverFX();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            GameNetworkManager.instance.ToggleLobbyLock(true);
            GameNetworkManager.instance.SpawnPlayers();

            seed.Value = Random.Range(-1000000, 100001);
            GridGenerator.instance.Generate();
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
                player.BombsRemaining.OnValueChanged += OnOpponentBombsRemainingChanged;
                opponents.Add(player);

                GameObject txt = Instantiate(opponentTilesLeftPrefab, opponentTilesLeftList.transform);
                txt.name = player.OwnerClientId.ToString();
                opponentTileTexts.Add(txt.GetComponent<TMP_Text>());
            }
        }

        if (GameNetworkManager.instance.isSingleplayer || opponents.Count == 0) opponentTilesLeftList.SetActive(false);
        else OnOpponentBombsRemainingChanged(0,0);

        GameNetworkManager.instance.OnClientDisconnectCallback += OnClientDisconnect_Callback;

        StartCoroutine(WaitForAllLoaded());
    }

    private void OnClientDisconnect_Callback(ulong clientId)
    {
        TMP_Text textToDelete = null;
        for (int i = 0; i < opponentTileTexts.Count; i++)
        {
            if (clientId.ToString() == opponentTileTexts[i].name)
            {
                textToDelete = opponentTileTexts[i];
                break;
            }
        }
        if (textToDelete)
        {
            opponentTileTexts.Remove(textToDelete);
            Destroy(textToDelete.gameObject);
        }
    }

    private void OnOpponentBombsRemainingChanged(int previousValue, int newValue)
    {
        for (int i = 0; i < opponents.Count; i++)
        {
            opponentTileTexts[i].text = $"{opponents[i].PlayerName.Value} Bombs Left: {opponents[i].BombsRemaining.Value}";
        }
    }

    public override void OnNetworkDespawn()
    {
        GameNetworkManager.instance.OnClientDisconnectCallback -= OnClientDisconnect_Callback;
        foreach (Player opponent in opponents)
        {
            opponent.BombsRemaining.OnValueChanged -= OnOpponentBombsRemainingChanged;
        }
    }

    void Update()
    {
        if (gameStarted.Value && !gameEnded.Value && !hasLost) {
            if (!pauseScreen.activeSelf || !GameNetworkManager.instance.isSingleplayer)
                time += Time.deltaTime / 60;
        }
        timerText.text = TimeUtils.formatTime(time);
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
        finalTimeText.text = $"Your time: {TimeUtils.formatTime(time)}";
        finalTimeText2.text = $"Your time: {TimeUtils.formatTime(time)}";

        SaveLoad.SaveHighScore(GameOptionPersistence.gridX, GameOptionPersistence.gridY, GameOptionPersistence.bombPercentage, time);

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
        else
        {
            foreach (Player player in GameNetworkManager.instance.players)
            {
                if (player.AuthID.Value == winner)
                {
                    winnerText.text = $"{player.PlayerName.Value} Wins!";
                    break;
                }
            }
            StartCoroutine(Coro_TriggerEnd(false));
        }
    }

    public void CheckCells()
    {
        int nonBombsLeft = 0;
        int bombsFound = 0;
        int flagsLeft = GridGenerator.instance.bombCount;
        int cellsTouched = 0;
        bool hasTouchedACell = false;

        Cell startPos = null;
        foreach (Cell cell in GridGenerator.instance.cells)
        {
            if (!cell.isBomb && !cell.isTriggered) nonBombsLeft++;
            if (cell.isBomb && cell.isFlagged) bombsFound++;
            if (cell.isFlagged) flagsLeft--;
            if (cell.isTriggered || cell.isFlagged) cellsTouched++;

            if (cell.curImg == "safe") startPos = cell;
        }

        if (startPos != null && cellsTouched != 0)
        {
            startPos.SetImage("");
        }
        Player.LocalPlayer.BombsRemaining.Value = GridGenerator.instance.bombCount - bombsFound;

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
        if (hasLost || gameEnded.Value)
        {
            TogglePause(false);
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
    }

    public void Reset()
    {
        if (GameNetworkManager.instance.isSingleplayer)
        {
            seed.Value = Random.Range(-1000000, 100001);
            time = 0;
        }

        TogglePause(false);
        GridGenerator.instance.Generate();
        UnLose();
        CheckCells();
    }

    public void ReturnToLobby()
    {
        if (IsHost) GameNetworkManager.instance.SceneManager.LoadScene("s1_Lobby", LoadSceneMode.Single);
    }

    private void UnLose()
    {
        hasLost = false;
        loseScreen.SetActive(false);
        winScreen.SetActive(false);
        if (GameNetworkManager.instance.isSingleplayer)
        {
            gameStarted.Value = false;
            gameEnded.Value = false;
            Player.LocalPlayer.Loaded.Value = true;
            if (!resetTriggeredByKey)
            {
                StartCoroutine(WaitForAllLoaded());
            } else
            {
                resetTriggeredByKey = false;
                gameStarted.Value = true;
            }
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit!");
        GameNetworkManager.instance.LeaveGame();
    }

    public void TogglePause(bool val)
    {
        pauseScreen.SetActive(val);
    }

    IEnumerator WaitForAllLoaded()
    {
        waitingForPlayersScreen.SetActive(true);
        while (true)
        {
            if (IsHost)
            {
                int loaded = 0;
                foreach (Player player in GameNetworkManager.instance.players)
                {
                    if (player.Loaded.Value)
                    {
                        loaded++;
                    }
                }
                if (loaded != GameNetworkManager.instance.players.Count) yield return new WaitForEndOfFrame();
                else break;
            } else
            {
                if (gameStartTime.Value < 0) yield return new WaitForEndOfFrame();
                else break;
            }
        }

        gameStartTime.Value = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        waitingForPlayersScreen.SetActive(false);
        countdownScreen.SetActive(true);
        
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(1);
        }

        countdownText.text = "GO!";
        yield return new WaitForSeconds(1);
        countdownScreen.SetActive(false);

        if (IsHost) gameStarted.Value = true;
    }
}
