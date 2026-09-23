using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AIController))]
public class AIControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        AIController controller = (AIController)target;

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        bool defsChanged = EditorGUI.EndChangeCheck();

        if (defsChanged)
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            if (Application.isPlaying)
                controller.SyncBlackBoardFromDefs();
        }

        DrawStateMachineSection(controller);
        DrawLiveBlackBoard(controller);

        GUILayout.Space(14);
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("打开 AI 编辑器", GUILayout.Height(32)))
            BehaviorTreeGraphWindow.OpenWindow(controller);
        GUI.backgroundColor = oldBg;
    }

    private void DrawStateMachineSection(AIController controller)
    {
        GUILayout.Space(12);
        EditorGUILayout.LabelField("状态机", EditorStyles.boldLabel);

        var stateNames = controller.GetStateNames();

        // Initial State
        if (stateNames.Count > 0)
        {
            int idx = stateNames.IndexOf(controller.InitialStateName);
            if (idx < 0) idx = 0;
            EditorGUI.BeginChangeCheck();
            idx = EditorGUILayout.Popup("Initial State", idx, stateNames.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Change Initial State");
                controller.InitialStateName = stateNames[idx];
                EditorUtility.SetDirty(controller);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Initial State", "（先添加状态）");
        }

        // KeyName 候选
        var keyNames = new List<string>();
        foreach (var e in controller.BlackBoardDefs)
            if (!string.IsNullOrEmpty(e.KeyName)) keyNames.Add(e.KeyName);

        int removeStateAt = -1;
        for (int i = 0; i < controller.States.Count; i++)
        {
            var entry = controller.States[i];

            GUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ===== 状态标题 =====
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"状态 {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(22))) removeStateAt = i;
            EditorGUILayout.EndHorizontal();

            // ===== 状态字段 =====
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Name", entry.Name);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Edit State Name");
                entry.Name = newName;
                EditorUtility.SetDirty(controller);
            }

            EditorGUI.BeginChangeCheck();
            var newScript = EditorGUILayout.ObjectField(
                "Script", entry.Script, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Change State Script");
                entry.Script = newScript;
                var ms = newScript as MonoScript;
                var cls = ms != null ? ms.GetClass() : null;
                entry.TypeName = cls != null ? cls.AssemblyQualifiedName : "";
                EditorUtility.SetDirty(controller);
            }
            else if (entry.Script != null && string.IsNullOrEmpty(entry.TypeName))
            {
                var ms = entry.Script as MonoScript;
                var cls = ms != null ? ms.GetClass() : null;
                entry.TypeName = cls != null ? cls.AssemblyQualifiedName : "";
            }

            // ===== 权重行为 =====
            GUILayout.Space(6);
            GUILayout.Label("权重行为", EditorStyles.boldLabel);

            int removeBehaviorAt = -1;
            for (int bi = 0; bi < entry.Behaviors.Count; bi++)
            {
                var b = entry.Behaviors[bi];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"行为 {bi + 1}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("×", GUILayout.Width(22))) removeBehaviorAt = bi;
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                b.Name = EditorGUILayout.TextField("Name", b.Name);

                if (stateNames.Count > 0)
                {
                    var choices = new List<string> { "(不切换)" };
                    choices.AddRange(stateNames);
                    int tIdx = string.IsNullOrEmpty(b.TargetStateName)
                        ? 0 : choices.IndexOf(b.TargetStateName);
                    if (tIdx < 0) tIdx = 0;
                    tIdx = EditorGUILayout.Popup("Target", tIdx, choices.ToArray());
                    b.TargetStateName = tIdx == 0 ? "" : choices[tIdx];
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controller, "Edit Behavior");
                    EditorUtility.SetDirty(controller);
                }

                // ===== 公式 =====
                GUILayout.Space(4);
                GUILayout.Label("公式  =  Σ Key × 系数", EditorStyles.miniLabel);

                float labelW = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 10;

                int removeTermAt = -1;
                for (int ti = 0; ti < b.Terms.Count; ti++)
                {
                    var term = b.Terms[ti];
                    EditorGUILayout.BeginHorizontal();

                    if (keyNames.Count > 0)
                    {
                        int kIdx = keyNames.IndexOf(term.KeyName);
                        if (kIdx < 0) kIdx = 0;
                        kIdx = EditorGUILayout.Popup(kIdx, keyNames.ToArray(), GUILayout.Width(110));
                        term.KeyName = keyNames[kIdx];
                    }
                    else
                    {
                        term.KeyName = EditorGUILayout.TextField(term.KeyName, GUILayout.Width(110));
                    }

                    GUILayout.Label("×", GUILayout.Width(14));
                    term.Coefficient = EditorGUILayout.FloatField(term.Coefficient, GUILayout.Width(60));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("−", GUILayout.Width(22))) removeTermAt = ti;

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUIUtility.labelWidth = labelW;

                if (removeTermAt >= 0)
                {
                    Undo.RecordObject(controller, "Remove Term");
                    b.Terms.RemoveAt(removeTermAt);
                    EditorUtility.SetDirty(controller);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(24);
                if (GUILayout.Button("＋ 添加公式项", GUILayout.Height(20)))
                {
                    Undo.RecordObject(controller, "Add Term");
                    b.Terms.Add(new WeightTerm());
                    EditorUtility.SetDirty(controller);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            if (removeBehaviorAt >= 0)
            {
                Undo.RecordObject(controller, "Remove Behavior");
                entry.Behaviors.RemoveAt(removeBehaviorAt);
                EditorUtility.SetDirty(controller);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            if (GUILayout.Button("＋ 添加行为", GUILayout.Height(22)))
            {
                Undo.RecordObject(controller, "Add Behavior");
                entry.Behaviors.Add(new WeightedBehavior());
                EditorUtility.SetDirty(controller);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        if (removeStateAt >= 0)
        {
            Undo.RecordObject(controller, "Remove State");
            controller.States.RemoveAt(removeStateAt);
            EditorUtility.SetDirty(controller);
        }

        GUILayout.Space(4);
        if (GUILayout.Button("＋ 添加状态", GUILayout.Height(26)))
        {
            Undo.RecordObject(controller, "Add State");
            controller.States.Add(new StateEntry());
            EditorUtility.SetDirty(controller);
        }
    }

    private void DrawLiveBlackBoard(AIController controller)
    {
        if (!Application.isPlaying || controller.BlackBoard == null) return;

        GUILayout.Space(12);
        EditorGUILayout.LabelField("黑板实时值", EditorStyles.boldLabel);

        if (controller.BlackBoardDefs.Count == 0)
        {
            EditorGUILayout.HelpBox("没有定义黑板变量。", MessageType.Info);
            return;
        }

        foreach (var entry in controller.BlackBoardDefs)
        {
            if (string.IsNullOrEmpty(entry.KeyName)) continue;
            DrawBlackBoardField(controller, entry);
        }
    }

    private void DrawBlackBoardField(AIController controller, BlackBoardEntry entry)
    {
        var cur = controller.BlackBoard.Get<object>(entry.KeyName);

        switch (entry.ValueType)
        {
            case BlackBoardValueType.Int:
                {
                    int v = cur is int i ? i : 0;
                    int nv = EditorGUILayout.IntField(entry.KeyName, v);
                    if (nv != v) controller.BlackBoard.Set(entry.KeyName, nv);
                    break;
                }
            case BlackBoardValueType.Float:
                {
                    float v = cur is float f ? f : 0f;
                    float nv = EditorGUILayout.FloatField(entry.KeyName, v);
                    if (!Mathf.Approximately(nv, v)) controller.BlackBoard.Set(entry.KeyName, nv);
                    break;
                }
            case BlackBoardValueType.Bool:
                {
                    bool v = cur is bool b && b;
                    bool nv = EditorGUILayout.Toggle(entry.KeyName, v);
                    if (nv != v) controller.BlackBoard.Set(entry.KeyName, nv);
                    break;
                }
            default:
                {
                    string v = cur?.ToString() ?? "";
                    string nv = EditorGUILayout.TextField(entry.KeyName, v);
                    if (nv != v) controller.BlackBoard.Set(entry.KeyName, nv);
                    break;
                }
        }
    }
}