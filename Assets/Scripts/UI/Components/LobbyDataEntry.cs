using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDataEntry : MonoBehaviour
{
    // Data
    public string lobbyID;
    public string lobbyName;
    public int minPlayers, maxPlayers;

    public TMP_Text lobbyNameText;
    public TMP_Text lobbyPlayerCountText;

    public Color[] possibleColors;
    [SerializeField] private Image bgImg, outlineImg;
    

    public void SetLobbyData()
    {
        if (lobbyName == "") lobbyNameText.text = "Empty";
        else lobbyNameText.text = lobbyName;
        lobbyPlayerCountText.text = minPlayers + "/" + maxPlayers + " Players";

        Color randColor = possibleColors[Random.Range(0, possibleColors.Length)];
        bgImg.color = randColor;
        outlineImg.color = new Color(randColor.r * 0.75f, randColor.g * 0.75f, randColor.b * 0.75f, 1.0f);
    }

    public void JoinLobby()
    {
        MenuUIManager.instance.ToggleLoadingScreen(true);
        GameNetworkManager.instance.JoinLobbyWithID(lobbyID);
    }
}
