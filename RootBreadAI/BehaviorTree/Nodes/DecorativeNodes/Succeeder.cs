using RootBeard.Interface;

namespace RootBeard.Framework
{
    /// <summary>
    /// 永远成功节点：Running 透传，Success/Failure 都变 Success。
    /// </summary>
    public class Succeeder : IBehaviorNode
    {
        private IBehaviorNode child;

        public Succeeder(IBehaviorNode child) { this.child = child; }

        public NodeState Execute()
        {
            if (child == null) return NodeState.Success;

            NodeState result = child.Execute();
            if (result == NodeState.Running) return NodeState.Running;
            return NodeState.Success;
        }
    }
}