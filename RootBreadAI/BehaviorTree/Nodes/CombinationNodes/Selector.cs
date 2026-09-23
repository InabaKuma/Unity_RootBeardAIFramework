using System.Collections.Generic;
using RootBeard.Interface;

namespace RootBeard.Framework
{
    public class Selector : IBehaviorNode
    {
        private List<IBehaviorNode> children = new List<IBehaviorNode>();
        private int runningIndex = -1; // 上次运行中的子节点索引

        public Selector(params IBehaviorNode[] nodes)
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

                if (result == NodeState.Success)
                {
                    runningIndex = -1; // 重置
                    return NodeState.Success;
                }
                // Failure 则继续下一个
            }

            runningIndex = -1;
            return NodeState.Failure;
        }
    }
}