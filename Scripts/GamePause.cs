using UnityEngine;

public class GamePause : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public static void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public static void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}