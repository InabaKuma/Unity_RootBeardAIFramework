using System;
using System.Collections.Generic;
using UnityEngine;

public enum BTNodeType
{
    Selector, Sequence, Condition, Action, Inverter, Succeeder, Repeater
}

public enum CompareOp { Eq, NotEq, Greater, Less, GreaterEq, LessEq }
public enum LogicOp { And, Or }
public enum BlackBoardValueType { Int, Float, Bool, String }

[Serializable]
public class BlackBoardEntry
{
    public string KeyName;
    public BlackBoardValueType ValueType = BlackBoardValueType.Int;
    public string DefaultValue = "0";
}

[Serializable]
public class ConditionItem
{
    public string KeyName;
    public CompareOp Op = CompareOp.Eq;
    public string Value;
}

[Serializable]
public class ConditionGroup
{
    public LogicOp Logic = LogicOp.And;
    public List<ConditionItem> Items = new List<ConditionItem>();
}

[Serializable]
public class WeightTerm
{
    public string KeyName;
    public float Coefficient;
}

[Serializable]
public class WeightedBehavior
{
    public string Name;
    public string TargetStateName;
    public List<WeightTerm> Terms = new List<WeightTerm>();
}

[Serializable]
public class StateEntry
{
    public string Name;
    public UnityEngine.Object Script;
    [HideInInspector] public string TypeName;

    public List<WeightedBehavior> Behaviors = new List<WeightedBehavior>();
}

[Serializable]
public class BTNodeData
{
    public string Guid;
    public string Name;
    public BTNodeType NodeType;
    public int Priority;
    public Vector2 Position;
    public List<string> ChildrenGuids = new List<string>();

    public ConditionGroup Conditions = new ConditionGroup();

    public string ActionStateName;

    public int RepeatCount = -1;
}

[Serializable]
public class BTComment
{
    public string Guid;
    public string Text = "新注释";
    public Vector2 Position;
    public Vector2 Size = new Vector2(220f, 100f);
    public float FontSize = 12f;
    public bool Bold = false;
    public Color BgColor = new Color(0.95f, 0.82f, 0.30f, 0.15f);
    public Color TextColor = new Color(1f, 0.95f, 0.72f);

    public string AttachedNodeGuid;
    public Color LinkColor = new Color(1f, 0.25f, 0.25f, 1f);

    public bool Collapsed = false;
}