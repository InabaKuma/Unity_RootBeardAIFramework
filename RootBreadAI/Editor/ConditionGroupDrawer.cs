using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ConditionGroupDrawer
{
    private static readonly string[] CompareOpLabels = { "==", "!=", ">", "<", ">=", "<=" };

    public static void Draw(ConditionGroup group, List<BlackBoardEntry> defs,
                            UnityEngine.Object undoObject = null, string undoLabel = "Edit Conditions")
    {
        if (group == null) return;

        void Record()
        {
            if (undoObject != null) Undo.RegisterCompleteObjectUndo(undoObject, undoLabel);
        }

        GUILayout.Label("条件组", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        LogicOp newLogic = (LogicOp)EditorGUILayout.EnumPopup("逻辑", group.Logic);
        if (EditorGUI.EndChangeCheck())
        {
            Record();
            group.Logic = newLogic;
        }

        GUILayout.Space(4);

        var keyNames = new List<string>();
        var valueTypes = new List<BlackBoardValueType>();
        if (defs != null)
        {
            foreach (var entry in defs)
            {
                if (string.IsNullOrEmpty(entry.KeyName)) continue;
                keyNames.Add(entry.KeyName);
                valueTypes.Add(entry.ValueType);
            }
        }

        int removeAt = -1;
        for (int i = 0; i < group.Items.Count; i++)
        {
            var item = group.Items[i];
            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"条件 {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                Record();
                removeAt = i;
            }
            EditorGUILayout.EndHorizontal();

            string newKey;
            if (keyNames.Count == 0)
            {
                if (defs == null)
                    EditorGUILayout.HelpBox("未选中 AIController，KeyName 需手动输入。", MessageType.Info);
                else
                    EditorGUILayout.HelpBox("AIController 上没有定义黑板变量。", MessageType.Warning);

                newKey = EditorGUILayout.TextField("KeyName", item.KeyName);
            }
            else
            {
                int keyIdx = keyNames.IndexOf(item.KeyName);
                if (keyIdx < 0) keyIdx = 0;
                keyIdx = EditorGUILayout.Popup("KeyName", keyIdx, keyNames.ToArray());
                newKey = keyNames[keyIdx];
            }

            if (newKey != item.KeyName)
            {
                Record();
                item.KeyName = newKey;
            }

            EditorGUI.BeginChangeCheck();
            CompareOp newOp = (CompareOp)EditorGUILayout.Popup("Operator", (int)item.Op, CompareOpLabels);
            if (EditorGUI.EndChangeCheck())
            {
                Record();
                item.Op = newOp;
            }

            int typeIdx = keyNames.IndexOf(item.KeyName);
            BlackBoardValueType vt = typeIdx >= 0 ? valueTypes[typeIdx] : BlackBoardValueType.String;

            string newValue = item.Value;
            switch (vt)
            {
                case BlackBoardValueType.Int:
                    {
                        int v = int.TryParse(item.Value, out var vi) ? vi : 0;
                        v = EditorGUILayout.IntField("Value", v);
                        newValue = v.ToString();
                        break;
                    }
                case BlackBoardValueType.Float:
                    {
                        float v = float.TryParse(item.Value, out var vf) ? vf : 0f;
                        v = EditorGUILayout.FloatField("Value", v);
                        newValue = v.ToString();
                        break;
                    }
                case BlackBoardValueType.Bool:
                    {
                        bool v = bool.TryParse(item.Value, out var vb) && vb;
                        v = EditorGUILayout.Toggle("Value", v);
                        newValue = v.ToString();
                        break;
                    }
                default:
                    newValue = EditorGUILayout.TextField("Value", item.Value);
                    break;
            }

            if (newValue != item.Value)
            {
                Record();
                item.Value = newValue;
            }

            GUILayout.EndVertical();
        }

        if (removeAt >= 0) group.Items.RemoveAt(removeAt);

        if (GUILayout.Button("+ 添加条件"))
        {
            Record();
            group.Items.Add(new ConditionItem());
        }
    }
}