using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI instance { get; private set; }

    public GameObject lobbyDataItemPrefab;
    public GameObject lobbyListContent;

    public List<GameObject> listOfLobbies = new List<GameObject>();

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
        GameNetworkManager.instance.CreateLobby();
    }
}
