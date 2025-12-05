using TMPro;
using UnityEngine;

public class BestTimeText : MonoBehaviour
{
    public TMP_Text text;
    public string prefix;

    void Start()
    {
        if (!text) text = GetComponent<TMP_Text>();
    }

    void LateUpdate()
    {
        float time = SaveLoad.GetBestTime(GameOptionPersistence.gridX, GameOptionPersistence.gridY, GameOptionPersistence.bombPercentage);
        string display = TimeUtils.formatTime(time);
        text.text = $"{prefix}{display}";
    }
}
