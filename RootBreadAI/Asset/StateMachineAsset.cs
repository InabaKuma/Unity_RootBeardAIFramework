using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StateEntry
{
    public string Name;
    public UnityEngine.Object Script;    // 编辑器里拖 .cs 文件，实际是 MonoScript
    [HideInInspector] public string TypeName;   // 由编辑器刷新，运行时用
}

[Serializable]
public class StateTransition
{
    public string FromStateName;
    public string ToStateName;
    public ConditionGroup Conditions = new ConditionGroup();
}

[CreateAssetMenu(fileName = "NewStateMachine", menuName = "AI/State Machine Asset")]
public class StateMachineAsset : ScriptableObject
{
    public string InitialStateName;
    public List<StateEntry> States = new List<StateEntry>();
    public List<StateTransition> Transitions = new List<StateTransition>();
}