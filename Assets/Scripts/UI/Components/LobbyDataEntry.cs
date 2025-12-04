using TMPro;
using UnityEngine;

public class LobbyDataEntry : MonoBehaviour
{
    // Data
    public string lobbyID;
    public string lobbyName;
    public int minPlayers, maxPlayers;

    public TMP_Text lobbyNameText;
    public TMP_Text lobbyPlayerCountText;

    public void SetLobbyData()
    {
        if (lobbyName == "") lobbyNameText.text = "Empty";
        else lobbyNameText.text = lobbyName;
        lobbyPlayerCountText.text = minPlayers + "/" + maxPlayers + " Players";
    }

    public void JoinLobby()
    {
        MenuUIManager.instance.ToggleLoadingScreen(true);
        GameNetworkManager.instance.JoinLobbyWithID(lobbyID);
    }
}
