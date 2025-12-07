using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI instance { get; private set; }

    public GameObject lobbyDataItemPrefab;
    public GameObject lobbyListContent;

    public List<GameObject> listOfLobbies = new List<GameObject>();

    public TMP_InputField maxPlayersInput;
    public TMP_Dropdown lobbyTypeInput;

    public TMP_InputField codeInput;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void GetListOfLobbies()
    {
        DestroyLobbies();

        GameNetworkManager.instance.GetLobbiesList();
    }

    public void DisplayLobbies(List<Lobby> lobbies)
    {
        for (int i = 0; i < lobbies.Count; i++)
        {
            GameObject createdItem = Instantiate(lobbyDataItemPrefab);
            LobbyDataEntry dataEntry = createdItem.GetComponent<LobbyDataEntry>();

            dataEntry.lobbyID = lobbies[i].Id;
            dataEntry.lobbyName = lobbies[i].Name;
            dataEntry.minPlayers = lobbies[i].Players.Count;
            dataEntry.maxPlayers = lobbies[i].MaxPlayers;
            dataEntry.SetLobbyData();

            createdItem.transform.SetParent(lobbyListContent.transform);
            createdItem.transform.localScale = Vector3.one;

            listOfLobbies.Add(createdItem);
        }
    }

    public void DestroyLobbies()
    {
        foreach (GameObject lobbyItem in listOfLobbies) Destroy(lobbyItem);
        listOfLobbies.Clear();
    }

    public void CreateLobby()
    {
        GameNetworkManager.LobbyType lobbyType = lobbyTypeInput.value == 0 ? GameNetworkManager.LobbyType.PUBLIC : GameNetworkManager.LobbyType.PRIVATE;
        GameNetworkManager.instance.CreateLobby(lobbyType, int.Parse(maxPlayersInput.text));
    }

    public void JoinViaCode()
    {
        if (codeInput.text.Replace(" ", "") == "" || codeInput.text.Length != 6) return;
        MenuUIManager.instance.ToggleLoadingScreen(true);
        GameNetworkManager.instance.JoinLobbyWithCode(codeInput.text.Replace(" ", ""));
    }

    public void OnEndPlayerCapEdit()
    {
        if (maxPlayersInput.text.Trim() == "") maxPlayersInput.text = "2";
        int maxPlayersSet = int.Parse(maxPlayersInput.text);
        if (maxPlayersSet < 2) maxPlayersInput.text = "2";
        else if (maxPlayersSet > GameNetworkManager.instance.MaxPlayers) maxPlayersInput.text = GameNetworkManager.instance.MaxPlayers.ToString();
    }
}
