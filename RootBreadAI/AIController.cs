using RootBeard.Framework;
using RootBeard.Interface;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public string RootNodeGuid;
    public List<BTNodeData> AllNodes = new List<BTNodeData>();
    public List<BTComment> Comments = new List<BTComment>();

    [Header("黑板定义")]
    public List<BlackBoardEntry> BlackBoardDefs = new List<BlackBoardEntry>();

    [Header("运行设置")]
    public float TickInterval = 1f;
    public bool LogExecution = true;

    [NonSerialized] public BlackBoard BlackBoard;

    [NonSerialized] public Dictionary<string, NodeState> NodeStates = new Dictionary<string, NodeState>();
    [NonSerialized] public List<string> TickPath = new List<string>();

    [NonSerialized]
    private Dictionary<StateMachineAsset, StateMachine> runtimeMachines
        = new Dictionary<StateMachineAsset, StateMachine>();

    private BehaviorTree tree;
    private float timer;

    private void Awake()
    {
        BlackBoard = new BlackBoard();
        SyncBlackBoardFromDefs();
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

    private void Start()
    {
        tree = BehaviorTreeBuilder.Build(this);
        if (tree == null)
            Debug.LogWarning($"[BT] {gameObject.name} 没有可运行的行为树");
    }

    private void Update()
    {
        foreach (var machine in runtimeMachines.Values)
            machine?.Update();

        if (tree == null) return;

        timer += Time.deltaTime;
        if (timer < TickInterval) return;
        timer = 0f;

        NodeStates.Clear();
        TickPath.Clear();

        var state = tree.Update();

        if (LogExecution)
        {
            string path = TickPath.Count > 0 ? string.Join(" → ", TickPath) : "(无)";
            Debug.Log($"[BT] {gameObject.name} tick 结果: {state} | 执行路径: {path}");
        }
    }

    private void OnDestroy()
    {
        foreach (var machine in runtimeMachines.Values)
            machine?.ExitCurrentState();
    }

    public StateMachine GetRuntimeMachine(StateMachineAsset asset)
    {
        if (asset == null) return null;
        if (runtimeMachines.TryGetValue(asset, out var m)) return m;

        m = StateMachineBuilder.Build(asset, BlackBoard);
        runtimeMachines[asset] = m;
        return m;
    }
}