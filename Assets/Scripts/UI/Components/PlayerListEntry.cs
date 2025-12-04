using System;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class PlayerListEntry : MonoBehaviour
{
    public string playerName;
    public ulong playerId;
    public Player player;
    public bool isHost;

    public TMP_Text playerNameText;
    public GameObject hostIcon;
    public GameObject kickButton;

    public void Setup()
    {
        player.PlayerName.OnValueChanged += NameChangeCallback;
        player.isHost.OnValueChanged += HostChangeCallback;

        playerNameText.text = playerName;
        hostIcon.SetActive(isHost);
        kickButton.SetActive(GameNetworkManager.instance.IsHost && !isHost);
    }

    void OnDestroy()
    {
        player.PlayerName.OnValueChanged -= NameChangeCallback;
        player.isHost.OnValueChanged -= HostChangeCallback;
    }

    public void Kick()
    {
        if (GameNetworkManager.instance.IsHost)
        {
            GameNetworkManager.instance.KickPlayer(playerId);
        }
    }

    private void NameChangeCallback(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        playerName = newValue.ToString();
        Setup();
    }

    private void HostChangeCallback(bool previousValue, bool newValue)
    {
        isHost = newValue;
        Setup();
    }
}
