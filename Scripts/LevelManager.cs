using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Activates target groups one at a time (fixes the "all groups live at once" bug). When the
// active group is solved it activates the next; when the last solves, the level is complete.
public class LevelManager : MonoBehaviour
{
    public List<TargetGroup> groups = new List<TargetGroup>();
    public string nextSceneName = "";   // optional scene to load on completion

    int activeIndex = -1;

    void Start() { ActivateNext(); }

    void Update()
    {
        if (activeIndex < 0 || activeIndex >= groups.Count) return;
        if (groups[activeIndex] != null && groups[activeIndex].IsSolved)
            ActivateNext();
    }

    void ActivateNext()
    {
        activeIndex++;
        if (activeIndex < groups.Count)
        {
            if (groups[activeIndex] != null) groups[activeIndex].Activate();
        }
        else
        {
            Debug.Log("[LevelManager] All groups solved - level complete.");
            if (!string.IsNullOrEmpty(nextSceneName)) SceneManager.LoadScene(nextSceneName);
        }
    }
}
