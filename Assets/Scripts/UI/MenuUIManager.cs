using System.IO;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour {
    public static MenuUIManager instance;

    public GameObject mainMenu;
    public GameObject lobbyListScreen, loadingScreen, lobbyCreateScreen;

    public TMP_InputField nameInputField;

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
