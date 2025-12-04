using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListEntry : MonoBehaviour
{
    public string playerName;
    public ulong playerId;
    public Player player;
    public bool isHost;

    public TMP_Text playerNameText;
    public GameObject hostIcon;
    public GameObject kickButton;

    public Color[] possibleColors;

    [SerializeField] private Image bgImg, outlineImg;

    public void Setup()
    {
        player.PlayerName.OnValueChanged += NameChangeCallback;
        player.isHost.OnValueChanged += HostChangeCallback;

        playerNameText.text = playerName;
        hostIcon.SetActive(isHost);
        kickButton.SetActive(GameNetworkManager.instance.IsHost && !isHost);

        Color randColor = possibleColors[Random.Range(0, possibleColors.Length)];
        bgImg.color = randColor;
        outlineImg.color = new Color(randColor.r * 0.75f, randColor.g * 0.75f, randColor.b * 0.75f, 1.0f);
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
