using System;
using RootBeard.Interface;

namespace RootBeard.Framework
{
    public class ConditionNode : IBehaviorNode
    {
        private BlackBoard blackboard;
        private Func<BlackBoard, bool> condition;

        public ConditionNode(BlackBoard blackboard, Func<BlackBoard, bool> condition)
        {
            this.blackboard = blackboard;
            this.condition = condition;
        }

        public NodeState Execute()
        {
            if (condition == null) return NodeState.Failure;
            return condition(blackboard) ? NodeState.Success : NodeState.Failure;
        }
    }
}