using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StateMachineAsset))]
public class StateMachineAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var asset = (StateMachineAsset)target;

        serializedObject.Update();

        // ============ 初始状态 ============
        var stateNames = new List<string>();
        foreach (var s in asset.States)
            if (!string.IsNullOrEmpty(s.Name)) stateNames.Add(s.Name);

        if (stateNames.Count > 0)
        {
            int idx = stateNames.IndexOf(asset.InitialStateName);
            if (idx < 0) idx = 0;
            EditorGUI.BeginChangeCheck();
            idx = EditorGUILayout.Popup("Initial State", idx, stateNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Change Initial State");
                asset.InitialStateName = stateNames[idx];
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            string newInitial = EditorGUILayout.TextField("Initial State", asset.InitialStateName);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Change Initial State");
                asset.InitialStateName = newInitial;
            }
        }

        // ============ 状态列表 ============
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("States", EditorStyles.boldLabel);

        int removeStateAt = -1;
        for (int i = 0; i < asset.States.Count; i++)
        {
            var entry = asset.States[i];
            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Name", entry.Name);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Edit State Name");
                entry.Name = newName;
            }
            if (GUILayout.Button("×", GUILayout.Width(22))) removeStateAt = i;
            EditorGUILayout.EndHorizontal();

            // 拖 .cs 文件
            EditorGUI.BeginChangeCheck();
            var newScript = EditorGUILayout.ObjectField(
                "Script", entry.Script, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(asset, "Change State Script");
                entry.Script = newScript;

                var ms = newScript as MonoScript;
                var cls = ms != null ? ms.GetClass() : null;
                entry.TypeName = cls != null ? cls.AssemblyQualifiedName : "";
            }
            else if (entry.Script != null && string.IsNullOrEmpty(entry.TypeName))
            {
                // 兜底：TypeName 为空时补一次
                var ms = entry.Script as MonoScript;
                var cls = ms != null ? ms.GetClass() : null;
                entry.TypeName = cls != null ? cls.AssemblyQualifiedName : "";
            }

            if (!string.IsNullOrEmpty(entry.TypeName))
            {
                var style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.6f, 0.85f, 1f) }
                };
                GUILayout.Label($"类型: {entry.TypeName}", style);
            }
            else if (entry.Script != null)
            {
                EditorGUILayout.HelpBox("该脚本不是 IState，或未编译完成。", MessageType.Warning);
            }

            GUILayout.EndVertical();
        }

        if (removeStateAt >= 0)
        {
            Undo.RecordObject(asset, "Remove State");
            asset.States.RemoveAt(removeStateAt);
        }

        if (GUILayout.Button("+ 添加状态"))
        {
            Undo.RecordObject(asset, "Add State");
            asset.States.Add(new StateEntry());
        }

        // ============ 转换列表 ============
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Transitions", EditorStyles.boldLabel);

        // 条件编辑用的黑板定义：优先从选中的 AIController 拿
        var defs = FindBlackBoardDefs();

        int removeTransAt = -1;
        for (int i = 0; i < asset.Transitions.Count; i++)
        {
            var t = asset.Transitions[i];
            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"转换 {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(22))) removeTransAt = i;
            EditorGUILayout.EndHorizontal();

            // From
            if (stateNames.Count == 0)
            {
                EditorGUI.BeginChangeCheck();
                string newFrom = EditorGUILayout.TextField("From", t.FromStateName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(asset, "Edit Transition");
                    t.FromStateName = newFrom;
                }
            }
            else
            {
                int fromIdx = stateNames.IndexOf(t.FromStateName);
                if (fromIdx < 0) fromIdx = 0;
                EditorGUI.BeginChangeCheck();
                fromIdx = EditorGUILayout.Popup("From", fromIdx, stateNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(asset, "Edit Transition");
                    t.FromStateName = stateNames[fromIdx];
                }
            }

            // To
            if (stateNames.Count == 0)
            {
                EditorGUI.BeginChangeCheck();
                string newTo = EditorGUILayout.TextField("To", t.ToStateName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(asset, "Edit Transition");
                    t.ToStateName = newTo;
                }
            }
            else
            {
                int toIdx = stateNames.IndexOf(t.ToStateName);
                if (toIdx < 0) toIdx = 0;
                EditorGUI.BeginChangeCheck();
                toIdx = EditorGUILayout.Popup("To", toIdx, stateNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(asset, "Edit Transition");
                    t.ToStateName = stateNames[toIdx];
                }
            }

            GUILayout.Space(4);
            ConditionGroupDrawer.Draw(t.Conditions, defs, asset, "Edit Transition Conditions");

            GUILayout.EndVertical();
        }

        if (removeTransAt >= 0)
        {
            Undo.RecordObject(asset, "Remove Transition");
            asset.Transitions.RemoveAt(removeTransAt);
        }

        if (GUILayout.Button("+ 添加转换"))
        {
            Undo.RecordObject(asset, "Add Transition");
            asset.Transitions.Add(new StateTransition());
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(asset);
    }

    private List<BlackBoardEntry> FindBlackBoardDefs()
    {
        if (Selection.activeGameObject != null)
        {
            var ctrl = Selection.activeGameObject.GetComponent<AIController>();
            if (ctrl != null) return ctrl.BlackBoardDefs;
        }
        return null;
    }
}