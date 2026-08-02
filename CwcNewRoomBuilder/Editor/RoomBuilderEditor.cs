using UnityEditor;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// RoomBuilder 组件的自定义 Inspector 编辑器。
    /// 提供交互式 2D 像素画板，支持通过点击与拖拽绘制异形房间，并提供实时生成预览以及一键清除、反转等快捷工具。
    /// </summary>
    [CustomEditor(typeof(RoomBuilder))]
    public class RoomBuilderEditor : UnityEditor.Editor
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)
        // 当前类无序列化属性与字段
        #endregion

        #region 3. 非序列化私有字段 (以 _ 开头)

        private RoomBuilder _roomBuilder;
        private bool _autoPreview = true;
        private bool _showSettings = true;
        private bool _showPresetSubEditor = true;

        private bool _isDragging = false;
        private Vector2Int _dragStartGridPos;
        private Vector2Int _dragEndGridPos;
        private bool _dragModeDraw = true;

        private UnityEditor.Editor _presetEditor;

        private enum PaintMode
        {
            Room = 1,
            Door = 2
        }
        private PaintMode _currentPaintMode = PaintMode.Room;

        #endregion

        #region 4. 公共属性 (Properties)
        // 当前类无公共属性
        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle & Editor Callbacks)

        /// <summary>
        /// 当编辑器被激活时初始化目标引用。
        /// </summary>
        private void OnEnable()
        {
            _roomBuilder = (RoomBuilder)target;

            // 从本地配置中恢复自动预览的勾选状态
            _autoPreview = EditorPrefs.GetBool("CwcNewRoomBuilder_AutoPreview", true);

            // 确保 gridData 被正确初始化
            if (_roomBuilder.gridData == null || _roomBuilder.gridData.Length != _roomBuilder.CanvasWidth * _roomBuilder.CanvasHeight)
            {
                _roomBuilder.ResizeGrid(_roomBuilder.CanvasWidth, _roomBuilder.CanvasHeight);
            }
        }

        /// <summary>
        /// 当编辑器被销毁或失焦时被调用，安全销毁子编辑器以防内存泄漏。
        /// </summary>
        private void OnDisable()
        {
            if (_presetEditor != null)
            {
                DestroyImmediate(_presetEditor);
                _presetEditor = null;
            }
        }

        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 绘制自定义 Inspector 界面。
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 绘制精美的顶部 Title
            DrawEditorHeader();

            EditorGUI.BeginChangeCheck();

            // 绘制基础属性配置区
            DrawSettingsArea();

            EditorGUILayout.Space(10);

            // 绘制画板网格尺寸控制
            DrawCanvasSizeControls();

            EditorGUILayout.Space(10);

            // 绘制核心像素画板与快捷工具
            DrawPixelCanvas();

            EditorGUILayout.Space(10);

            // 绘制生成与预览操作按钮
            DrawActionButtons();

            // 检查 GUI 是否被修改，以支持自动实时预览
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_roomBuilder);
                if (_autoPreview)
                {
                    _roomBuilder.GenerateWithCurrentSeed();
                }
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

        #region 7. 私有方法 (Private Methods)

        /// <summary>
        /// 绘制精美标题栏与说明信息。
        /// </summary>
        private void DrawEditorHeader()
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.5f, 0.9f, 1f); // 现代科技蓝

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.Space(5);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            EditorGUILayout.LabelField("CwcNewRoomBuilder - 房间像素画板", titleStyle);

            EditorGUILayout.Space(3);

            var helpStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 1f) }
            };
            EditorGUILayout.LabelField("点击下方网格格子绘制房间形状。支持实时渲染与自动墙体包边。", helpStyle);

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalColor;
        }

        /// <summary>
        /// 绘制核心配置折叠区。
        /// </summary>
        private void DrawSettingsArea()
        {
            _showSettings = EditorGUILayout.Foldout(_showSettings, "房间生成器配置参数", true);
            if (!_showSettings) return;

            EditorGUILayout.BeginVertical("Box");

            // 属性字段逐一绘制
            SerializedProperty presetProp = serializedObject.FindProperty("_preset");
            SerializedProperty tileSizeProp = serializedObject.FindProperty("_tileSize");
            SerializedProperty roofHeightProp = serializedObject.FindProperty("_roofHeight");
            SerializedProperty decorSafeAreaProp = serializedObject.FindProperty("_decorSafeArea");
            SerializedProperty maxRecursionDepthProp = serializedObject.FindProperty("_maxRecursionDepth");
            SerializedProperty generatorIdProp = serializedObject.FindProperty("_generatorId");
            SerializedProperty currentSeedProp = serializedObject.FindProperty("_currentSeed");

            if (presetProp != null) EditorGUILayout.PropertyField(presetProp, new GUIContent("房间资源预设 (Preset)"));
            if (tileSizeProp != null) EditorGUILayout.PropertyField(tileSizeProp, new GUIContent("物理瓦片边长 (TileSize)"));
            if (roofHeightProp != null) EditorGUILayout.PropertyField(roofHeightProp, new GUIContent("屋顶物理层高 (RoofHeight)"));
            if (decorSafeAreaProp != null) EditorGUILayout.PropertyField(decorSafeAreaProp, new GUIContent("饰品靠墙安全距 (DecorSafeArea)"));
            if (maxRecursionDepthProp != null) EditorGUILayout.PropertyField(maxRecursionDepthProp, new GUIContent("递归挂载深度 (MaxDepth)"));
            if (generatorIdProp != null) EditorGUILayout.PropertyField(generatorIdProp, new GUIContent("生成器标识 ID (GeneratorId)"));
            if (currentSeedProp != null) EditorGUILayout.PropertyField(currentSeedProp, new GUIContent("当前随机数种子 (Seed)"));

            // 级联内联编辑 Preset 资源中的 Group 子属性
            if (_roomBuilder.Preset != null)
            {
                if (_presetEditor == null || _presetEditor.target != _roomBuilder.Preset)
                {
                    if (_presetEditor != null) DestroyImmediate(_presetEditor);
                    _presetEditor = CreateEditor(_roomBuilder.Preset);
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");
                _showPresetSubEditor = EditorGUILayout.Foldout(_showPresetSubEditor, "快速展开编辑预设内部组 (Preset Group Editor)", true);
                if (_showPresetSubEditor)
                {
                    EditorGUI.indentLevel++;
                    _presetEditor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                if (_presetEditor != null)
                {
                    DestroyImmediate(_presetEditor);
                    _presetEditor = null;
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制画板网格的长宽调整滑块，触发 Resize。
        /// </summary>
        private void DrawCanvasSizeControls()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("画板大小调节", EditorStyles.boldLabel);

            int currentWidth = _roomBuilder.CanvasWidth;
            int currentHeight = _roomBuilder.CanvasHeight;

            int newWidth = EditorGUILayout.IntSlider("网格宽度 (X)", currentWidth, 1, 50);
            int newHeight = EditorGUILayout.IntSlider("网格高度 (Z)", currentHeight, 1, 50);

            if (newWidth != currentWidth || newHeight != currentHeight)
            {
                Undo.RecordObject(_roomBuilder, "Resize Canvas Grid");
                _roomBuilder.ResizeGrid(newWidth, newHeight);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制像素交互画板。
        /// </summary>
        private void DrawPixelCanvas()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("像素绘制面板 (Top:+Z, Right:+X) [按住鼠标拖拽可绘制或清除矩形]", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            int width = _roomBuilder.CanvasWidth;
            int height = _roomBuilder.CanvasHeight;

            // 再次安全防御，防数组越界
            if (_roomBuilder.gridData == null || _roomBuilder.gridData.Length != width * height)
            {
                _roomBuilder.ResizeGrid(width, height);
            }

            // 绘制工具栏选择绘制模式
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            string[] toolbars = new string[] { " 绘制房间区域 (Room)", " 绘制边界通道/门 (Door)" };
            _currentPaintMode = (PaintMode)(GUILayout.Toolbar((int)_currentPaintMode - 1, toolbars) + 1);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            float cellSize = 24f;
            float cellGap = 2f;
            float totalCellSize = cellSize + cellGap;

            float canvasWidthPx = width * totalCellSize - cellGap;
            float canvasHeightPx = height * totalCellSize - cellGap;

            // 居中排版计算，分配 Inspector GUI 里的 Rect 空间
            Rect controlRect = GUILayoutUtility.GetRect(canvasWidthPx, canvasHeightPx);
            float inspectorWidth = EditorGUIUtility.currentViewWidth;
            float xOffset = (inspectorWidth - canvasWidthPx - 40f) * 0.5f;
            if (xOffset < 0f) xOffset = 0f;

            Rect canvasRect = new Rect(controlRect.x + xOffset, controlRect.y, canvasWidthPx, canvasHeightPx);

            // 处理鼠标交互事件
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (canvasRect.Contains(mousePos))
                    {
                        _isDragging = true;
                        _dragStartGridPos = GetGridPosFromMouse(mousePos, canvasRect, width, height, totalCellSize);
                        _dragEndGridPos = _dragStartGridPos;

                        int index = _dragStartGridPos.x + _dragStartGridPos.y * width;
                        int brushValue = (int)_currentPaintMode;
                        // 仅在格子当前值已与将要绘制的笔刷值相同时，才触发擦除（设为0），否则均为覆盖写入
                        _dragModeDraw = (_roomBuilder.gridData[index] != brushValue);

                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDragging)
                    {
                        _dragEndGridPos = GetGridPosFromMouse(mousePos, canvasRect, width, height, totalCellSize);
                        e.Use();
                        Repaint(); // 仅通知重绘 selection 选区，不设置 GUI.changed，防止拖拽过程引发频繁生成卡顿！
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDragging)
                    {
                        _isDragging = false;
                        _dragEndGridPos = GetGridPosFromMouse(mousePos, canvasRect, width, height, totalCellSize);

                        Undo.RecordObject(_roomBuilder, "Draw Rect Grid");

                        int xMin = Mathf.Min(_dragStartGridPos.x, _dragEndGridPos.x);
                        int xMax = Mathf.Max(_dragStartGridPos.x, _dragEndGridPos.x);
                        int zMin = Mathf.Min(_dragStartGridPos.y, _dragEndGridPos.y);
                        int zMax = Mathf.Max(_dragStartGridPos.y, _dragEndGridPos.y);

                        int paintValue = _dragModeDraw ? (int)_currentPaintMode : 0;
                        for (int x = xMin; x <= xMax; x++)
                        {
                            for (int z = zMin; z <= zMax; z++)
                            {
                                int idx = x + z * width;
                                _roomBuilder.gridData[idx] = paintValue;
                            }
                        }

                        EditorUtility.SetDirty(_roomBuilder);
                        GUI.changed = true; // 松开鼠标时触发 GUI.changed，从而只在拖动结束运行一次 Generate()
                        e.Use();
                    }
                    break;
            }

            // 绘制画布背景网格
            EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            // 绘制网格格子
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + z * width;
                    int cellValue = _roomBuilder.gridData[index];

                    // 换算单个格子 Rect
                    float cellRectY = canvasRect.yMin + ((height - 1) - z) * totalCellSize;
                    float cellRectX = canvasRect.xMin + x * totalCellSize;
                    Rect cellRect = new Rect(cellRectX, cellRectY, cellSize, cellSize);

                    // 颜色配置：1为普通房间格子（浅灰色），2为门（亮蓝色），0为暗灰色
                    Color cellColor = cellValue == 1 ? new Color(0.65f, 0.65f, 0.65f, 1f) :
                                      cellValue == 2 ? new Color(0.2f, 0.85f, 0.3f, 1f) :
                                      new Color(0.25f, 0.25f, 0.25f, 1f);

                    // 拖拽过程中的选区格子，绘制预览渲染色
                    if (_isDragging)
                    {
                        int xMin = Mathf.Min(_dragStartGridPos.x, _dragEndGridPos.x);
                        int xMax = Mathf.Max(_dragStartGridPos.x, _dragEndGridPos.x);
                        int zMin = Mathf.Min(_dragStartGridPos.y, _dragEndGridPos.y);
                        int zMax = Mathf.Max(_dragStartGridPos.y, _dragEndGridPos.y);

                        if (x >= xMin && x <= xMax && z >= zMin && z <= zMax)
                        {
                            if (_dragModeDraw)
                            {
                                cellColor = _currentPaintMode == PaintMode.Room ? new Color(0.8f, 0.8f, 0.8f, 0.8f) : new Color(0.35f, 0.95f, 0.45f, 0.8f);
                            }
                            else
                            {
                                cellColor = new Color(0.5f, 0.2f, 0.2f, 0.8f);
                            }
                        }
                    }

                    EditorGUI.DrawRect(cellRect, cellColor);
                }
            }

            // 如果处于拖动状态，额外使用 Handles 绘制半透明拉框与细边框线
            if (_isDragging)
            {
                int xMin = Mathf.Min(_dragStartGridPos.x, _dragEndGridPos.x);
                int xMax = Mathf.Max(_dragStartGridPos.x, _dragEndGridPos.x);
                int zMin = Mathf.Min(_dragStartGridPos.y, _dragEndGridPos.y);
                int zMax = Mathf.Max(_dragStartGridPos.y, _dragEndGridPos.y);

                float selX = canvasRect.xMin + xMin * totalCellSize;
                float selY = canvasRect.yMin + ((height - 1) - zMax) * totalCellSize;
                float selW = (xMax - xMin + 1) * totalCellSize - cellGap;
                float selH = (zMax - zMin + 1) * totalCellSize - cellGap;

                Rect selRect = new Rect(selX, selY, selW, selH);
                Color outlineColor = _dragModeDraw ?
                                     (_currentPaintMode == PaintMode.Room ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color(0.2f, 0.9f, 0.3f, 1f)) :
                                     new Color(0.9f, 0.2f, 0.2f, 1f);
                Handles.DrawSolidRectangleWithOutline(selRect, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.1f), outlineColor);
            }

            EditorGUILayout.Space(5);

            // 绘制辅助快捷按钮栏
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("一键清空", EditorStyles.miniButtonLeft))
            {
                Undo.RecordObject(_roomBuilder, "Clear Grid");
                System.Array.Clear(_roomBuilder.gridData, 0, _roomBuilder.gridData.Length);
                GUI.changed = true;
            }
            if (GUILayout.Button("一键全选", EditorStyles.miniButtonMid))
            {
                Undo.RecordObject(_roomBuilder, "Fill Grid");
                for (int i = 0; i < _roomBuilder.gridData.Length; i++) _roomBuilder.gridData[i] = (int)_currentPaintMode;
                GUI.changed = true;
            }
            if (GUILayout.Button("一键反转", EditorStyles.miniButtonRight))
            {
                Undo.RecordObject(_roomBuilder, "Invert Grid");
                for (int i = 0; i < _roomBuilder.gridData.Length; i++) _roomBuilder.gridData[i] = _roomBuilder.gridData[i] == 0 ? (int)_currentPaintMode : 0;
                GUI.changed = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 将鼠标坐标换算为网格节点坐标，已进行坐标越界保护。
        /// </summary>
        private Vector2Int GetGridPosFromMouse(Vector2 mousePos, Rect canvasRect, int width, int height, float totalCellSize)
        {
            float localX = mousePos.x - canvasRect.xMin;
            float localY = mousePos.y - canvasRect.yMin;

            int gridX = Mathf.FloorToInt(localX / totalCellSize);
            int gridZ = (height - 1) - Mathf.FloorToInt(localY / totalCellSize);

            gridX = Mathf.Clamp(gridX, 0, width - 1);
            gridZ = Mathf.Clamp(gridZ, 0, height - 1);

            return new Vector2Int(gridX, gridZ);
        }

        /// <summary>
        /// 绘制底部的控制与生成动作按钮。
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical("Box");

            // 实时预览 Toggle，并在状态发生改变时将其持久化到本地配置中
            bool newAutoPreview = EditorGUILayout.Toggle("自动实时预览 (Auto Preview)", _autoPreview);
            if (newAutoPreview != _autoPreview)
            {
                _autoPreview = newAutoPreview;
                EditorPrefs.SetBool("CwcNewRoomBuilder_AutoPreview", _autoPreview);
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.25f, 0.82f, 0.4f, 1f); // 绿色生成键
            if (GUILayout.Button("生成房间 (Generate)", GUILayout.Height(30)))
            {
                _roomBuilder.Generate();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.25f, 1f); // 红色清理键
            if (GUILayout.Button("清除内容 (Clear)", GUILayout.Height(30)))
            {
                _roomBuilder.Clear();
            }

            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
