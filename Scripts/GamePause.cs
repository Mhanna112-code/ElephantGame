using UnityEngine;

public class GamePause : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public static void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("[GamePause] PAUSE -> timeScale=0");
    }

    public static void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Debug.Log("[GamePause] RESUME -> timeScale=1");
    }
}