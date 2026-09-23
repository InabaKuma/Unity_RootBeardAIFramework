using RootBeard.Interface;

namespace RootBeard.Framework
{
    /// <summary>
    /// 行为树：持有一个根节点，在 Update 中驱动整棵树。
    /// </summary>
    public class BehaviorTree
    {
        private IBehaviorNode root;

        public BehaviorTree(IBehaviorNode root)
        {
            this.root = root;
        }

        public void SetRoot(IBehaviorNode newRoot)
        {
            root = newRoot;
        }

        public NodeState Update()
        {
            if (root == null)
                return NodeState.Failure;

            return root.Execute();
        }
    }
}