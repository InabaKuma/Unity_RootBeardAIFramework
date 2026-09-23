using System;
using System.Collections.Generic;

namespace RootBeard.Framework
{
    /// <summary>
    /// 效用动作：综合多个考虑因素，得出一个 0~1 的效用分数。
    /// </summary>
    public class UtilityAction
    {
        private readonly List<Consideration> considerations = new List<Consideration>();
        private readonly Action executeAction;

        public UtilityAction(Action action)
        {
            executeAction = action;
        }

        public void AddConsideration(Consideration c)
        {
            if (c == null || c.Weight <= 0f) return;
            considerations.Add(c);
        }

        public float GetUtility()
        {
            if (considerations.Count == 0) return 0f;

            float totalWeight = 0f;
            float totalScore = 0f;

            foreach (var c in considerations)
            {
                totalWeight += c.Weight;
                totalScore += c.GetScore();
            }

            return totalWeight > 0 ? totalScore / totalWeight : 0f;
        }

        public void Execute() => executeAction?.Invoke();
    }
}