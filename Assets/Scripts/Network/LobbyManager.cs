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
    [SerializeField] private TMP_InputField gridSizeXInput, gridSizeYInput, bombPercentageInput;

    List<PlayerListEntry> playerListEntries = new List<PlayerListEntry>();

    string prevBombPercent = "";

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
        
            gridSizeXInput.interactable = false;
            gridSizeYInput.interactable = false;
            bombPercentageInput.interactable = false;
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
        gridSizeXInput.text = GameOptionPersistence.gridX.ToString();
        gridSizeYInput.text = GameOptionPersistence.gridY.ToString();
        bombPercentageInput.text = GameOptionPersistence.bombPercentage.ToString();
    }

    private void PlayerCollectionChanged_Callback(object sender, NotifyCollectionChangedEventArgs args)
    {
        RemoveUnnecessaryPlayerItems();
        if (args.NewItems == null || args.NewItems.Count == 0) return;
        foreach (Player newPlayer in args.NewItems)
        {
            AddPlayerItem(newPlayer);
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
        int gridX = 10;
        int gridY = 10;
        float bombPercentage = 12.5f;
        try
        {
            gridX = int.Parse(gridSizeXInput.text);
            gridY = int.Parse(gridSizeYInput.text);
            bombPercentage = float.Parse(bombPercentageInput.text);
        } catch (Exception e)
        {
            if (prevBombPercent != "")
            {
                bombPercentage = float.Parse(prevBombPercent);
            } else
            {
                bombPercentage = 12.5f;
            }
        }

        if (gridX > 100) gridX = 100;
        if (gridY > 100) gridY = 100;
        if (bombPercentage > 99) bombPercentage = 99;

        if (gridX < 5) gridX = 5;
        if (gridY < 5) gridY = 5;
        if (bombPercentage < 1) bombPercentage = 1;

        if (gridSizeXInput.text != gridX.ToString()) gridSizeXInput.text = gridX.ToString();
        if (gridSizeYInput.text != gridY.ToString()) gridSizeYInput.text = gridY.ToString();
        if (bombPercentageInput.text != bombPercentage.ToString()) bombPercentageInput.text = bombPercentage.ToString();

        prevBombPercent = bombPercentageInput.text;

        UpdateGameOptions_Rpc(gridX, gridY, bombPercentage);
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateGameOptions_Rpc(int gridX, int gridY, float bombPercentage)
    {
        Debug.Log("Received game option update");
        GameOptionPersistence.gridX = gridX;
        GameOptionPersistence.gridY = gridY;
        GameOptionPersistence.bombPercentage = bombPercentage;

        if (!IsHost)
        {
            if (gridSizeXInput.text != gridX.ToString()) gridSizeXInput.text = gridX.ToString();
            if (gridSizeYInput.text != gridY.ToString()) gridSizeYInput.text = gridY.ToString();
            if (bombPercentageInput.text != bombPercentage.ToString()) bombPercentageInput.text = bombPercentage.ToString();
        }
    }
}
