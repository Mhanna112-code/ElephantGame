using System.Collections.Generic;
using UnityEngine;

// Owns one SequenceLock (the tested combination-lock logic) and drives the target visuals
// plus the gate. Bullets call OnTargetHit when they strike one of this group's targets.
// Groups are activated one at a time by the LevelManager.
public class TargetGroup : MonoBehaviour
{
    public bool ordered = true;
    public List<Target> targets = new List<Target>();
    public Gate gate;                  // opened when this group solves
    public bool autoSortByX = true;    // safety: order left->right if the list is scrambled

    SequenceLock lockLogic;

    public bool IsSolved => lockLogic != null && lockLogic.state == SequenceLock.State.Solved;
    public bool IsActive => lockLogic != null &&
        lockLogic.state != SequenceLock.State.Dormant && lockLogic.state != SequenceLock.State.Solved;

    void Awake()
    {
        targets.RemoveAll(t => t == null);
        if (autoSortByX)
            targets.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        int[] orders = new int[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].order = i + 1;
            targets[i].group = this;
            orders[i] = i + 1;
        }
        lockLogic = new SequenceLock(ordered, orders);
        Refresh();
    }

    public void Activate() { if (lockLogic != null) lockLogic.Activate(); }

    void Update()
    {
        if (lockLogic == null) return;
        lockLogic.Tick(Time.deltaTime);
        Refresh();
    }

    // Called by a bullet that hit target t. Registers the lock hit and opens the gate on solve.
    // Returns true (the bullet should ricochet off the target, not be destroyed).
    public bool OnTargetHit(Target t)
    {
        int i = targets.IndexOf(t);
        if (i < 0) return false;
        SequenceLock.Hit r = lockLogic.RegisterHit(i);
        if (r == SequenceLock.Hit.Solved && gate != null) gate.Open();
        return true;
    }

    void Refresh()
    {
        bool dormant = lockLogic.state == SequenceLock.State.Dormant;
        for (int i = 0; i < targets.Count; i++)
        {
            Target t = targets[i];
            if (t == null) continue;
            Color c;
            if (lockLogic.Solved[i]) c = t.solvedColor;
            else if (dormant) c = t.dormantColor;
            else if (lockLogic.Lit[i]) c = t.hitColor;
            else c = t.unhitColor;
            if (lockLogic.wrongFlash > 0f && !lockLogic.Solved[i]) c = t.unhitColor;
            t.SetColor(c);
        }
    }
}
