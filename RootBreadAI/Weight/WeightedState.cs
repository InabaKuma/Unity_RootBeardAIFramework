using System;
using System.Collections.Generic;
using RootBeard.Framework;
using RootBeard.Interface;

public abstract class WeightedState : IState
{
    protected BlackBoard BlackBoard;
    protected List<WeightedBehavior> Behaviors;
    protected Action<string> ChangeState;

    public void Setup(BlackBoard board, List<WeightedBehavior> behaviors, Action<string> changeState)
    {
        BlackBoard = board;
        Behaviors = behaviors;
        ChangeState = changeState;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }

    public virtual void Update()
    {
        if (Behaviors == null || Behaviors.Count == 0) return;

        WeightedBehavior best = Behaviors[0];
        float bestScore = GetWeight(best);

        for (int i = 1; i < Behaviors.Count; i++)
        {
            float s = GetWeight(Behaviors[i]);
            if (s > bestScore) { bestScore = s; best = Behaviors[i]; }
        }

        ExecuteBehavior(best);
    }

    protected float GetWeight(WeightedBehavior b)
    {
        if (b.Terms == null) return 0f;

        float sum = 0f;
        foreach (var t in b.Terms)
        {
            if (string.IsNullOrEmpty(t.KeyName)) continue;
            if (BlackBoard == null || !BlackBoard.ContainsKey(t.KeyName)) continue;

            var v = BlackBoard.Get<object>(t.KeyName);
            if (v == null) continue;

            float f;
            try { f = Convert.ToSingle(v); }
            catch { continue; }

            sum += f * t.Coefficient;
        }
        return sum;
    }

    protected virtual void ExecuteBehavior(WeightedBehavior behavior)
    {
        if (!string.IsNullOrEmpty(behavior.TargetStateName))
            ChangeState?.Invoke(behavior.TargetStateName);
    }
}