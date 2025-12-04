using System.IO;
using TMPro;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour {
    public static MenuUIManager instance;

    public GameObject mainMenu;
    public GameObject lobbyListScreen, loadingScreen;

    public Button multiplayerButton;

    private void Awake() {
        instance = this;
    }

    void Start()
    {
        if (!GameNetworkManager.instance.initializedUS) ToggleLoadingScreen(true);
    }

    public void PlaySingleplayer() {
        GameOptionPersistence.gridX = 10;
        GameOptionPersistence.gridY = 10;
        GameOptionPersistence.bombPercentage = 12.5f;
        GameNetworkManager.instance.Host(true);
    }

    public void QuitGame() {
        Application.Quit();
        Debug.Log("Quit!");
    }

    public void ToggleLoadingScreen(bool val)
    {
        lobbyListScreen.SetActive(false);
        mainMenu.SetActive(!val);
        loadingScreen.SetActive(val);
    }
}
