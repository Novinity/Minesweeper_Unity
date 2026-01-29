using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager instance {get; private set;}

    [SerializeField] private TMP_Text lobbyNameText, lobbyCodeText;

    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private GameObject startButton, gameOptions;

    List<PlayerListEntry> playerListEntries = new List<PlayerListEntry>();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (!GameNetworkManager.instance)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            GameNetworkManager.instance.SpawnPlayers();
            startButton.GetComponent<Button>().interactable = true;
            GameNetworkManager.instance.ToggleLobbyLock(false);
        } else
        {
            startButton.GetComponent<Button>().interactable = false;
        
            GameOptionHandler.instance.gridSizeXInput.interactable = false;
            GameOptionHandler.instance.gridSizeYInput.interactable = false;
            GameOptionHandler.instance.bombPercentageInput.interactable = false;
            GameOptionHandler.instance.startPosToggle.interactable = false;
        }
        if (GameNetworkManager.instance.isSingleplayer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        } else
        {
            GameNetworkManager.instance.players.CollectionChanged += PlayerCollectionChanged_Callback;
            lobbyNameText.text = GameNetworkManager.instance.m_Lobby.Name;
            lobbyCodeText.text = $"Code: {GameNetworkManager.instance.m_Lobby.LobbyCode}";
            foreach (Player player in GameNetworkManager.instance.players)
            {
                AddPlayerItem(player);
            }
        }

        if (IsHost) UpdateGameOptions();
        GameOptionHandler.instance.gridSizeXInput.text = GameOptionPersistence.gridX.ToString();
        GameOptionHandler.instance.gridSizeYInput.text = GameOptionPersistence.gridY.ToString();
        GameOptionHandler.instance.bombPercentageInput.text = GameOptionPersistence.bombPercentage.ToString();
        GameOptionHandler.instance.startPosToggle.isOn = GameOptionPersistence.startPos;
    }

    private void PlayerCollectionChanged_Callback(object sender, NotifyCollectionChangedEventArgs args)
    {
        RemoveUnnecessaryPlayerItems();
        if (args.NewItems == null || args.NewItems.Count == 0) return;
        foreach (Player newPlayer in args.NewItems)
        {
            AddPlayerItem(newPlayer);
        }

        if (IsHost)
        {
            UpdateGameOptions();
        }
    }

    void OnDestroy()
    {
        if (GameNetworkManager.instance) GameNetworkManager.instance.players.CollectionChanged -= PlayerCollectionChanged_Callback;
    }

    public void Leave()
    {
        GameNetworkManager.instance.LeaveGame();
    }

    private void AddPlayerItem(Player player)
    {
        PlayerListEntry playerListEntry = Instantiate(playerEntryPrefab, playerListContent).GetComponent<PlayerListEntry>();

        playerListEntry.playerName = player.PlayerName.Value.ToString();
        playerListEntry.playerId = player.OwnerClientId;
        playerListEntry.player = player;
        playerListEntry.isHost = player.IsOwnedByServer;

        playerListEntry.Setup();

        playerListEntries.Add(playerListEntry);
    }

    public void StartGame()
    {
        if (!GameNetworkManager.instance.IsHost) return;
        UpdateGameOptions();
        GameNetworkManager.instance.SceneManager.LoadScene("s2_Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void RemoveUnnecessaryPlayerItems()
    {
        List<PlayerListEntry> toRemove = new List<PlayerListEntry>();
        List<Player> players = new List<Player>(GameNetworkManager.instance.players);
        List<ulong> playerIds = new List<ulong>();
        foreach (Player player in players)
        {
            playerIds.Add(player.OwnerClientId);
        }

        foreach (PlayerListEntry entry in playerListEntries)
        {
            if (!playerIds.Contains(entry.playerId) || entry.player == null)
            {
                toRemove.Add(entry);
            }
        }
        foreach (PlayerListEntry entry in toRemove)
        {
            playerListEntries.Remove(entry);
            Destroy(entry.gameObject);
        }
    }

    public void UpdateGameOptions()
    {
        Debug.Log("Updating game options");
        UpdateGameOptions_Rpc(GameOptionPersistence.gridX, GameOptionPersistence.gridY, GameOptionPersistence.bombPercentage, GameOptionPersistence.startPos);
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateGameOptions_Rpc(int gridX, int gridY, float bombPercentage, bool startPos)
    {
        Debug.Log("Received game option update");
        GameOptionPersistence.gridX = gridX;
        GameOptionPersistence.gridY = gridY;
        GameOptionPersistence.bombPercentage = bombPercentage;
        GameOptionPersistence.startPos = startPos;

        if (!IsHost)
        {
            if (GameOptionHandler.instance.gridSizeXInput.text != gridX.ToString()) GameOptionHandler.instance.gridSizeXInput.text = gridX.ToString();
            if (GameOptionHandler.instance.gridSizeYInput.text != gridY.ToString()) GameOptionHandler.instance.gridSizeYInput.text = gridY.ToString();
            if (GameOptionHandler.instance.bombPercentageInput.text != bombPercentage.ToString()) GameOptionHandler.instance.bombPercentageInput.text = bombPercentage.ToString();
            if (GameOptionHandler.instance.startPosToggle.isOn != startPos) GameOptionHandler.instance.startPosToggle.isOn = startPos;
        }
    }
}
