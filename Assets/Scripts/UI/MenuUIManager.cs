using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour {
    public static MenuUIManager instance;

    public GameObject mainMenu;
    public GameObject lobbyListScreen, loadingScreen, lobbyCreateScreen, kickScreen;
    public Button multiplayerButton;

    public TMP_InputField nameInputField;
    public TMP_Text reasonText, versionText;

    private void Awake() {
        instance = this;
    }

    void Start()
    {
        if (!GameNetworkManager.instance.initializedUS) ToggleLoadingScreen(true);

        GameOptionPersistence.gridX = 10;
        GameOptionPersistence.gridY = 10;
        GameOptionPersistence.bombPercentage = 12.5f;
        GameOptionPersistence.startPos = true;

        versionText.text = $"v{Application.version}";

        UpdatePlayerName();

        if (NetworkManager.Singleton.DisconnectReason != null && NetworkManager.Singleton.DisconnectEvent != NetworkTransport.DisconnectEvents.TransportShutdown)
        {
            reasonText.text = NetworkManager.Singleton.DisconnectReason;
            kickScreen.SetActive(true);
        }
    }

    public void PlaySingleplayer() {
        GameNetworkManager.instance.Host(true);
    }

    public void QuitGame() {
        Application.Quit();
        Debug.Log("Quit!");
    }

    public void ToggleLoadingScreen(bool val)
    {
        lobbyListScreen.SetActive(false);
        lobbyCreateScreen.SetActive(false);
        mainMenu.SetActive(!val);
        loadingScreen.SetActive(val);
    }

    public async Task UpdatePlayerName()
    {
        string name = await AuthenticationService.Instance.GetPlayerNameAsync();
        string[] sp = name.Split('#');
        nameInputField.text = sp[0];
    }

    public async void SetPlayerName()
    {
        nameInputField.interactable = false;
        string targetName = nameInputField.text;
        if (nameInputField.text == "")
        {
            targetName = $"Player-{Random.Range(1000,10000)}";
        }
        await AuthenticationService.Instance.UpdatePlayerNameAsync(targetName);
        await UpdatePlayerName();
        PlayerPrefs.SetString("PlayerName", targetName);
        nameInputField.interactable = true;
    }
}
