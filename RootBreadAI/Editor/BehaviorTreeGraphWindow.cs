using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BehaviorTreeGraphWindow : EditorWindow
{
    private AIController controller;

    private const float NodeWidth = 180f;
    private const float NodeHeight = 70f;
    private const float HeaderHeight = 26f;
    private const float PanelWidth = 280f;
    private const float PortRadius = 5f;
    private const float ConnectionHitThreshold = 16f;

    private const float MinZoom = 0.3f;
    private const float MaxZoom = 2.5f;

    private const float CommentHandleSize = 12f;
    private const float CommentFoldHeaderHeight = 26f;

    private BTNodeData selectedNode;
    private BTNodeData draggingNode;
    private Vector2 dragOffset;
    private Vector2 panelScroll;

    private BTNodeData connectingFrom;

    private BTComment selectedComment;
    private BTComment draggingComment;
    private BTComment resizingComment;
    private Vector2 commentDragOffset;
    private Vector2 commentResizeStart;
    private Vector2 commentResizeStartSize;
    private bool commentUndoRecorded;

    private float zoom = 1f;
    private Vector2 panOffset = Vector2.zero;
    private bool panning;

    private bool dragUndoRecorded;

    private static BTNodeData clipboardNode;
    private static int pasteCount;

    private string searchQuery = "";
    private readonly List<BTNodeData> searchResults = new List<BTNodeData>();
    private int searchResultIndex = -1;
    private const string SearchFieldName = "BTSearchField";
    private Rect searchFieldRect;
    private Rect searchLabelRect;

    private static readonly Dictionary<BTNodeType, Color> TypeColors = new Dictionary<BTNodeType, Color>
    {
        { BTNodeType.Selector,  new Color(0.55f, 0.35f, 0.75f) },
        { BTNodeType.Sequence,  new Color(0.30f, 0.55f, 0.85f) },
        { BTNodeType.Condition, new Color(0.85f, 0.65f, 0.20f) },
        { BTNodeType.Action,    new Color(0.30f, 0.70f, 0.40f) },
        { BTNodeType.Inverter,  new Color(0.85f, 0.40f, 0.55f) },
        { BTNodeType.Succeeder, new Color(0.40f, 0.75f, 0.75f) },
        { BTNodeType.Repeater,  new Color(0.75f, 0.55f, 0.30f) },
    };

    private static Texture2D _nodeBg;
    private static Texture2D _nodeBgSelected;

    private static string SafeShort(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length >= n ? s.Substring(0, n) : s;
    }

    private static void EnsureTextures()
    {
        if (_nodeBg == null) _nodeBg = MakeTex(new Color(0.22f, 0.22f, 0.22f));
        if (_nodeBgSelected == null) _nodeBgSelected = MakeTex(new Color(0.28f, 0.32f, 0.42f));
    }

    private static Texture2D MakeTex(Color c)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        return tex;
    }

    public static void OpenWindow(AIController c)
    {
        var window = GetWindow<BehaviorTreeGraphWindow>("AI 行为树编辑器");
        window.controller = c;
        window.minSize = new Vector2(800, 400);
        window.Show();
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }

    private Vector2 ToScreen(Vector2 world) => new Vector2(
        world.x * zoom + panOffset.x,
        world.y * zoom + panOffset.y);

    private Rect ToScreen(Rect world) => new Rect(
        world.x * zoom + panOffset.x,
        world.y * zoom + panOffset.y,
        world.width * zoom,
        world.height * zoom);

    private Vector2 MouseToCanvas(Vector2 screenPos) => (screenPos - panOffset) / zoom;

    private void RecordUndo(string actionName)
    {
        if (controller != null)
            Undo.RegisterCompleteObjectUndo(controller, actionName);
    }

    private void OnGUI()
    {
        if (controller == null)
        {
            GUILayout.Label("AIController 已被销毁，请关闭此窗口。");
            return;
        }

        EnsureTextures();

        if (controller.Comments == null)
            controller.Comments = new List<BTComment>();

        Rect canvasRect = new Rect(0, 0, position.width - PanelWidth, position.height);
        Rect panelRect = new Rect(position.width - PanelWidth, 0, PanelWidth, position.height);

        const float sbW = 240f;
        const float sbH = 20f;
        searchFieldRect = new Rect(canvasRect.width - sbW - 12, 12, sbW, sbH);
        searchLabelRect = new Rect(searchFieldRect.x - 200, searchFieldRect.y, 190, sbH);

        EditorGUI.DrawRect(canvasRect, new Color(0.15f, 0.15f, 0.15f));

        Event e = Event.current;
        bool mouseOverSearch = searchFieldRect.Contains(e.mousePosition) ||
                               searchLabelRect.Contains(e.mousePosition);

        if (canvasRect.Contains(e.mousePosition) && !mouseOverSearch)
            ProcessEvents(e);

        DrawGrid(canvasRect);
        DrawComments();
        DrawConnections();
        DrawPendingLine();
        DrawNodes();

        DrawSearchBar();

        EditorGUI.DrawRect(panelRect, new Color(0.20f, 0.20f, 0.20f));
        EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 1, panelRect.height), new Color(0, 0, 0, 0.6f));
        DrawPanel(panelRect);

        var hintStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        GUI.Label(new Rect(8, canvasRect.height - 22, 500, 18),
            $"Zoom: {zoom:0.00}  滚轮缩放 · 中键平移 · 0 复位 · Ctrl+F 搜索", hintStyle);
    }

    private void DrawGrid(Rect canvasRect)
    {
        float stepWorld = 25f;
        while (stepWorld * zoom < 12f) stepWorld *= 4f;

        float stepScreen = stepWorld * zoom;
        float canvasW = canvasRect.width;
        float canvasH = canvasRect.height;

        float startX = Mathf.Repeat(panOffset.x, stepScreen);
        float startY = Mathf.Repeat(panOffset.y, stepScreen);

        Handles.BeginGUI();
        Handles.color = new Color(1f, 1f, 1f, 0.05f);

        for (float sx = startX; sx <= canvasW; sx += stepScreen)
            Handles.DrawLine(new Vector3(sx, 0f), new Vector3(sx, canvasH));

        for (float sy = startY; sy <= canvasH; sy += stepScreen)
            Handles.DrawLine(new Vector3(0f, sy), new Vector3(canvasW, sy));

        Handles.EndGUI();
    }

    private void DrawSearchBar()
    {
        EditorGUI.DrawRect(new Rect(searchFieldRect.x - 4, searchFieldRect.y - 4,
            searchFieldRect.width + 8, searchFieldRect.height + 8),
            new Color(0.10f, 0.10f, 0.10f, 0.9f));

        EditorGUI.BeginChangeCheck();
        GUI.SetNextControlName(SearchFieldName);
        string newQuery = EditorGUI.TextField(searchFieldRect, searchQuery);
        if (EditorGUI.EndChangeCheck())
        {
            searchQuery = newQuery;
            RebuildSearchResults();
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            string label;
            if (searchResults.Count == 0)
                label = "无匹配";
            else
                label = $"{searchResultIndex + 1} / {searchResults.Count}  (回车切换)";

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = searchResults.Count == 0
                    ? new Color(1f, 0.5f, 0.5f)
                    : new Color(0.9f, 0.9f, 0.9f) },
                hover = { textColor = new Color(1f, 0.85f, 0.4f) },
                active = { textColor = new Color(1f, 0.7f, 0.2f) }
            };
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.border = new RectOffset(0, 0, 0, 0);

            if (GUI.Button(searchLabelRect, label, style))
            {
                EditorGUI.FocusTextInControl(SearchFieldName);
                Repaint();
            }
        }
    }

    private void RebuildSearchResults()
    {
        searchResults.Clear();
        searchResultIndex = -1;

        if (string.IsNullOrEmpty(searchQuery)) return;

        string q = searchQuery.ToLowerInvariant();
        foreach (var node in controller.AllNodes)
        {
            bool nameMatch = !string.IsNullOrEmpty(node.Name) && node.Name.ToLowerInvariant().Contains(q);
            bool typeMatch = node.NodeType.ToString().ToLowerInvariant().Contains(q);
            bool guidMatch = !string.IsNullOrEmpty(node.Guid) && node.Guid.ToLowerInvariant().Contains(q);

            if (nameMatch || typeMatch || guidMatch)
                searchResults.Add(node);
        }

        if (searchResults.Count > 0)
        {
            searchResultIndex = 0;
            CenterOnNode(searchResults[0]);
        }
    }

    private void JumpToNextResult()
    {
        if (searchResults.Count == 0) return;
        searchResultIndex = (searchResultIndex + 1) % searchResults.Count;
        CenterOnNode(searchResults[searchResultIndex]);
    }

    private void CenterOnNode(BTNodeData node)
    {
        if (node == null) return;

        float canvasWidth = position.width - PanelWidth;
        float canvasHeight = position.height;

        Vector2 nodeCenter = new Vector2(
            node.Position.x + NodeWidth * 0.5f,
            node.Position.y + NodeHeight * 0.5f);

        panOffset = new Vector2(canvasWidth * 0.5f, canvasHeight * 0.5f) - nodeCenter * zoom;
        selectedNode = node;
        selectedComment = null;

        Repaint();
    }

    private Rect GetCommentScreenRect(BTComment c)
    {
        Vector2 tl = ToScreen(new Vector2(c.Position.x, c.Position.y));
        if (c.Collapsed)
            return new Rect(tl.x, tl.y, c.Size.x * zoom, CommentFoldHeaderHeight);
        return new Rect(tl.x, tl.y, c.Size.x * zoom, c.Size.y * zoom);
    }

    private Rect GetCommentFoldButtonRect(Rect commentScreenRect)
    {
        return new Rect(
            commentScreenRect.xMax - CommentFoldHeaderHeight,
            commentScreenRect.y,
            CommentFoldHeaderHeight,
            CommentFoldHeaderHeight);
    }

    private void DrawComments()
    {
        if (controller.Comments == null) return;

        DrawCommentLinks();

        foreach (var c in controller.Comments)
        {
            Rect rect = GetCommentScreenRect(c);

            EditorGUI.DrawRect(rect, c.BgColor);

            Color border = (selectedComment == c)
                ? new Color(1f, 0.85f, 0.3f)
                : new Color(c.TextColor.r, c.TextColor.g, c.TextColor.b, 0.6f);
            DrawBorder(rect, border, selectedComment == c ? 2f : 1f);

            Rect foldBtn = GetCommentFoldButtonRect(rect);
            var arrowStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = c.TextColor },
                fontSize = 11
            };
            GUI.Label(foldBtn, c.Collapsed ? "▶" : "▼", arrowStyle);

            if (c.Collapsed)
            {
                string title = string.IsNullOrEmpty(c.Text) ? "注释" : c.Text;
                int nl = title.IndexOf('\n');
                if (nl >= 0) title = title.Substring(0, nl);

                var titleStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = Mathf.Max(8, Mathf.RoundToInt(c.FontSize)),
                    fontStyle = c.Bold ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = c.TextColor },
                    clipping = TextClipping.Clip,
                    padding = new RectOffset(6, 0, 0, 0)
                };
                GUI.Label(
                    new Rect(rect.x + 4, rect.y, rect.width - CommentFoldHeaderHeight - 4, rect.height),
                    title, titleStyle);
            }
            else
            {
                var style = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.UpperLeft,
                    fontSize = Mathf.Max(8, Mathf.RoundToInt(c.FontSize * zoom)),
                    fontStyle = c.Bold ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = c.TextColor },
                    padding = new RectOffset(8, 8, 6, 6)
                };
                GUI.Label(rect, string.IsNullOrEmpty(c.Text) ? " " : c.Text, style);

                Rect handle = new Rect(
                    rect.xMax - CommentHandleSize,
                    rect.yMax - CommentHandleSize,
                    CommentHandleSize, CommentHandleSize);
                EditorGUI.DrawRect(handle, new Color(1f, 0.85f, 0.3f, 0.85f));
            }
        }
    }

    private void DrawCommentLinks()
    {
        if (controller.Comments == null) return;

        foreach (var c in controller.Comments)
        {
            if (string.IsNullOrEmpty(c.AttachedNodeGuid)) continue;

            var node = controller.AllNodes.Find(n => n.Guid == c.AttachedNodeGuid);
            if (node == null) continue;

            Vector2 commentCenter = new Vector2(c.Position.x + c.Size.x * 0.5f, c.Position.y + c.Size.y * 0.5f);
            Vector2 nodeCenter = new Vector2(node.Position.x + NodeWidth * 0.5f, node.Position.y + NodeHeight * 0.5f);

            Rect commentRect = new Rect(c.Position.x, c.Position.y, c.Size.x, c.Size.y);
            Rect nodeRect = new Rect(node.Position.x, node.Position.y, NodeWidth, NodeHeight);

            Vector2 startW = RayRectEdge(commentCenter, nodeCenter, commentRect);
            Vector2 endW = RayRectEdge(nodeCenter, commentCenter, nodeRect);

            Vector2 start = ToScreen(startW);
            Vector2 end = ToScreen(endW);

            Vector2 d = end - start;
            Vector2 ctrl1, ctrl2;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
            {
                ctrl1 = start + new Vector2(d.x * 0.4f, 0);
                ctrl2 = end - new Vector2(d.x * 0.4f, 0);
            }
            else
            {
                ctrl1 = start + new Vector2(0, d.y * 0.4f);
                ctrl2 = end - new Vector2(0, d.y * 0.4f);
            }

            Color solid = new Color(c.LinkColor.r, c.LinkColor.g, c.LinkColor.b, 1f);
            Color oldColor = Handles.color;

            Handles.BeginGUI();
            try
            {
                Handles.color = solid;

                const int samples = 60;
                Vector2 prev = start;
                for (int i = 1; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    Vector2 pt = CubicBezier(start, ctrl1, ctrl2, end, t);
                    Handles.DrawAAPolyLine(6f * zoom, prev, pt);
                    prev = pt;
                }

                Handles.DrawSolidDisc(start, Vector3.forward, 5f * zoom);
                Handles.DrawSolidDisc(end, Vector3.forward, 5f * zoom);
            }
            finally
            {
                Handles.color = oldColor;
                Handles.EndGUI();
            }
        }
    }

    private static Vector2 RayRectEdge(Vector2 from, Vector2 to, Rect rect)
    {
        Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
        Vector2 dc = to - center;

        if (Mathf.Abs(dc.x) < 0.0001f && Mathf.Abs(dc.y) < 0.0001f)
            return center;

        float halfW = rect.width * 0.5f;
        float halfH = rect.height * 0.5f;

        float tX = Mathf.Abs(dc.x) > 0.0001f ? halfW / Mathf.Abs(dc.x) : float.MaxValue;
        float tY = Mathf.Abs(dc.y) > 0.0001f ? halfH / Mathf.Abs(dc.y) : float.MaxValue;
        float t = Mathf.Min(tX, tY);

        return center + dc * t;
    }

    private BTComment HitTestComment(Vector2 canvasPos, out bool isHandle)
    {
        isHandle = false;
        if (controller.Comments == null) return null;

        for (int i = controller.Comments.Count - 1; i >= 0; i--)
        {
            var c = controller.Comments[i];

            float h = c.Collapsed ? CommentFoldHeaderHeight / zoom : c.Size.y;
            Rect rect = new Rect(c.Position.x, c.Position.y, c.Size.x, h);

            if (!c.Collapsed)
            {
                Rect handle = new Rect(
                    rect.xMax - CommentHandleSize / zoom,
                    rect.yMax - CommentHandleSize / zoom,
                    CommentHandleSize / zoom,
                    CommentHandleSize / zoom);

                if (handle.Contains(canvasPos)) { isHandle = true; return c; }
            }
            if (rect.Contains(canvasPos)) return c;
        }
        return null;
    }

    private void AddComment(Vector2 canvasPos)
    {
        RecordUndo("Add Comment");

        var c = new BTComment
        {
            Guid = System.Guid.NewGuid().ToString(),
            Text = "新注释",
            Position = canvasPos
        };
        controller.Comments.Add(c);
        selectedComment = c;
        selectedNode = null;

        EditorUtility.SetDirty(controller);
        Repaint();
    }

    private void DeleteComment(BTComment c)
    {
        if (c == null) return;

        RecordUndo("Delete Comment");
        controller.Comments.Remove(c);
        if (selectedComment == c) selectedComment = null;

        EditorUtility.SetDirty(controller);
        Repaint();
    }

    private Vector2 GetInputPort(BTNodeData node) =>
        new Vector2(node.Position.x + NodeWidth * 0.5f, node.Position.y);

    private Vector2 GetOutputPort(BTNodeData node) =>
        new Vector2(node.Position.x + NodeWidth * 0.5f, node.Position.y + NodeHeight);

    private void DrawNodes()
    {
        foreach (var node in controller.AllNodes)
        {
            Rect worldRect = new Rect(node.Position.x, node.Position.y, NodeWidth, NodeHeight);
            Rect rect = ToScreen(worldRect);
            Color typeColor = TypeColors.TryGetValue(node.NodeType, out var c) ? c : Color.gray;

            float hh = HeaderHeight * zoom;

            EditorGUI.DrawRect(new Rect(rect.x + 3 * zoom, rect.y + 3 * zoom, rect.width, rect.height),
                new Color(0, 0, 0, 0.35f));

            Texture2D bg = (selectedNode == node) ? _nodeBgSelected : _nodeBg;
            GUI.DrawTexture(rect, bg);

            if (selectedNode == node)
                DrawBorder(rect, new Color(1f, 0.9f, 0.35f), 2f);

            if (!string.IsNullOrEmpty(searchQuery) && searchResults.Contains(node))
            {
                bool isCurrent = searchResultIndex >= 0 &&
                                 searchResultIndex < searchResults.Count &&
                                 searchResults[searchResultIndex] == node;
                Color searchColor = isCurrent
                    ? new Color(1f, 0.55f, 0.05f)
                    : new Color(1f, 0.9f, 0.3f, 0.8f);
                DrawBorder(rect, searchColor, isCurrent ? 3f : 2f);
            }

            if (Application.isPlaying
                && controller.NodeStates != null
                && controller.NodeStates.TryGetValue(node.Guid, out var state))
            {
                Color stateColor;
                string stateLabel;

                switch (state)
                {
                    case RootBeard.Interface.NodeState.Success:
                        stateColor = new Color(0.2f, 1f, 0.4f);
                        stateLabel = "SUCCESS";
                        break;
                    case RootBeard.Interface.NodeState.Failure:
                        stateColor = new Color(1f, 0.35f, 0.35f);
                        stateLabel = "FAIL";
                        break;
                    default:
                        stateColor = new Color(1f, 0.9f, 0.2f);
                        stateLabel = "RUNNING";
                        break;
                }

                DrawBorder(rect, stateColor, 3f);

                GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = Mathf.Max(8, Mathf.RoundToInt(11 * zoom))
                };
                stateStyle.normal.textColor = stateColor;
                stateStyle.hover.textColor = stateColor;
                stateStyle.active.textColor = stateColor;
                stateStyle.focused.textColor = stateColor;
                GUI.Label(new Rect(rect.x, rect.y - 18 * zoom, rect.width - 6, 16 * zoom), stateLabel, stateStyle);
            }

            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, hh), typeColor);

            string title = string.IsNullOrEmpty(node.Name) ? node.NodeType.ToString() : node.Name;

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(8, Mathf.RoundToInt(12 * zoom))
            };
            titleStyle.normal.textColor = Color.white;
            titleStyle.hover.textColor = Color.white;
            titleStyle.active.textColor = Color.white;
            titleStyle.focused.textColor = Color.white;
            GUI.Label(new Rect(rect.x, rect.y, rect.width, hh), title, titleStyle);

            GUIStyle infoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(8, Mathf.RoundToInt(10 * zoom))
            };
            var infoColor = new Color(0.85f, 0.85f, 0.85f);
            infoStyle.normal.textColor = infoColor;
            infoStyle.hover.textColor = infoColor;
            infoStyle.active.textColor = infoColor;
            infoStyle.focused.textColor = infoColor;
            GUI.Label(new Rect(rect.x, rect.y + hh + 8 * zoom, rect.width, 18 * zoom),
                GetNodeSubtitle(node), infoStyle);

            Vector2 inPort = ToScreen(GetInputPort(node));
            Vector2 outPort = ToScreen(GetOutputPort(node));

            Handles.BeginGUI();
            Handles.color = new Color(0.9f, 0.9f, 0.9f);
            Handles.DrawSolidDisc(inPort, Vector3.forward, PortRadius * zoom);
            Handles.color = typeColor;
            Handles.DrawSolidDisc(outPort, Vector3.forward, PortRadius * zoom);
            Handles.EndGUI();
        }
    }

    private string GetNodeSubtitle(BTNodeData node)
    {
        string prefix = $"P{node.Priority}";

        if (node.Guid == controller.RootNodeGuid)
            return $"{prefix} · ★ 根节点";

        switch (node.NodeType)
        {
            case BTNodeType.Condition:
                if (node.Conditions == null || node.Conditions.Items.Count == 0)
                    return $"{prefix} · (未设置条件)";
                return $"{prefix} · {node.Conditions.Logic} · {node.Conditions.Items.Count} 条";

            case BTNodeType.Action:
                if (string.IsNullOrEmpty(node.ActionStateName)) return $"{prefix} · (未选择状态)";
                return $"{prefix} · → {node.ActionStateName}";

            case BTNodeType.Repeater:
                return $"{prefix} · {(node.RepeatCount == -1 ? "∞" : node.RepeatCount.ToString())}";

            default:
                return $"{prefix} · {SafeShort(node.Guid, 6)}";
        }
    }

    private void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawConnections()
    {
        foreach (var node in controller.AllNodes)
        {
            foreach (var childGuid in node.ChildrenGuids)
            {
                var child = controller.AllNodes.Find(n => n.Guid == childGuid);
                if (child == null) continue;

                Vector2 start = ToScreen(GetOutputPort(node));
                Vector2 end = ToScreen(GetInputPort(child));
                Vector2 dir = (end.y - start.y) * 0.5f * Vector2.up;

                Color oldColor = Handles.color;
                Handles.BeginGUI();
                try
                {
                    Handles.color = new Color(1f, 0.85f, 0.1f, 1f);
                    Handles.DrawBezier(start, end, start + dir, end - dir,
                        Handles.color, null, 5f * zoom);
                }
                finally
                {
                    Handles.color = oldColor;
                    Handles.EndGUI();
                }
            }
        }
    }

    private void DrawPendingLine()
    {
        if (connectingFrom == null) return;

        Vector2 start = ToScreen(GetOutputPort(connectingFrom));
        Vector2 end = Event.current.mousePosition;
        Vector2 dir = (end.y - start.y) * 0.5f * Vector2.up;

        Handles.BeginGUI();
        Handles.DrawBezier(start, end, start + dir, end - dir,
            new Color(1f, 1f, 0.6f, 0.9f), null, 5f * zoom);
        Handles.EndGUI();
        Repaint();
    }

    private void DrawPanel(Rect panelRect)
    {
        GUILayout.BeginArea(panelRect);
        panelScroll = EditorGUILayout.BeginScrollView(panelScroll);

        GUILayout.BeginHorizontal();
        GUILayout.Space(12);
        GUILayout.BeginVertical();

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        GUILayout.Label("节点属性", headerStyle);
        GUILayout.Space(8);

        if (selectedComment != null)
        {
            DrawCommentPanel(selectedComment);
        }
        else if (selectedNode == null)
        {
            GUILayout.Label("未选中节点。", EditorStyles.miniLabel);
        }
        else
        {
            string newName = EditorGUILayout.TextField("Name", selectedNode.Name);
            int newPriority = EditorGUILayout.IntField("Priority", selectedNode.Priority);

            if (newName != selectedNode.Name || newPriority != selectedNode.Priority)
            {
                RecordUndo("Edit Node Properties");
                selectedNode.Name = newName;
                selectedNode.Priority = newPriority;
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.LabelField("类型", selectedNode.NodeType.ToString());
            EditorGUILayout.LabelField("GUID", SafeShort(selectedNode.Guid, 8));
            EditorGUILayout.LabelField("子节点数", selectedNode.ChildrenGuids.Count.ToString());

            GUILayout.Space(12);

            if (selectedNode.NodeType == BTNodeType.Condition)
            {
                if (selectedNode.Conditions == null)
                    selectedNode.Conditions = new ConditionGroup();

                ConditionGroupDrawer.Draw(selectedNode.Conditions, controller.BlackBoardDefs, controller, "Edit Conditions");
            }
            else if (selectedNode.NodeType == BTNodeType.Action)
            {
                DrawActionPanel(selectedNode);
            }
            else if (selectedNode.NodeType == BTNodeType.Repeater)
            {
                GUILayout.Label("重复参数", EditorStyles.boldLabel);
                int newCount = EditorGUILayout.IntField("Repeat Count", selectedNode.RepeatCount);
                if (newCount != selectedNode.RepeatCount)
                {
                    RecordUndo("Edit Repeater");
                    selectedNode.RepeatCount = newCount;
                    EditorUtility.SetDirty(controller);
                }
                EditorGUILayout.HelpBox("-1 = 无限循环，其他值 = 有限次数", MessageType.Info);
            }
            else if (selectedNode.NodeType == BTNodeType.Inverter)
            {
                GUILayout.Label("取反节点", EditorStyles.boldLabel);
                GUILayout.Label("Success ↔ Failure，Running 保持", EditorStyles.miniLabel);
            }
            else if (selectedNode.NodeType == BTNodeType.Succeeder)
            {
                GUILayout.Label("永远成功节点", EditorStyles.boldLabel);
                GUILayout.Label("Success / Failure → Success，Running 保持", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("（组合节点暂无参数）", EditorStyles.miniLabel);
            }
        }

        GUILayout.EndVertical();
        GUILayout.Space(12);
        GUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawCommentPanel(BTComment c)
    {
        GUILayout.Label("注释", EditorStyles.boldLabel);
        GUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        bool newCollapsed = EditorGUILayout.Toggle("折叠", c.Collapsed);
        string newText = EditorGUILayout.TextArea(c.Text, GUILayout.Height(100));
        float newFontSize = EditorGUILayout.Slider("字体大小", c.FontSize, 8f, 28f);
        bool newBold = EditorGUILayout.Toggle("加粗", c.Bold);
        Color newBg = EditorGUILayout.ColorField("背景色", c.BgColor);
        Color newText2 = EditorGUILayout.ColorField("文字色", c.TextColor);
        Color newLink = EditorGUILayout.ColorField("关联线色", c.LinkColor);

        if (EditorGUI.EndChangeCheck())
        {
            RecordUndo("Edit Comment");
            c.Collapsed = newCollapsed;
            c.Text = newText;
            c.FontSize = newFontSize;
            c.Bold = newBold;
            c.BgColor = newBg;
            c.TextColor = newText2;
            c.LinkColor = newLink;
            EditorUtility.SetDirty(controller);
        }

        GUILayout.Space(8);
        if (string.IsNullOrEmpty(c.AttachedNodeGuid))
        {
            GUILayout.Label("未关联到节点", EditorStyles.miniLabel);
        }
        else
        {
            var node = controller.AllNodes.Find(n => n.Guid == c.AttachedNodeGuid);
            if (node == null)
            {
                GUILayout.Label("关联节点已不存在", EditorStyles.miniLabel);
            }
            else
            {
                string display = string.IsNullOrEmpty(node.Name) ? node.NodeType.ToString() : node.Name;
                GUILayout.Label($"关联: {display} [{SafeShort(node.Guid, 6)}]", EditorStyles.miniLabel);

                if (GUILayout.Button("断开关联"))
                {
                    RecordUndo("Detach Comment");
                    c.AttachedNodeGuid = null;
                    EditorUtility.SetDirty(controller);
                }
            }
        }

        GUILayout.Space(12);
        if (GUILayout.Button("删除注释"))
        {
            DeleteComment(c);
        }
    }

    private void DrawActionPanel(BTNodeData node)
    {
        GUILayout.Label("动作参数", EditorStyles.boldLabel);

        var stateNames = controller.GetStateNames();

        if (stateNames.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "AIController 上没有配置状态。\n" +
                "请先在 Inspector 的『状态机』区添加状态。",
                MessageType.Warning);
            return;
        }

        string newStateName = node.ActionStateName;

        int idx = stateNames.IndexOf(newStateName);
        if (idx < 0) idx = 0;
        idx = EditorGUILayout.Popup("目标状态", idx, stateNames.ToArray());
        newStateName = stateNames[idx];

        if (newStateName != node.ActionStateName)
        {
            RecordUndo("Edit Action Node");
            node.ActionStateName = newStateName;
            EditorUtility.SetDirty(controller);
        }
    }

    private void ProcessEvents(Event e)
    {
        string focused = GUI.GetNameOfFocusedControl();
        bool searchFocused = focused == SearchFieldName;

        switch (e.type)
        {
            case EventType.ScrollWheel:
                {
                    float oldZoom = zoom;
                    zoom = Mathf.Clamp(zoom - e.delta.y * 0.05f, MinZoom, MaxZoom);

                    Vector2 mouseCanvasBefore = (e.mousePosition - panOffset) / oldZoom;
                    panOffset = e.mousePosition - mouseCanvasBefore * zoom;

                    e.Use();
                    Repaint();
                    break;
                }

            case EventType.MouseDown:
                if (e.button == 0 && controller.Comments != null)
                {
                    for (int i = controller.Comments.Count - 1; i >= 0; i--)
                    {
                        var c = controller.Comments[i];
                        Rect screenRect = GetCommentScreenRect(c);
                        Rect foldBtn = GetCommentFoldButtonRect(screenRect);

                        if (foldBtn.Contains(e.mousePosition))
                        {
                            RecordUndo("Toggle Comment");
                            c.Collapsed = !c.Collapsed;
                            EditorUtility.SetDirty(controller);
                            e.Use();
                            Repaint();
                            return;
                        }
                    }
                }

                if (e.button == 2)
                {
                    panning = true;
                    e.Use();
                }
                else if (e.button == 1)
                {
                    Vector2 cm = MouseToCanvas(e.mousePosition);

                    var (parent, child) = HitTestConnection(cm);
                    if (parent != null)
                    {
                        RecordUndo("Disconnect");
                        parent.ChildrenGuids.Remove(child.Guid);
                        EditorUtility.SetDirty(controller);
                        Repaint();
                        e.Use();
                        return;
                    }

                    var hitNode = HitTest(cm);
                    if (hitNode != null)
                    {
                        ShowNodeContextMenu(hitNode);
                        e.Use();
                        return;
                    }

                    bool isHandle;
                    var hitComment = HitTestComment(cm, out isHandle);
                    if (hitComment != null)
                    {
                        ShowCommentContextMenu(hitComment);
                        e.Use();
                        return;
                    }

                    ShowCanvasContextMenu(cm);
                    e.Use();
                }
                else if (e.button == 0)
                {
                    Vector2 cm = MouseToCanvas(e.mousePosition);

                    BTNodeData portHit = null;
                    foreach (var n in controller.AllNodes)
                    {
                        if ((cm - GetOutputPort(n)).magnitude < PortRadius + 4f)
                        {
                            portHit = n;
                            break;
                        }
                    }

                    if (portHit != null)
                    {
                        connectingFrom = portHit;
                        e.Use();
                        return;
                    }

                    var hitNode = HitTest(cm);
                    if (hitNode != null)
                    {
                        selectedNode = hitNode;
                        selectedComment = null;
                        draggingNode = hitNode;
                        dragOffset = cm - hitNode.Position;
                        dragUndoRecorded = false;
                        e.Use();
                        Repaint();
                        return;
                    }

                    bool isHandle;
                    var hitComment = HitTestComment(cm, out isHandle);
                    if (hitComment != null)
                    {
                        selectedComment = hitComment;
                        selectedNode = null;
                        commentUndoRecorded = false;

                        if (isHandle)
                        {
                            resizingComment = hitComment;
                            commentResizeStart = cm;
                            commentResizeStartSize = hitComment.Size;
                        }
                        else
                        {
                            draggingComment = hitComment;
                            commentDragOffset = cm - hitComment.Position;
                        }

                        e.Use();
                        Repaint();
                        return;
                    }

                    selectedNode = null;
                    selectedComment = null;
                    e.Use();
                    Repaint();
                }
                break;

            case EventType.MouseDrag:
                if (panning)
                {
                    panOffset += e.delta;
                    Repaint();
                    e.Use();
                }
                else if (connectingFrom != null)
                {
                    Repaint();
                    e.Use();
                }
                else if (draggingNode != null)
                {
                    if (!dragUndoRecorded)
                    {
                        RecordUndo("Move Node");
                        dragUndoRecorded = true;
                    }

                    Vector2 cm = MouseToCanvas(e.mousePosition);
                    draggingNode.Position = cm - dragOffset;
                    Repaint();
                    e.Use();
                }
                else if (draggingComment != null)
                {
                    if (!commentUndoRecorded)
                    {
                        RecordUndo("Move Comment");
                        commentUndoRecorded = true;
                    }

                    Vector2 cm = MouseToCanvas(e.mousePosition);
                    draggingComment.Position = cm - commentDragOffset;
                    Repaint();
                    e.Use();
                }
                else if (resizingComment != null)
                {
                    if (!commentUndoRecorded)
                    {
                        RecordUndo("Resize Comment");
                        commentUndoRecorded = true;
                    }

                    Vector2 cm = MouseToCanvas(e.mousePosition);
                    Vector2 delta = cm - commentResizeStart;
                    Vector2 newSize = commentResizeStartSize + delta;
                    newSize.x = Mathf.Max(60f, newSize.x);
                    newSize.y = Mathf.Max(40f, newSize.y);
                    resizingComment.Size = newSize;
                    Repaint();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (panning)
                {
                    panning = false;
                    e.Use();
                }
                else if (connectingFrom != null)
                {
                    Vector2 cm = MouseToCanvas(e.mousePosition);
                    BTNodeData target = HitTest(cm);
                    if (target != null && target != connectingFrom)
                        CreateConnection(connectingFrom, target);

                    connectingFrom = null;
                    Repaint();
                    e.Use();
                }
                else if (draggingNode != null)
                {
                    draggingNode = null;
                    dragUndoRecorded = false;
                    EditorUtility.SetDirty(controller);
                    e.Use();
                }
                else if (draggingComment != null)
                {
                    draggingComment = null;
                    commentUndoRecorded = false;
                    EditorUtility.SetDirty(controller);
                    e.Use();
                }
                else if (resizingComment != null)
                {
                    resizingComment = null;
                    commentUndoRecorded = false;
                    EditorUtility.SetDirty(controller);
                    e.Use();
                }
                break;

            case EventType.KeyDown:
                {
                    bool ctrl = e.control || e.command;

                    if (ctrl && e.keyCode == KeyCode.F)
                    {
                        EditorGUI.FocusTextInControl(SearchFieldName);
                        e.Use();
                        break;
                    }

                    if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        if (!string.IsNullOrEmpty(searchQuery) && searchResults.Count > 0)
                        {
                            JumpToNextResult();
                            e.Use();
                        }
                        break;
                    }

                    if (e.keyCode == KeyCode.Escape && !string.IsNullOrEmpty(searchQuery))
                    {
                        searchQuery = "";
                        searchResults.Clear();
                        searchResultIndex = -1;
                        Repaint();
                        e.Use();
                        break;
                    }

                    if (searchFocused)
                        break;

                    if (e.keyCode == KeyCode.Delete)
                    {
                        if (selectedComment != null)
                        {
                            DeleteComment(selectedComment);
                            e.Use();
                        }
                        else if (selectedNode != null)
                        {
                            DeleteNode(selectedNode);
                            e.Use();
                        }
                    }
                    else if (ctrl && e.keyCode == KeyCode.C)
                    {
                        CopySelectedNode();
                        e.Use();
                    }
                    else if (ctrl && e.keyCode == KeyCode.V)
                    {
                        PasteNode();
                        e.Use();
                    }
                    else if (ctrl && e.keyCode == KeyCode.Z)
                    {
                        Undo.PerformUndo();
                        e.Use();
                    }
                    else if (ctrl && e.keyCode == KeyCode.Y)
                    {
                        Undo.PerformRedo();
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Alpha0)
                    {
                        zoom = 1f;
                        panOffset = Vector2.zero;
                        Repaint();
                        e.Use();
                    }
                }
                break;
        }
    }

    private BTNodeData HitTest(Vector2 canvasPos)
    {
        for (int i = controller.AllNodes.Count - 1; i >= 0; i--)
        {
            var node = controller.AllNodes[i];
            Rect rect = new Rect(node.Position.x, node.Position.y, NodeWidth, NodeHeight);
            if (rect.Contains(canvasPos)) return node;
        }
        return null;
    }

    private (BTNodeData, BTNodeData) HitTestConnection(Vector2 canvasPos)
    {
        foreach (var node in controller.AllNodes)
        {
            foreach (var childGuid in node.ChildrenGuids)
            {
                var child = controller.AllNodes.Find(n => n.Guid == childGuid);
                if (child == null) continue;

                Vector2 start = GetOutputPort(node);
                Vector2 end = GetInputPort(child);
                Vector2 dir = (end.y - start.y) * 0.5f * Vector2.up;

                const int samples = 30;
                Vector2 prev = start;
                for (int i = 1; i <= samples; i++)
                {
                    float t = i / (float)samples;
                    Vector2 pt = CubicBezier(start, start + dir, end - dir, end, t);

                    if (DistancePointToSegment(canvasPos, prev, pt) < ConnectionHitThreshold)
                        return (node, child);

                    prev = pt;
                }
            }
        }

        return (null, null);
    }

    private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0
             + 3f * u * u * t * p1
             + 3f * u * t * t * p2
             + t * t * t * p3;
    }

    private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.0001f) return (p - a).magnitude;

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        Vector2 proj = a + t * ab;
        return (p - proj).magnitude;
    }

    private void ShowCanvasContextMenu(Vector2 canvasPos)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("添加 Selector"), false, () => AddNode(BTNodeType.Selector, canvasPos));
        menu.AddItem(new GUIContent("添加 Sequence"), false, () => AddNode(BTNodeType.Sequence, canvasPos));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("添加 Condition"), false, () => AddNode(BTNodeType.Condition, canvasPos));
        menu.AddItem(new GUIContent("添加 Action"), false, () => AddNode(BTNodeType.Action, canvasPos));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("添加 Inverter"), false, () => AddNode(BTNodeType.Inverter, canvasPos));
        menu.AddItem(new GUIContent("添加 Succeeder"), false, () => AddNode(BTNodeType.Succeeder, canvasPos));
        menu.AddItem(new GUIContent("添加 Repeater"), false, () => AddNode(BTNodeType.Repeater, canvasPos));

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("添加注释"), false, () => AddComment(canvasPos));

        menu.AddSeparator("");

        if (clipboardNode == null)
            menu.AddDisabledItem(new GUIContent("粘贴"));
        else
            menu.AddItem(new GUIContent($"粘贴 ({clipboardNode.NodeType})"), false, PasteNode);

        menu.ShowAsContext();
    }

    private void ShowNodeContextMenu(BTNodeData node)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("复制节点"), false, () =>
        {
            selectedNode = node;
            selectedComment = null;
            CopySelectedNode();
        });
        menu.AddItem(new GUIContent("粘贴节点"), false, PasteNode);
        menu.AddSeparator("");

        if (node.ChildrenGuids.Count > 0)
            menu.AddItem(new GUIContent("断开所有子节点"), false, () =>
            {
                RecordUndo("Disconnect All");
                node.ChildrenGuids.Clear();
                EditorUtility.SetDirty(controller);
                Repaint();
            });
        else
            menu.AddDisabledItem(new GUIContent("断开所有子节点"));

        if (controller.RootNodeGuid != node.Guid)
            menu.AddItem(new GUIContent("设为根节点"), false, () =>
            {
                RecordUndo("Set Root");
                controller.RootNodeGuid = node.Guid;
                EditorUtility.SetDirty(controller);
                Repaint();
            });
        else
            menu.AddDisabledItem(new GUIContent("设为根节点"));

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("删除节点"), false, () => DeleteNode(node));

        menu.ShowAsContext();
    }

    private void ShowCommentContextMenu(BTComment c)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent(c.Collapsed ? "展开" : "折叠"), false, () =>
        {
            RecordUndo("Toggle Comment");
            c.Collapsed = !c.Collapsed;
            EditorUtility.SetDirty(controller);
            Repaint();
        });
        menu.AddSeparator("");

        if (controller.AllNodes.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("关联到节点 / (没有节点)"));
        }
        else
        {
            foreach (var node in controller.AllNodes)
            {
                var target = node;
                string display = string.IsNullOrEmpty(node.Name) ? node.NodeType.ToString() : node.Name;
                string shortId = SafeShort(node.Guid, 6);
                bool isCurrent = c.AttachedNodeGuid == node.Guid;

                menu.AddItem(
                    new GUIContent($"关联到节点 / {display} [{shortId}]"),
                    isCurrent,
                    () =>
                    {
                        RecordUndo("Attach Comment");
                        c.AttachedNodeGuid = target.Guid;
                        EditorUtility.SetDirty(controller);
                        Repaint();
                    });
            }
        }

        if (!string.IsNullOrEmpty(c.AttachedNodeGuid))
        {
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("断开关联"), false, () =>
            {
                RecordUndo("Detach Comment");
                c.AttachedNodeGuid = null;
                EditorUtility.SetDirty(controller);
                Repaint();
            });
        }

        menu.AddSeparator("");
        menu.AddItem(new GUIContent("删除注释"), false, () => DeleteComment(c));

        menu.ShowAsContext();
    }

    private void AddNode(BTNodeType type, Vector2 canvasPos)
    {
        RecordUndo("Add Node");

        var node = new BTNodeData
        {
            Guid = System.Guid.NewGuid().ToString(),
            NodeType = type,
            Position = canvasPos
        };
        controller.AllNodes.Add(node);

        if (string.IsNullOrEmpty(controller.RootNodeGuid))
            controller.RootNodeGuid = node.Guid;

        EditorUtility.SetDirty(controller);
        Repaint();
    }

    private void CreateConnection(BTNodeData parent, BTNodeData child)
    {
        if (parent.NodeType == BTNodeType.Condition || parent.NodeType == BTNodeType.Action)
        {
            Debug.LogWarning($"[BT] {parent.NodeType} 是叶子节点，不能有子节点");
            return;
        }

        if (parent.NodeType == BTNodeType.Inverter ||
            parent.NodeType == BTNodeType.Succeeder ||
            parent.NodeType == BTNodeType.Repeater)
        {
            if (parent.ChildrenGuids.Count >= 1)
            {
                Debug.LogWarning($"[BT] {parent.NodeType} 只能有一个子节点");
                return;
            }
        }

        RecordUndo("Connect");

        foreach (var n in controller.AllNodes)
            n.ChildrenGuids.Remove(child.Guid);

        if (!parent.ChildrenGuids.Contains(child.Guid))
            parent.ChildrenGuids.Add(child.Guid);

        EditorUtility.SetDirty(controller);
        Repaint();
    }

    private void DeleteNode(BTNodeData node)
    {
        RecordUndo("Delete Node");

        controller.AllNodes.Remove(node);
        foreach (var n in controller.AllNodes)
            n.ChildrenGuids.Remove(node.Guid);

        if (controller.RootNodeGuid == node.Guid)
            controller.RootNodeGuid = null;

        if (controller.Comments != null)
        {
            foreach (var c in controller.Comments)
            {
                if (c.AttachedNodeGuid == node.Guid)
                    c.AttachedNodeGuid = null;
            }
        }

        if (selectedNode == node) selectedNode = null;
        EditorUtility.SetDirty(controller);
        Repaint();
    }

    private void CopySelectedNode()
    {
        if (selectedNode == null) return;

        clipboardNode = CloneNodeData(selectedNode);
        clipboardNode.ChildrenGuids.Clear();
        pasteCount = 0;

        Debug.Log($"[BT] 已复制节点 '{selectedNode.Name}'");
    }

    private void PasteNode()
    {
        if (clipboardNode == null)
        {
            Debug.Log("[BT] 剪贴板为空");
            return;
        }

        RecordUndo("Paste Node");

        pasteCount++;
        float offset = 30f * pasteCount;

        var clone = CloneNodeData(clipboardNode);
        clone.Guid = System.Guid.NewGuid().ToString();
        clone.ChildrenGuids.Clear();
        clone.Position = clipboardNode.Position + new Vector2(offset, offset);

        controller.AllNodes.Add(clone);
        selectedNode = clone;
        selectedComment = null;

        EditorUtility.SetDirty(controller);
        Repaint();

        Debug.Log($"[BT] 已粘贴节点 '{clone.Name}'");
    }

    private BTNodeData CloneNodeData(BTNodeData src)
    {
        return new BTNodeData
        {
            Guid = src.Guid,
            Name = src.Name,
            NodeType = src.NodeType,
            Priority = src.Priority,
            Position = src.Position,
            ChildrenGuids = new List<string>(src.ChildrenGuids),
            Conditions = CloneConditionGroup(src.Conditions),
            ActionStateName = src.ActionStateName,
            RepeatCount = src.RepeatCount
        };
    }

    private ConditionGroup CloneConditionGroup(ConditionGroup src)
    {
        if (src == null) return new ConditionGroup();

        var g = new ConditionGroup { Logic = src.Logic };
        foreach (var item in src.Items)
        {
            g.Items.Add(new ConditionItem
            {
                KeyName = item.KeyName,
                Op = item.Op,
                Value = item.Value
            });
        }
        return g;
    }
}