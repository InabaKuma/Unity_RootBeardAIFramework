using System;
using System.Collections.Generic;
using UnityEngine;
using RootBeard.Framework;
using RootBeard.Interface;

public static class StateMachineBuilder
{
    public static StateMachine Build(StateMachineAsset asset, BlackBoard blackBoard)
    {
        if (asset == null)
        {
            Debug.LogWarning("[SM] asset 为空");
            return null;
        }

        var machine = new StateMachine();
        IState initial = null;

        foreach (var entry in asset.States)
        {
            if (string.IsNullOrEmpty(entry.TypeName))
            {
                Debug.LogWarning($"[SM] 状态 '{entry.Name}' 没有绑定脚本或 TypeName 未刷新");
                continue;
            }

            var type = Type.GetType(entry.TypeName);
            if (type == null || !typeof(IState).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[SM] '{entry.Name}' 的类型 '{entry.TypeName}' 不是 IState");
                continue;
            }

            var state = (IState)Activator.CreateInstance(type);
            machine.Register(entry.Name, state);

            if (entry.Name == asset.InitialStateName)
                initial = state;
        }

        foreach (var t in asset.Transitions)
        {
            if (string.IsNullOrEmpty(t.FromStateName) || string.IsNullOrEmpty(t.ToStateName)) continue;

            var group = t.Conditions;
            if (group == null || group.Items.Count == 0)
            {
                Debug.LogWarning($"[SM] 转换 {t.FromStateName} → {t.ToStateName} 没有配条件，永远不会触发");
            }

            var bb = blackBoard;
            machine.RegisterTransition(t.FromStateName, t.ToStateName,
                () => ConditionEvaluator.Evaluate(bb, group));
        }

        if (initial != null)
            machine.ChangeState(initial);
        else
            Debug.LogWarning($"[SM] 找不到初始状态 '{asset.InitialStateName}'");

        return machine;
    }
}