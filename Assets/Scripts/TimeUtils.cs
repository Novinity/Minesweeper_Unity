using UnityEngine;

public class TimeUtils
{
    public static string formatTime(float time)
    {
        if (time < 0) return "Unknown";
        float curTime = time;
        int hour = Mathf.FloorToInt(curTime);
        if (hour > 12) hour -= 12;
        int minutes = Mathf.FloorToInt(60 * (curTime - Mathf.FloorToInt(curTime)));
        return $"{hour}:{minutes:00}";
    }
}