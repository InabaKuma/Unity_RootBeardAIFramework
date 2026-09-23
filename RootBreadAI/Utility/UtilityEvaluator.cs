using System.Collections.Generic;

namespace RootBeard.Framework
{
    /// <summary>
    /// 效用评估器：评估所有动作的效用分数，选择最高的执行。
    /// </summary>
    public class UtilityEvaluator
    {
        private List<UtilityAction> actions = new List<UtilityAction>();

        public void AddAction(UtilityAction action) => actions.Add(action);

        public UtilityAction EvaluateBest()
        {
            if (actions.Count == 0) return null;

            UtilityAction best = null;
            float bestScore = float.MinValue;

            foreach (var action in actions)
            {
                float score = action.GetUtility();
                if (score > bestScore)
                {
                    bestScore = score;
                    best = action;
                }
            }

            return best;
        }

        public void EvaluateAndExecute()
        {
            EvaluateBest()?.Execute();
        }
    }
}