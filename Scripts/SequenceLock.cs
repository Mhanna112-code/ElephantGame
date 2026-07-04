using System.Collections.Generic;

// SequenceLock - the combination-lock logic, a faithful C# port of the browser prototype's
// tested lock-core.js. NO UnityEngine dependency, so it compiles and unit-tests headless.
// A Unity MonoBehaviour (TargetGroup) owns one of these and drives the visuals from its state.
public class SequenceLock
{
    public const float WINDOW = 2.0f;   // max seconds between consecutive hits
    public const float STEP = 0.4f;     // telegraph: seconds each target shown during the reveal
    public const float ALL_BLINK = 0.5f;
    public const float CLEAR = 0.35f;
    public const float WRONG_FLASH = 0.6f;
    public const float RESET_DELAY = 0.35f;

    public enum State { Dormant, Telegraph, Input, Solved }
    public enum Hit { Ignored, Registered, Solved, Wrong }

    public readonly bool Ordered;
    public readonly int Count;
    readonly int[] order;          // order[i] = the required position (1..N) of target i
    public readonly bool[] Lit;    // per-target lit flag (for rendering)
    public readonly bool[] Solved;
    public readonly List<int> HitOrder = new List<int>();

    public State state = State.Dormant;
    public float window;           // seconds left in the between-hit window
    public float tg;               // telegraph clock
    public float wrongFlash;
    float pendingReset;

    public SequenceLock(bool ordered, int[] targetOrders)
    {
        Ordered = ordered;
        order = targetOrders;
        Count = targetOrders.Length;
        Lit = new bool[Count];
        Solved = new bool[Count];
    }

    public void Activate()
    {
        state = State.Telegraph; tg = 0f; window = 0f; pendingReset = 0f;
        HitOrder.Clear();
        for (int i = 0; i < Count; i++) Lit[i] = false;
    }

    void Reset()
    {
        HitOrder.Clear(); window = 0f;
        for (int i = 0; i < Count; i++) Lit[i] = false;
        state = State.Telegraph; tg = 0f;
    }

    // Advance one group by dt. Returns Hit.Solved the frame it solves, else Hit.Ignored.
    public Hit Tick(float dt)
    {
        if (state == State.Dormant || state == State.Solved) return Hit.Ignored;

        if (pendingReset > 0f)
        {
            pendingReset -= dt;
            if (pendingReset <= 0f) Reset();
            return Hit.Ignored;
        }

        if (state == State.Telegraph)
        {
            tg += dt;
            float revealEnd = Ordered ? Count * STEP : STEP;
            float blinkEnd = revealEnd + ALL_BLINK;
            float clearEnd = blinkEnd + CLEAR;
            for (int i = 0; i < Count; i++) Lit[i] = false;
            if (tg < revealEnd)
            {
                if (Ordered)
                {
                    int idx = (int)(tg / STEP) + 1;
                    for (int i = 0; i < Count; i++) Lit[i] = (order[i] == idx);
                }
                else for (int i = 0; i < Count; i++) Lit[i] = true;
            }
            else if (tg < blinkEnd) { for (int i = 0; i < Count; i++) Lit[i] = true; }
            else if (tg < clearEnd) { for (int i = 0; i < Count; i++) Lit[i] = false; }
            else
            {
                state = State.Input; HitOrder.Clear(); window = 0f;
                for (int i = 0; i < Count; i++) Lit[i] = false;
            }
            return Hit.Ignored;
        }

        // Input
        if (wrongFlash > 0f) wrongFlash -= dt;
        if (HitOrder.Count > 0 && HitOrder.Count < Count)
        {
            window -= dt;
            if (window <= 0f) Reset();
        }
        return Hit.Ignored;
    }

    // Register a hit on target index i.
    public Hit RegisterHit(int i)
    {
        if (state != State.Input) return Hit.Ignored;
        if (i < 0 || i >= Count || Lit[i] || Solved[i]) return Hit.Ignored;

        Lit[i] = true;
        HitOrder.Add(i);
        window = WINDOW;

        if (HitOrder.Count < Count) return Hit.Registered;

        bool correct = true;
        if (Ordered)
            for (int k = 0; k < HitOrder.Count; k++)
                if (order[HitOrder[k]] != k + 1) { correct = false; break; }

        if (correct)
        {
            state = State.Solved;
            for (int j = 0; j < Count; j++) { Solved[j] = true; Lit[j] = true; }
            return Hit.Solved;
        }
        wrongFlash = WRONG_FLASH; pendingReset = RESET_DELAY;
        return Hit.Wrong;
    }
}
