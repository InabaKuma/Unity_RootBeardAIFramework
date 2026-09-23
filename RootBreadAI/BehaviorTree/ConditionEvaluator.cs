using System;
using UnityEngine;
using RootBeard.Framework;

/// <summary>
/// 条件评估器。行为树的 Condition 节点、状态机的转换条件都走这里。
/// </summary>
public static class ConditionEvaluator
{
    public static bool Evaluate(BlackBoard board, ConditionGroup group)
    {
        if (board == null || group == null || group.Items.Count == 0) return false;

        if (group.Logic == LogicOp.And)
        {
            foreach (var item in group.Items)
                if (!EvaluateItem(board, item)) return false;
            return true;
        }
        else
        {
            foreach (var item in group.Items)
                if (EvaluateItem(board, item)) return true;
            return false;
        }
    }

    private static bool EvaluateItem(BlackBoard board, ConditionItem item)
    {
        if (string.IsNullOrEmpty(item.KeyName)) return false;
        if (!board.ContainsKey(item.KeyName)) return false;

        var actual = board.Get<object>(item.KeyName);
        if (actual == null) return false;

        return Compare(actual, item.Op, item.Value);
    }

    private static bool Compare(object actual, CompareOp op, string expectedStr)
    {
        if (actual is int || actual is float || actual is double ||
            actual is long || actual is short || actual is byte)
        {
            if (!double.TryParse(expectedStr, out double b)) return false;
            double a = Convert.ToDouble(actual);

            switch (op)
            {
                case CompareOp.Eq: return Math.Abs(a - b) < 0.0001;
                case CompareOp.NotEq: return Math.Abs(a - b) >= 0.0001;
                case CompareOp.Greater: return a > b;
                case CompareOp.Less: return a < b;
                case CompareOp.GreaterEq: return a >= b;
                case CompareOp.LessEq: return a <= b;
            }
        }

        if (actual is bool)
        {
            if (!bool.TryParse(expectedStr, out bool b)) return false;
            bool a = (bool)actual;

            switch (op)
            {
                case CompareOp.Eq: return a == b;
                case CompareOp.NotEq: return a != b;
                default: return false;
            }
        }

        string sa = actual.ToString();
        switch (op)
        {
            case CompareOp.Eq: return sa == expectedStr;
            case CompareOp.NotEq: return sa != expectedStr;
            default:
                int cmp = string.Compare(sa, expectedStr, StringComparison.Ordinal);
                switch (op)
                {
                    case CompareOp.Greater: return cmp > 0;
                    case CompareOp.Less: return cmp < 0;
                    case CompareOp.GreaterEq: return cmp >= 0;
                    case CompareOp.LessEq: return cmp <= 0;
                }
                return false;
        }
    }
}