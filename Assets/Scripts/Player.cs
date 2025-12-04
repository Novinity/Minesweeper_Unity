using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    public static Player LocalPlayer { get; private set;}

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>("Unknown", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> isHost = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TilesLeft = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> FlagsLeft = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Start()
    {
        if (!GameNetworkManager.instance.players.Contains(this))
        {
            GameNetworkManager.instance.players.Add(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        PlayerName.Value = PlayerPrefs.GetString("PlayerName");
        LocalPlayer = this;
    }

    public override void OnNetworkDespawn()
    {
        GameNetworkManager.instance.players.Remove(this);
    }
}
