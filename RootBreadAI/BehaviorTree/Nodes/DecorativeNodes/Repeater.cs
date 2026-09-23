using RootBeard.Interface;

namespace RootBeard.Framework
{
    public class Repeater : IBehaviorNode
    {
        private readonly IBehaviorNode child;
        private readonly int maxRepeat;
        private int currentCount;

        public Repeater(IBehaviorNode child, int maxRepeat = -1)
        {
            this.child = child;
            // 小于 -1 的数值视为 -1（无限模式），等于 0 时直接完成
            this.maxRepeat = maxRepeat < -1 ? -1 : maxRepeat;
        }

        public NodeState Execute()
        {
            if (child == null) return NodeState.Failure;
            if (maxRepeat == 0) return NodeState.Success;

            // 无限模式
            if (maxRepeat == -1)
            {
                NodeState r = child.Execute();
                return r == NodeState.Failure ? NodeState.Failure : NodeState.Running;
            }

            // 有限模式
            NodeState result = child.Execute();

            if (result == NodeState.Running) return NodeState.Running;

            if (result == NodeState.Failure)
            {
                currentCount = 0;
                return NodeState.Failure;
            }

            currentCount++;
            if (currentCount >= maxRepeat)
            {
                currentCount = 0;
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }
}