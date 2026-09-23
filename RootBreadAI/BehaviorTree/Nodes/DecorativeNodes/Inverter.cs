using RootBeard.Interface;

namespace RootBeard.Framework
{
    /// <summary>
    /// 取反节点：Success ↔ Failure，Running 保持不变。
    /// </summary>
    public class Inverter : IBehaviorNode
    {
        private IBehaviorNode child;

        public Inverter(IBehaviorNode child) { this.child = child; }

        public NodeState Execute()
        {
            if (child == null) return NodeState.Failure;

            NodeState result = child.Execute();
            if (result == NodeState.Success) return NodeState.Failure;
            if (result == NodeState.Failure) return NodeState.Success;
            return NodeState.Running;
        }
    }

}