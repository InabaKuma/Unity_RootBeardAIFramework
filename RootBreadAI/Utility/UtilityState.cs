using RootBeard.Interface;

namespace RootBeard.Framework
{
    /// <summary>
    /// 带效用评估的状态基类。如果 evaluator 为空，则执行默认动作。
    /// </summary>
    public abstract class UtilityState : IState
    {
        protected UtilityEvaluator evaluator;

        public virtual void Enter() { }
        public virtual void Exit() { }

        public virtual void Update()
        {
            if (evaluator != null)
            {
                evaluator.EvaluateAndExecute();
            }
            else
            {
                ExecuteDefaultAction();
            }
        }

        protected abstract void ExecuteDefaultAction();
    }
}