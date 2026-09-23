using System;

namespace RootBeard.Interface
{
    public interface IBehaviorNode
    {
        NodeState Execute();
    }

    public enum NodeState
    {
        Success,
        Failure,
        Running
    }
}