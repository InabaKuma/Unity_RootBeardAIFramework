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

        // 关键：先把 SerializedObject 的改动写回 target，再同步
        if (defsChanged)
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            if (Application.isPlaying)
                controller.SyncBlackBoardFromDefs();
        }

        // 运行时显示黑板实时值，可编辑
        if (Application.isPlaying && controller.BlackBoard != null)
        {
            GUILayout.Space(12);
            EditorGUILayout.LabelField("黑板实时值", EditorStyles.boldLabel);

            if (controller.BlackBoardDefs.Count == 0)
            {
                EditorGUILayout.HelpBox("没有定义黑板变量。", MessageType.Info);
            }
            else
            {
                foreach (var entry in controller.BlackBoardDefs)
                {
                    if (string.IsNullOrEmpty(entry.KeyName)) continue;
                    DrawBlackBoardField(controller, entry);
                }
            }
        }

        GUILayout.Space(10);
        if (GUILayout.Button("打开 AI 编辑器", GUILayout.Height(30)))
        {
            BehaviorTreeGraphWindow.OpenWindow(controller);
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