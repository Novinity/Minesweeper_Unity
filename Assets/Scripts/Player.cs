using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    public static Player LocalPlayer { get; private set;}

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>("Unknown", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString512Bytes> AuthID = new NetworkVariable<FixedString512Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isHost = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> BombsRemaining = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public NetworkVariable<bool> Loaded = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Start()
    {
        if (!GameNetworkManager.instance.players.Contains(this))
        {
            GameNetworkManager.instance.players.Add(this);
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        Debug.Log(newScene.name);
        if (newScene.name == "s2_Game") Loaded.Value = true;
        else Loaded.Value = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        PlayerName.Value = PlayerPrefs.GetString("PlayerName");
        AuthID.Value = AuthenticationService.Instance.PlayerId;
        LocalPlayer = this;

        SceneManager.activeSceneChanged += OnSceneChanged;
        if (SceneManager.GetActiveScene().name != "s2_Game")
        {
            Loaded.Value = false;
        } else
        {
            StartCoroutine(WaitForLoaded_Coro());
        }
    }

    public override void OnNetworkDespawn()
    {
        GameNetworkManager.instance.players.Remove(this);
    }

    IEnumerator WaitForLoaded_Coro()
    {
        bool cancel = false;
        while (GridGenerator.instance == null || !GridGenerator.instance.generated)
        {
            if (SceneManager.GetActiveScene().name != "s2_Game")
            {
                cancel = true;
                break;
            }
            yield return new WaitForEndOfFrame();
        }
        if (!cancel) Loaded.Value = true;
    }
}
