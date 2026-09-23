using System;
using RootBeard.Interface;

namespace RootBeard.Framework
{
    public class ActionNode : IBehaviorNode
    {
        private Func<NodeState> action;

        // 构造函数 1：瞬间完成的动作，自动返回 Success
        public ActionNode(Action action)
        {
            this.action = () => { action?.Invoke(); return NodeState.Success; };
        }

        // 构造函数 2：带返回值的动作，可以返回 Running
        public ActionNode(Func<NodeState> action)
        {
            this.action = action;
        }

        public NodeState Execute()
        {
            return action?.Invoke() ?? NodeState.Failure;
        }
    }
}