using System;
using System.Collections.Generic;
using UnityEngine;
using RootBeard.Framework;
using RootBeard.Interface;

public static class BehaviorTreeBuilder
{
    public static BehaviorTree Build(AIController controller)
    {
        if (string.IsNullOrEmpty(controller.RootNodeGuid))
        {
            Debug.LogWarning("[BT] 没有设置根节点");
            return null;
        }

        var rootData = controller.AllNodes.Find(n => n.Guid == controller.RootNodeGuid);
        if (rootData == null)
        {
            Debug.LogWarning("[BT] 找不到根节点数据");
            return null;
        }

        var rootNode = BuildNode(controller, rootData);
        return new BehaviorTree(rootNode);
    }

    private static IBehaviorNode BuildNode(AIController controller, BTNodeData data)
    {
        IBehaviorNode inner;

        switch (data.NodeType)
        {
            case BTNodeType.Selector:
                {
                    var children = BuildSortedChildren(controller, data);
                    inner = new Selector(children.ToArray());
                    break;
                }

            case BTNodeType.Sequence:
                {
                    var children = BuildSortedChildren(controller, data);
                    inner = new Sequence(children.ToArray());
                    break;
                }

            case BTNodeType.Condition:
                {
                    var group = data.Conditions;
                    var bb = controller.BlackBoard;
                    inner = new ConditionNode(bb, (board) => ConditionEvaluator.Evaluate(board, group));
                    break;
                }

            case BTNodeType.Action:
                {
                    var targetState = data.ActionStateName;
                    var ctrl = controller;
                    string guid = data.Guid;
                    inner = new ActionNode(() =>
                    {
                        if (string.IsNullOrEmpty(targetState))
                        {
                            string shortGuid = guid.Length >= 6 ? guid.Substring(0, 6) : guid;
                            Debug.LogWarning($"[BT] Action [{shortGuid}] 没有选择目标状态");
                            return NodeState.Failure;
                        }
                        ctrl.ChangeState(targetState);
                        return NodeState.Success;
                    });
                    break;
                }

            case BTNodeType.Inverter:
                {
                    var child = BuildFirstChild(controller, data);
                    inner = child != null
                        ? new Inverter(child)
                        : (IBehaviorNode)new ActionNode(() => NodeState.Failure);
                    break;
                }

            case BTNodeType.Succeeder:
                {
                    var child = BuildFirstChild(controller, data);
                    inner = child != null
                        ? new Succeeder(child)
                        : (IBehaviorNode)new ActionNode(() => NodeState.Failure);
                    break;
                }

            case BTNodeType.Repeater:
                {
                    var child = BuildFirstChild(controller, data);
                    inner = child != null
                        ? new Repeater(child, data.RepeatCount)
                        : (IBehaviorNode)new ActionNode(() => NodeState.Failure);
                    break;
                }

            default:
                return new ActionNode(() => NodeState.Failure);
        }

        string displayName = string.IsNullOrEmpty(data.Name) ? data.NodeType.ToString() : data.Name;
        return new TrackedNode(controller, data.Guid, displayName, inner);
    }

    private static IBehaviorNode BuildFirstChild(AIController controller, BTNodeData parent)
    {
        if (parent.ChildrenGuids.Count == 0) return null;
        var childGuid = parent.ChildrenGuids[0];
        var childData = controller.AllNodes.Find(n => n.Guid == childGuid);
        return childData != null ? BuildNode(controller, childData) : null;
    }

    private static List<IBehaviorNode> BuildSortedChildren(AIController controller, BTNodeData parent)
    {
        var childDatas = new List<BTNodeData>();
        foreach (var childGuid in parent.ChildrenGuids)
        {
            var childData = controller.AllNodes.Find(n => n.Guid == childGuid);
            if (childData != null) childDatas.Add(childData);
        }

        childDatas.Sort((a, b) =>
        {
            int cmp = GetTypeOrder(a.NodeType).CompareTo(GetTypeOrder(b.NodeType));
            if (cmp != 0) return cmp;

            cmp = a.Priority.CompareTo(b.Priority);
            if (cmp != 0) return cmp;

            return a.Position.x.CompareTo(b.Position.x);
        });

        var result = new List<IBehaviorNode>();
        foreach (var childData in childDatas)
            result.Add(BuildNode(controller, childData));

        return result;
    }

    private static int GetTypeOrder(BTNodeType type)
    {
        switch (type)
        {
            case BTNodeType.Selector: return 0;
            case BTNodeType.Sequence: return 0;
            case BTNodeType.Inverter: return 0;
            case BTNodeType.Succeeder: return 0;
            case BTNodeType.Repeater: return 0;
            case BTNodeType.Condition: return 1;
            case BTNodeType.Action: return 2;
            default: return 3;
        }
    }

    private class TrackedNode : IBehaviorNode
    {
        private readonly AIController controller;
        private readonly string guid;
        private readonly string displayName;
        private readonly IBehaviorNode inner;

        public TrackedNode(AIController controller, string guid, string displayName, IBehaviorNode inner)
        {
            this.controller = controller;
            this.guid = guid;
            this.displayName = displayName;
            this.inner = inner;
        }

        public NodeState Execute()
        {
            var result = inner.Execute();
            controller.NodeStates[guid] = result;
            controller.TickPath.Add(displayName);
            return result;
        }
    }
}