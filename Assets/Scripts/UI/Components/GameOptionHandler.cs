using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOptionHandler : MonoBehaviour
{
    public static GameOptionHandler instance {get; private set;}

    public TMP_InputField gridSizeXInput, gridSizeYInput, bombPercentageInput;
    public Toggle startPosToggle;

    string prevBombPercent = "";

    void Awake()
    {
        instance = this;
    }

    public void UpdateGameOptions()
    {
        int gridX = 10;
        int gridY = 10;
        float bombPercentage = 12.5f;
        bool startPos = true;
        try
        {
            startPos = startPosToggle.isOn;
            gridX = int.Parse(gridSizeXInput.text);
            gridY = int.Parse(gridSizeYInput.text);
            bombPercentage = float.Parse(bombPercentageInput.text);
        } catch (Exception e)
        {
            if (prevBombPercent != "")
            {
                bombPercentage = float.Parse(prevBombPercent);
            } else
            {
                bombPercentage = 12.5f;
            }
        }

        if (gridX > 100) gridX = 100;
        if (gridY > 100) gridY = 100;
        if (bombPercentage > 99) bombPercentage = 99;

        if (gridX < 5) gridX = 5;
        if (gridY < 5) gridY = 5;
        if (bombPercentage < 1) bombPercentage = 1;

        if (gridSizeXInput.text != gridX.ToString()) gridSizeXInput.text = gridX.ToString();
        if (gridSizeYInput.text != gridY.ToString()) gridSizeYInput.text = gridY.ToString();
        if (bombPercentageInput.text != bombPercentage.ToString()) bombPercentageInput.text = bombPercentage.ToString();

        prevBombPercent = bombPercentageInput.text;

        if (LobbyManager.instance) LobbyManager.instance.UpdateGameOptions();
        else
        {
            Debug.Log("updating");
            GameOptionPersistence.gridX = gridX;
            GameOptionPersistence.gridY = gridY;
            GameOptionPersistence.bombPercentage = bombPercentage;
            GameOptionPersistence.startPos = startPos;
        }
    }
}
