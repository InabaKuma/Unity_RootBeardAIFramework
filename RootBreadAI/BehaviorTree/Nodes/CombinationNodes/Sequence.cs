using System.Collections.Generic;
using RootBeard.Interface;

namespace RootBeard.Framework
{
    public class Sequence : IBehaviorNode
    {
        private List<IBehaviorNode> children = new List<IBehaviorNode>();
        private int runningIndex = -1;

        public Sequence(params IBehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public void Add(IBehaviorNode node) => children.Add(node);

        public NodeState Execute()
        {
            int startIndex = runningIndex >= 0 ? runningIndex : 0;

            for (int i = startIndex; i < children.Count; i++)
            {
                NodeState result = children[i].Execute();

                if (result == NodeState.Running)
                {
                    runningIndex = i;
                    return NodeState.Running;
                }

                if (result == NodeState.Failure)
                {
                    runningIndex = -1;
                    return NodeState.Failure;
                }
                // Success 则继续下一个
            }

            runningIndex = -1;
            return NodeState.Success;
        }
    }
}