using System;
using System.IO;
using UnityEngine;

public class SaveLoad : MonoBehaviour
{
    public static float GetBestTime(int gridX, int gridY, float bombPercentage)
    {
        string compiled = $"{gridX}-{gridY}-{bombPercentage.ToString().Replace(".", "")}";
        string filePath = Path.Combine(Application.persistentDataPath, $"{compiled}.dat");

        try
        {
            string fileContent = File.ReadAllText(filePath);
            float parsed = float.Parse(fileContent.Trim());
            return parsed;
        } catch (Exception e)
        {
            return -1;
        }
    }

    public static void SaveHighScore(int gridX, int gridY, float bombPercentage, float time)
    {
        Debug.Log(time);
        float bestTime = GetBestTime(gridX, gridY, bombPercentage);
        if (bestTime == -1 || bestTime > time)
        {
            string compiled = $"{gridX}-{gridY}-{bombPercentage.ToString().Replace(".", "")}";
            string filePath = Path.Combine(Application.persistentDataPath, $"{compiled}.dat");

            try
            {
                File.WriteAllText(filePath, time.ToString());
            } catch (Exception e)
            {
                Debug.LogError($"Error writing to file: {e}");
            }
        }
    }
}
