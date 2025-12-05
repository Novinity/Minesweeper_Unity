using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEditor;
using UnityEngine;

public class GameNetworkManager : NetworkManager
{
    public static GameNetworkManager instance { get; private set; }

    public ObservableCollection<Player> players = new ObservableCollection<Player>();

    private const int HARD_PLAYER_CAP = 8;
    public int MaxPlayers = 8;

    public bool isConnected { get; private set; }
    public bool isSingleplayer { get; private set; }

    GNMCompanion companion;

    public Lobby m_Lobby;

    public List<Lobby> lobbies = new List<Lobby>();

    LobbyEventCallbacks lobbyEventCallbacks;

    public bool initializedUS = false;

    public enum LobbyType
    {
        PUBLIC,
        PRIVATE
    }

    void Awake()
    {
        companion = GetComponent<GNMCompanion>();
    }

    async void Start()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        await UnityServices.InitializeAsync();

        if (UnityServices.Instance.State == ServicesInitializationState.Initialized)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            if (!PlayerPrefs.HasKey("PlayerName"))
            {
                string newName = "Player-" + UnityEngine.Random.Range(1000, 10000).ToString();
                PlayerPrefs.SetString("PlayerName", newName);
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            }
        }
        initializedUS = true;
        if (MenuUIManager.instance)
        {
            MenuUIManager.instance.UpdatePlayerName();
            MenuUIManager.instance.ToggleLoadingScreen(false);
        }

        OnServerStarted += OnServerStarted_Callback;
        OnServerStopped += OnServerStopped_Callback;
        OnClientStarted += OnClientStarted_Callback;
        OnClientStopped += OnClientStopped_Callback;
        OnClientConnectedCallback += OnClientConnected_Callback;
        OnClientDisconnectCallback += OnClientDisconnect_Callback;
        ConnectionApprovalCallback += ApprovalCheck;
    }

    private void OnClientConnected_Callback(ulong clientId)
    {
        if (IsServer && clientId != ServerClientId)
        {
            SpawnNewPlayer(clientId);
        }
    }

    private void OnClientDisconnect_Callback(ulong clientId)
    {
        Player targetPlayer = null;
        foreach (Player player in players)
        {
            if (player.OwnerClientId == clientId)
            {
                targetPlayer = player;
            }
        }
        if (!targetPlayer) return;

        if (IsServer) Destroy(targetPlayer);
        players.Remove(targetPlayer);
    }

    void OnDestroy()
    {
        if (instance != this) return;
        if (m_Lobby != null) LobbyService.Instance.RemovePlayerAsync(m_Lobby.Id, AuthenticationService.Instance.PlayerId);

        OnServerStarted -= OnServerStarted_Callback;
        OnServerStopped -= OnServerStopped_Callback;
        OnClientStarted -= OnClientStarted_Callback;
        OnClientStopped -= OnClientStopped_Callback;
        OnClientConnectedCallback -= OnClientConnected_Callback;
        OnClientDisconnectCallback -= OnClientDisconnect_Callback;
        ConnectionApprovalCallback -= ApprovalCheck;

        instance = null;
    }

    public void Host(bool singleplayer = true)
    {
        ToggleSingleplayer(singleplayer);
        if (singleplayer) StartHost();
    }

    public async void CreateLobby(LobbyType lobbyType = LobbyType.PUBLIC, int maxPlayers = 2)
    {
        if (maxPlayers > HARD_PLAYER_CAP) maxPlayers = HARD_PLAYER_CAP;
        ToggleSingleplayer(false);
        if (MenuUIManager.instance) MenuUIManager.instance.ToggleLoadingScreen(true);

        string lobbyName = PlayerPrefs.GetString("PlayerName") + "'s Lobby";
        CreateLobbyOptions options = new CreateLobbyOptions();
        options.IsPrivate = lobbyType == LobbyType.PRIVATE;

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections: maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        options.Data = new Dictionary<string, DataObject>()
        {
            { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }  
        };

        companion.unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        m_Lobby = lobby;

        StartCoroutine(HeartbeatLobbyCoroutine(1));

        // lobbyEventCallbacks = new LobbyEventCallbacks();
        // lobbyEventCallbacks.PlayerJoined += (List<LobbyPlayerJoined> playersJoined) =>
        // {
        //     foreach (LobbyPlayerJoined lobbyPlayerJoined in playersJoined)
        //     {
        //         Debug.Log(lobbyPlayerJoined.Player.Profile.Name);
        //     }  
        // };


        StartHost();
    }

    private void ApprovalCheck(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
    {
        Debug.Log("approval requested");
        if (request.ClientNetworkId == ServerClientId)
        {
            response.Approved = true;
            return;
        }
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        if ((ConnectedClients.Count >= MaxPlayers && request.Payload == null) || !LobbyManager.instance)
        {
            response.Approved = false;
            response.Reason = "Server is full.";
            Debug.Log("full server");
            return;
        }
        if (GameManager.instance)
        {
            response.Approved = false;
            response.Reason = "Game has already started.";
            Debug.Log("game already started");
            return;
        }

        Debug.Log("approved!");
        response.Approved = true;
    }

    private void OnServerStarted_Callback()
    {
        if (isSingleplayer)
        {
            SceneManager.LoadScene("s2_Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene("s1_Lobby", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnServerStopped_Callback(bool obj)
    {
        players.Clear();
        isConnected = false;
        if (m_Lobby != null)
        {
            if (m_Lobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                LobbyService.Instance.DeleteLobbyAsync(m_Lobby.Id);
            }
            else
            {
                LobbyService.Instance.RemovePlayerAsync(m_Lobby.Id, AuthenticationService.Instance.PlayerId);
            }
            OnKickedFromLobby();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("s0_MainMenu");
    }

    private void OnClientStarted_Callback()
    {
        isConnected = true;
    }

    private void OnClientStopped_Callback(bool obj)
    {
        Debug.Log($"Client stopped. Reason: {obj}");
        players.Clear();
        if (m_Lobby != null) LobbyService.Instance.RemovePlayerAsync(m_Lobby.Id, AuthenticationService.Instance.PlayerId);
        m_Lobby = null;
        isConnected = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("s0_MainMenu");
    }

    private async Task OnLobbyEntered(Lobby lobby)
    {
        Debug.Log("lobby entered");
        if (!IsHost)
        {
            StartCoroutine(LobbyTimeoutCheck_Coro());
            Debug.Log("not host");
            m_Lobby = lobby;

            isSingleplayer = false;
            companion.singlePlayerTransport.enabled = false;
            companion.unityTransport.enabled = true;
            NetworkConfig.NetworkTransport = companion.unityTransport;
            ToggleSingleplayer(false);

            bool gotJoinCode = lobby.Data.TryGetValue("RelayCode", out DataObject joinCodeObject);
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCodeObject.Value);
            companion.unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "udp"));

            if (!gotJoinCode || joinCodeObject.Value == null)
            {
                LeaveGame();
                return;
            }

            StartClient();
        }
    }

    void OnApplicationQuit()
    {
        if (m_Lobby != null) LobbyService.Instance.RemovePlayerAsync(m_Lobby.Id, AuthenticationService.Instance.PlayerId);
    }

    public async Task GetLobbiesList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            options.Filters = new List<QueryFilter>()
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"
                ),
                new QueryFilter(
                    field: QueryFilter.FieldOptions.IsLocked,
                    op: QueryFilter.OpOptions.EQ,
                    value: "false"
                )
            };

            options.Order = new List<QueryOrder>()
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.AvailableSlots
                )
            };

            QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
            this.lobbies = lobbies.Results;

            if (LobbyListUI.instance) LobbyListUI.instance.DisplayLobbies(this.lobbies);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Player SpawnNewPlayer(ulong clientId)
    {
        if (IsServer)
        {
            GameObject player = Instantiate(companion.playerPrefab);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
            player.GetComponent<Player>().isHost.Value = clientId == ServerClientId;
            return player.GetComponent<Player>();
        }
        return null;
    }

    public void SpawnPlayers(bool spawnPlayers = true)
    {
        if (IsServer)
        {
            if (spawnPlayers)
            {
                foreach (KeyValuePair<ulong, NetworkClient> client in ConnectedClients)
                {
                    try
                    {
                        Player existingPlayer = players.First((a) => a.OwnerClientId == client.Key);
                    }
                    catch
                    {
                        SpawnNewPlayer(client.Key);
                    }
                }
            }
        }
    }

    public async void JoinLobbyWithCode(string lobbyCode)
    {
        Lobby lobby = null;
        try
        {
            lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            MenuUIManager.instance.ToggleLoadingScreen(false);
            return;
        }
        m_Lobby = lobby;

        var callbacks = new LobbyEventCallbacks();
        callbacks.KickedFromLobby += OnKickedFromLobby;

        try
        {
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(m_Lobby.Id, callbacks);
            lobbyEventCallbacks = callbacks;
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason)
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{m_Lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }

        m_Lobby = lobby;
        OnLobbyEntered(lobby);
    }

    public async void JoinLobbyWithID(string lobbyID)
    {
        Lobby lobby = null;
        try
        {
            lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyID);
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            MenuUIManager.instance.ToggleLoadingScreen(false);
            return;
        }

        m_Lobby = lobby;

        var callbacks = new LobbyEventCallbacks();
        callbacks.KickedFromLobby += OnKickedFromLobby;

        try
        {
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(m_Lobby.Id, callbacks);
            lobbyEventCallbacks = callbacks;
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason)
            {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{m_Lobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }

        m_Lobby = lobby;
        OnLobbyEntered(lobby);
    }

    private void OnKickedFromLobby()
    {
        lobbyEventCallbacks = null;
        m_Lobby = null;
    }

    public void LeaveGame()
    {
        if (IsHost)
        {
            List<ulong> tempClientIds = new List<ulong>(ConnectedClientsIds);
            foreach (ulong clientID in tempClientIds)
            {
                if (clientID == LocalClientId) continue;
                DisconnectClient(clientID, "Host has left the game");
            }
            if (m_Lobby != null) LobbyService.Instance.DeleteLobbyAsync(m_Lobby.Id);
            m_Lobby = null;
        }
        if (m_Lobby != null) LobbyService.Instance.RemovePlayerAsync(m_Lobby.Id, AuthenticationService.Instance.PlayerId);
        Shutdown();
    }

    public void KickPlayer(Player player)
    {
        if (IsHost || IsServer)
        {
            DisconnectClient(player.OwnerClientId, "You were kicked");
        }
    }

    public void KickPlayer(ulong clientId)
    {
        if (IsHost || IsServer)
        {
            DisconnectClient(clientId, "You were kicked");
        }
    }

    public void ToggleSingleplayer(bool singleplayer = true)
    {
        companion.singlePlayerTransport.enabled = singleplayer;
        companion.unityTransport.enabled = !singleplayer;
        NetworkConfig.NetworkTransport = singleplayer ? companion.singlePlayerTransport : companion.unityTransport;
        isSingleplayer = singleplayer;
    }

    IEnumerator LobbyTimeoutCheck_Coro()
    {
        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.5f);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                StopCoroutine("LobbyTimeoutCheck_Coro");
        }
        if (!IsConnectedClient || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
            MenuUIManager.instance.ToggleLoadingScreen(false);
            Shutdown();
        }
    }

    IEnumerator HeartbeatLobbyCoroutine(float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (m_Lobby != null)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(m_Lobby.Id);
            yield return delay;
        }
    }

    public async void ToggleLobbyLock(bool val)
    {
        if (m_Lobby == null) return;
        UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions
        {
            IsLocked = val
        };

        try
        {
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(m_Lobby.Id, updateLobbyOptions);
            m_Lobby = updatedLobby;
        } catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Failed to update lobby: {e.Message}");
        }
    }

    // public string GenerateLobbyCode(int length = 6)
    // {
    //     string possibilities = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
    //     string final = "";
    //     for (int i = 0; i < length; i++)
    //     {
    //         final += possibilities[UnityEngine.Random.Range(0, possibilities.Length)];
    //     }
    //     return final;
    // }
}
