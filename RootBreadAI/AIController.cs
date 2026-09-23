using System;
using System.Collections.Generic;
using UnityEngine;
using RootBeard.Framework;
using RootBeard.Interface;

public class AIController : MonoBehaviour
{
    public string RootNodeGuid;
    public List<BTNodeData> AllNodes = new List<BTNodeData>();
    public List<BTComment> Comments = new List<BTComment>();

    [Header("黑板定义")]
    public List<BlackBoardEntry> BlackBoardDefs = new List<BlackBoardEntry>();

    [HideInInspector] public string InitialStateName;
    [HideInInspector] public List<StateEntry> States = new List<StateEntry>();

    [Header("运行设置")]
    public bool LogExecution = true;

    [NonSerialized] public BlackBoard BlackBoard;
    [NonSerialized] public StateMachine StateMachine;

    [NonSerialized] public Dictionary<string, NodeState> NodeStates = new Dictionary<string, NodeState>();
    [NonSerialized] public List<string> TickPath = new List<string>();

    private BehaviorTree tree;
    private bool pendingTick;

    private void Awake()
    {
        BlackBoard = new BlackBoard();
        BlackBoard.OnValueChanged += OnBlackBoardChanged;
        SyncBlackBoardFromDefs();
    }

    private void Start()
    {
        BuildStateMachine();

        tree = BehaviorTreeBuilder.Build(this);
        if (tree == null)
            Debug.LogWarning($"[BT] {gameObject.name} 没有可运行的行为树");
    }

    private void Update()
    {
        StateMachine?.Update();

        if (pendingTick)
        {
            pendingTick = false;
            TickTree();
        }
    }

    private void OnDestroy()
    {
        if (BlackBoard != null)
            BlackBoard.OnValueChanged -= OnBlackBoardChanged;

        StateMachine?.ExitCurrentState();
    }

    private void OnBlackBoardChanged(string key) => pendingTick = true;

    private void TickTree()
    {
        if (tree == null) return;

        NodeStates.Clear();
        TickPath.Clear();

        var state = tree.Update();

        if (LogExecution)
        {
            string path = TickPath.Count > 0 ? string.Join(" → ", TickPath) : "(无)";
            Debug.Log($"[BT] {gameObject.name} tick 结果: {state} | 执行路径: {path}");
        }
    }

    public void SyncBlackBoardFromDefs()
    {
        if (BlackBoard == null) BlackBoard = new BlackBoard();
        foreach (var entry in BlackBoardDefs)
        {
            if (string.IsNullOrEmpty(entry.KeyName)) continue;
            BlackBoard.Set(entry.KeyName, ParseDefault(entry));
        }
    }

    private object ParseDefault(BlackBoardEntry entry)
    {
        switch (entry.ValueType)
        {
            case BlackBoardValueType.Int:
                return int.TryParse(entry.DefaultValue, out var i) ? i : 0;
            case BlackBoardValueType.Float:
                return float.TryParse(entry.DefaultValue, out var f) ? f : 0f;
            case BlackBoardValueType.Bool:
                return bool.TryParse(entry.DefaultValue, out var b) ? b : false;
            case BlackBoardValueType.String:
                return entry.DefaultValue ?? "";
            default:
                return null;
        }
    }

    public void BuildStateMachine()
    {
        StateMachine = new StateMachine();
        IState initial = null;

        foreach (var entry in States)
        {
            if (string.IsNullOrEmpty(entry.TypeName)) continue;

            var type = Type.GetType(entry.TypeName);
            if (type == null || !typeof(IState).IsAssignableFrom(type)) continue;

            var state = (IState)Activator.CreateInstance(type);
            StateMachine.Register(entry.Name, state);

            if (state is WeightedState ws)
            {
                var capturedMachine = StateMachine;
                ws.Setup(
                    BlackBoard,
                    entry.Behaviors,
                    target => capturedMachine.ChangeState(target)
                );
            }

            if (entry.Name == InitialStateName)
                initial = state;
        }

        if (initial != null)
            StateMachine.ChangeState(initial);
    }

    public void ChangeState(string name)
    {
        if (StateMachine == null) BuildStateMachine();
        StateMachine?.ChangeState(name);
    }

    public List<string> GetStateNames()
    {
        var result = new List<string>();
        foreach (var s in States)
            if (!string.IsNullOrEmpty(s.Name)) result.Add(s.Name);
        return result;
    }
}