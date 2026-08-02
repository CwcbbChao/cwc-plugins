using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.NewRoomBuilder
{
    /// <summary>
    /// 核心房间生成器组件。
    /// 遵循第一性原理，将房间结构抽象为体素暴露面的贴面；
    /// 支持基于插槽方向（SlotDirection）的解耦嵌套生成、模块化 Group 复用，并延续了逻辑父锚点的旋转与缩放计算规范。
    /// </summary>
    [AddComponentMenu("Cwcbb/Room Builder/Room Builder")]
    public class RoomBuilder : MonoBehaviour
    {
        #region 1. 常量与静态字段
        // 当前类无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("数据预设")]
        [Tooltip("房间美术资源配置预设，里面包含了结构件组列表和装饰品组列表")]
        [SerializeField]
        private RoomPreset _preset;

        [Header("网格画板尺寸")]
        [Tooltip("画板的宽度（格子数量）")]
        [SerializeField]
        [Range(1, 100)]
        private int _canvasWidth = 10;

        [Tooltip("画板的高度（格子数量）")]
        [SerializeField]
        [Range(1, 100)]
        private int _canvasHeight = 10;

        [Tooltip("网格中单个格子的物理物理大小（米）")]
        [SerializeField]
        private float _tileSize = 3.0f;

        [Tooltip("房间的层高物理高度")]
        [SerializeField]
        private float _roofHeight = 3.0f;

        [Header("装饰规则配置")]
        [Tooltip("地板装饰品离外墙的最小安全格子距离。安全距离越大，普通地板饰品离墙面越远，避免挡路")]
        [SerializeField]
        [Range(0, 5)]
        private int _decorSafeArea = 1;

        [Tooltip("装饰品插槽递归嵌套生成的最大层数限制，防止子物体嵌套插槽导致死循环")]
        [SerializeField]
        [Range(1, 5)]
        private int _maxRecursionDepth = 2;

        [Tooltip("房间生成器标识 ID，用于与 DoorPin 的 generatorId 进行匹配以开辟门通道")]
        [SerializeField]
        private int _generatorId = 0;

        [Header("虚拟插槽切分设置")]
        [Tooltip("自动生成虚拟插槽的切分网格步长（米）")]
        [SerializeField]
        private float _slotStepSize = 0.5f;

        [Tooltip("虚拟插槽在水平面上的最大随机抖动偏移量（米）")]
        [SerializeField]
        private float _slotJitter = 0.1f;

        [Header("随机化设置")]
        [Tooltip("当前生成的随机数种子。在画板涂抹动态预览时保持不变，仅在手动点击 Generate 时重刷")]
        [SerializeField]
        private int _currentSeed = 123456;

        [HideInInspector]
        [Tooltip("扁平化的一维网格状态数组，供编辑器画板直接读写")]
        public int[] gridData;

        #endregion

        #region 3. 非序列化私有字段 (以 _ 开头)

        /// <summary>
        /// 激活的网格坐标集合。
        /// </summary>
        private readonly HashSet<Vector2Int> _activeGridCoords = new HashSet<Vector2Int>();

        /// <summary>
        /// 识别为门通道（不生成墙壁）的网格坐标集合。
        /// </summary>
        private readonly HashSet<Vector2Int> _doorGridCoords = new HashSet<Vector2Int>();

        /// <summary>
        /// 缓存已生成的所有游戏对象，以便在清理时安全销毁。
        /// </summary>
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        /// <summary>
        /// 缓存已生成的结构瓦片艺术对象及其所属的结构组，供首轮虚拟插槽的自动计算。
        /// </summary>
        private readonly List<(GameObject artObj, StructureGroup group)> _spawnedStructures = new List<(GameObject, StructureGroup)>();

        private struct OccupiedDecorData
        {
            public Vector3 position;
            public float spacing;
            public DecorationElement config;
            public bool isVolumeObject;
        }

        /// <summary>
        /// 缓存已放置装饰物的位置和相关占用数据。
        /// </summary>
        private readonly List<OccupiedDecorData> _occupiedDecorPositions = new List<OccupiedDecorData>();

        /// <summary>
        /// 缓存各结构组对应生成的分类容器 Transform。
        /// </summary>
        private readonly Dictionary<StructureGroup, Transform> _structureGroupContainers = new Dictionary<StructureGroup, Transform>();

        /// <summary>
        /// 缓存各装饰组对应生成的分类容器 Transform。
        /// </summary>
        private readonly Dictionary<DecorationGroup, Transform> _decorationGroupContainers = new Dictionary<DecorationGroup, Transform>();

        /// <summary>
        /// 所有生成内容的根托管 GameObject 容器。
        /// </summary>
        private GameObject _roomContainer;

        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取当前绑定的房间配置预设。
        /// </summary>
        public RoomPreset Preset => _preset;

        /// <summary>
        /// 获取单格物理边长。
        /// </summary>
        public float TileSize => _tileSize;

        /// <summary>
        /// 获取房间高度。
        /// </summary>
        public float RoofHeight => _roofHeight;

        /// <summary>
        /// 获取画板网格宽度。
        /// </summary>
        public int CanvasWidth => _canvasWidth;

        /// <summary>
        /// 获取画板网格高度。
        /// </summary>
        public int CanvasHeight => _canvasHeight;

        /// <summary>
        /// 获取当前的随机种子。
        /// </summary>
        public int CurrentSeed => _currentSeed;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)

        /// <summary>
        /// 在编辑器 Scene 视图中绘制网格辅助调试线。
        /// </summary>
        private void OnDrawGizmos()
        {
            // 1. 绘制网格辅助线
            Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);
            for (int x = 0; x <= _canvasWidth; x++)
            {
                Vector3 start = transform.TransformPoint(new Vector3(x * _tileSize, 0f, 0f));
                Vector3 end = transform.TransformPoint(new Vector3(x * _tileSize, 0f, _canvasHeight * _tileSize));
                Gizmos.DrawLine(start, end);
            }
            for (int z = 0; z <= _canvasHeight; z++)
            {
                Vector3 start = transform.TransformPoint(new Vector3(0f, 0f, z * _tileSize));
                Vector3 end = transform.TransformPoint(new Vector3(_canvasWidth * _tileSize, 0f, z * _tileSize));
                Gizmos.DrawLine(start, end);
            }

            // 2. 绘制画板激活的格子
            if (gridData != null && gridData.Length == _canvasWidth * _canvasHeight)
            {
                for (int x = 0; x < _canvasWidth; x++)
                {
                    for (int z = 0; z < _canvasHeight; z++)
                    {
                        int index = x + z * _canvasWidth;
                        int cellValue = gridData[index];
                        if (cellValue != 0)
                        {
                            Vector3 localCenter = new Vector3((x + 0.5f) * _tileSize, 0f, (z + 0.5f) * _tileSize);
                            Vector3 worldCenter = transform.TransformPoint(localCenter);
                            Vector3 size = new Vector3(_tileSize * 0.95f, 0f, _tileSize * 0.95f);

                            if (cellValue == 2) // 门通道格子
                            {
                                Gizmos.color = new Color(1.0f, 0.5f, 0.0f, 0.8f);
                                Gizmos.DrawWireCube(worldCenter, size);
                            }
                            else // 普通空气格子
                            {
                                Gizmos.color = new Color(0.2f, 0.6f, 1.0f, 0.7f);
                                Gizmos.DrawWireCube(worldCenter, size);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 执行一键房间生成流程。
        /// 手动点击 Generate 时，会重新生成一个新的随机种子并在此状态下完成生成。
        /// </summary>
        public virtual void Generate()
        {
            _currentSeed = Random.Range(1, 999999);
            GenerateWithSeed(_currentSeed);
        }

        /// <summary>
        /// 使用当前的随机数种子执行生成。
        /// 专为画板动态预览设计，只改变结构而不改变已有物件的摆放种子。
        /// </summary>
        public virtual void GenerateWithCurrentSeed()
        {
            GenerateWithSeed(_currentSeed);
        }

        /// <summary>
        /// 在指定的随机种子状态下生成整个房间。
        /// </summary>
        /// <param name="seed">种子数值</param>
        public virtual void GenerateWithSeed(int seed)
        {
            Random.InitState(seed);
            Clear();
            InitContainer();
            ParseGridData();
            InitGroupContainers(); // 组容器自动分类初始化
            SpawnStructures();
            SpawnDecorations();
        }

        /// <summary>
        /// 清理所有已生成的游戏对象，重置内部状态，销毁托管容器。
        /// </summary>
        public virtual void Clear()
        {
            // 清理场景中生成的对象
            if (_roomContainer != null)
            {
                DestroyImmediate(_roomContainer);
                _roomContainer = null;
            }

            // 防御性清理：在编辑器或重新加载预制件后，私有引用 _roomContainer 会变为 null，但子节点仍然存在。
            // 遍历所有直接子物体，将名为 "Room_Content_Container" 的残留容器彻底销毁
            List<GameObject> orphans = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child != null && child.name == "Room_Content_Container")
                {
                    orphans.Add(child.gameObject);
                }
            }
            foreach (GameObject orphan in orphans)
            {
                if (orphan != null)
                {
                    DestroyImmediate(orphan);
                }
            }

            // 清理集合与组容器字典缓存
            _spawnedObjects.Clear();
            _spawnedStructures.Clear();
            _occupiedDecorPositions.Clear();
            _activeGridCoords.Clear();
            _doorGridCoords.Clear();
            _structureGroupContainers.Clear();
            _decorationGroupContainers.Clear();
        }

        /// <summary>
        /// 重设画板网格的长宽，并确保已有的网格绘制数据不会丢失。
        /// </summary>
        /// <param name="newWidth">新网格宽度</param>
        /// <param name="newHeight">新网格高度</param>
        public virtual void ResizeGrid(int newWidth, int newHeight)
        {
            if (newWidth < 1) newWidth = 1;
            if (newHeight < 1) newHeight = 1;

            int[] newGrid = new int[newWidth * newHeight];
            if (gridData != null)
            {
                for (int x = 0; x < Mathf.Min(_canvasWidth, newWidth); x++)
                {
                    for (int z = 0; z < Mathf.Min(_canvasHeight, newHeight); z++)
                    {
                        newGrid[x + z * newWidth] = gridData[x + z * _canvasWidth];
                    }
                }
            }

            gridData = newGrid;
            _canvasWidth = newWidth;
            _canvasHeight = newHeight;
        }

        #endregion

        #region 7. 私有方法 (Private Methods)

        /// <summary>
        /// 初始化托管所有生成内容的根容器 GameObject。
        /// </summary>
        private void InitContainer()
        {
            _roomContainer = new GameObject("Room_Content_Container");
            _roomContainer.transform.parent = this.transform;
            _roomContainer.transform.localPosition = Vector3.zero;
            _roomContainer.transform.localRotation = Quaternion.identity;
            _roomContainer.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 按照预设中引用的结构组与装饰组，自动在 Hierarchy 下创建分类容器对象，用于规整收纳生成的物体。
        /// </summary>
        private void InitGroupContainers()
        {
            _structureGroupContainers.Clear();
            _decorationGroupContainers.Clear();

            if (_preset == null) return;

            // 1. 初始化结构组容器
            if (_preset.StructureGroups != null)
            {
                foreach (var group in _preset.StructureGroups)
                {
                    if (group == null || _structureGroupContainers.ContainsKey(group)) continue;

                    GameObject groupObj = new GameObject($"Group_Structure_{group.name}");
                    groupObj.transform.parent = _roomContainer.transform;
                    groupObj.transform.localPosition = Vector3.zero;
                    groupObj.transform.localRotation = Quaternion.identity;
                    groupObj.transform.localScale = Vector3.one;

                    int layerId = GetLayerFromMask(group.Layer);
                    if (layerId != -1)
                    {
                        groupObj.layer = layerId;
                    }

                    _structureGroupContainers[group] = groupObj.transform;
                    _spawnedObjects.Add(groupObj);
                }
            }

            // 2. 初始化装饰组容器
            if (_preset.DecorationGroups != null)
            {
                foreach (var group in _preset.DecorationGroups)
                {
                    if (group == null || _decorationGroupContainers.ContainsKey(group)) continue;

                    GameObject groupObj = new GameObject($"Group_Decoration_{group.name}");
                    groupObj.transform.parent = _roomContainer.transform;
                    groupObj.transform.localPosition = Vector3.zero;
                    groupObj.transform.localRotation = Quaternion.identity;
                    groupObj.transform.localScale = Vector3.one;

                    int layerId = GetLayerFromMask(group.Layer);
                    if (layerId != -1)
                    {
                        groupObj.layer = layerId;
                    }

                    _decorationGroupContainers[group] = groupObj.transform;
                    _spawnedObjects.Add(groupObj);
                }
            }
        }

        /// <summary>
        /// 将扁平的一维画板网格数组转化为内部高效的网格坐标集合，并解析场景中的门销边界。
        /// </summary>
        private void ParseGridData()
        {
            _activeGridCoords.Clear();
            _doorGridCoords.Clear();

            if (gridData == null || gridData.Length != _canvasWidth * _canvasHeight)
            {
                gridData = new int[_canvasWidth * _canvasHeight];
            }

            for (int x = 0; x < _canvasWidth; x++)
            {
                for (int z = 0; z < _canvasHeight; z++)
                {
                    int index = x + z * _canvasWidth;
                    int cellValue = gridData[index];
                    if (cellValue != 0)
                    {
                        _activeGridCoords.Add(new Vector2Int(x, z));
                        if (cellValue == 2)
                        {
                            _doorGridCoords.Add(new Vector2Int(x, z));
                        }
                    }
                }
            }

            // 结合场景中的 DoorPin 组件，动态更新门格子标记
            IdentifyDoorPinsGrid();
        }

        /// <summary>
        /// 扫描场景中具有相同 generatorId 的 DoorPin，检测并把覆盖的激活网格标记为门。
        /// </summary>
        private void IdentifyDoorPinsGrid()
        {
            List<DoorPin> pins = new List<DoorPin>();

            // 1. 查找运行时的 DoorPin
            DoorPin[] activePins = FindObjectsByType<DoorPin>(FindObjectsSortMode.None);
            if (activePins != null)
            {
                pins.AddRange(activePins);
            }

            // 2. 编辑器非播放状态下，从场景及预制件树中查找
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var pin in GetComponentsInChildren<DoorPin>(true))
                {
                    if (pin != null && !pins.Contains(pin))
                    {
                        pins.Add(pin);
                    }
                }

                Transform root = transform.root;
                if (root != null && root != transform)
                {
                    foreach (var pin in root.GetComponentsInChildren<DoorPin>(true))
                    {
                        if (pin != null && !pins.Contains(pin))
                        {
                            pins.Add(pin);
                        }
                    }
                }
            }
#endif

            // 3. 执行范围交叉判定
            foreach (var pin in pins)
            {
                if (pin == null) continue;
                if (pin.generatorId != _generatorId) continue;

                foreach (var coord in _activeGridCoords)
                {
                    if (IsGridOverlappingWithDoorPin(coord.x, coord.y, pin))
                    {
                        _doorGridCoords.Add(coord);
                    }
                }
            }
        }

        /// <summary>
        /// 判断指定的网格格子是否与范围门销 (DoorPin) 的有向包围盒 (OBB) 发生重合，且水平占用超过瓦片边长的 10%。
        /// </summary>
        /// <param name="x">网格 X 坐标</param>
        /// <param name="z">网格 Z 坐标</param>
        /// <param name="pin">范围门销组件</param>
        /// <returns>若重叠占用比例达到 10% 以上则返回 true，否则返回 false</returns>
        private bool IsGridOverlappingWithDoorPin(int x, int z, DoorPin pin)
        {
            if (pin == null) return false;

            // 1. 计算格子在 RoomBuilder 局部空间下的 AABB 8 个顶点
            float xMin = x * _tileSize;
            float xMax = (x + 1f) * _tileSize;
            float yMin = 0f;
            float yMax = _roofHeight;
            float zMin = z * _tileSize;
            float zMax = (z + 1f) * _tileSize;

            Vector3[] gridCorners = new Vector3[8];
            gridCorners[0] = new Vector3(xMin, yMin, zMin);
            gridCorners[1] = new Vector3(xMax, yMin, zMin);
            gridCorners[2] = new Vector3(xMin, yMin, zMax);
            gridCorners[3] = new Vector3(xMax, yMin, zMax);
            gridCorners[4] = new Vector3(xMin, yMax, zMin);
            gridCorners[5] = new Vector3(xMax, yMax, zMin);
            gridCorners[6] = new Vector3(xMin, yMax, zMax);
            gridCorners[7] = new Vector3(xMax, yMax, zMax);

            // 2. 将格子的 8 个角点转换到 DoorPin 本地坐标系下，计算格子在此空间下的投影包络 (AABB)
            float gridLocalXMin = float.MaxValue;
            float gridLocalXMax = float.MinValue;
            float gridLocalYMin = float.MaxValue;
            float gridLocalYMax = float.MinValue;
            float gridLocalZMin = float.MaxValue;
            float gridLocalZMax = float.MinValue;

            for (int i = 0; i < 8; i++)
            {
                Vector3 worldPt = transform.TransformPoint(gridCorners[i]);
                Vector3 localPt = pin.transform.InverseTransformPoint(worldPt);

                if (localPt.x < gridLocalXMin) gridLocalXMin = localPt.x;
                if (localPt.x > gridLocalXMax) gridLocalXMax = localPt.x;
                if (localPt.y < gridLocalYMin) gridLocalYMin = localPt.y;
                if (localPt.y > gridLocalYMax) gridLocalYMax = localPt.y;
                if (localPt.z < gridLocalZMin) gridLocalZMin = localPt.z;
                if (localPt.z > gridLocalZMax) gridLocalZMax = localPt.z;
            }

            // 3. 计算在 DoorPin 局部空间下的三轴重叠区间大小
            float pinMinX = pin.positionOffset.x;
            float pinMaxX = pin.positionOffset.x + pin.boundsSize.x;
            float pinMinY = pin.positionOffset.y;
            float pinMaxY = pin.positionOffset.y + pin.boundsSize.y;
            float pinMinZ = pin.positionOffset.z;
            float pinMaxZ = pin.positionOffset.z + pin.boundsSize.z;

            float overlapX = Mathf.Min(pinMaxX, gridLocalXMax) - Mathf.Max(pinMinX, gridLocalXMin);
            float overlapY = Mathf.Min(pinMaxY, gridLocalYMax) - Mathf.Max(pinMinY, gridLocalYMin);
            float overlapZ = Mathf.Min(pinMaxZ, gridLocalZMax) - Mathf.Max(pinMinZ, gridLocalZMin);

            // 如果在任何一个轴上重叠小于等于 0，则说明没有物理交集
            if (overlapX <= 0f || overlapY <= 0f || overlapZ <= 0f)
            {
                return false;
            }

            // 4. 水平占用比例判定阈值：占用超过瓦片边长的 10%
            float threshold = _tileSize * 0.1f;

            // 为防 DoorPin 自身某轴尺寸特别小（比如极细门框），我们与 DoorPin 本身尺寸进行 Min 计算，避免因阈值比 DoorPin 大而恒定判定为未重叠
            float requiredOverlapX = Mathf.Min(pin.boundsSize.x * 0.99f, threshold);
            float requiredOverlapZ = Mathf.Min(pin.boundsSize.z * 0.99f, threshold);

            return (overlapX >= requiredOverlapX && overlapZ >= requiredOverlapZ);
        }

        /// <summary>
        /// 结构件生成阶段 (Structure Pass)。
        /// 遍历激活格子，通过分析邻接格子的空气状态确定暴露面方向，随后放置加权筛选后的结构瓦片。
        /// </summary>
        private void SpawnStructures()
        {
            if (_preset == null || _preset.StructureGroups == null || _preset.StructureGroups.Count == 0)
            {
                Debug.LogWarning("未指定 RoomPreset 资源或结构组为空，无法生成结构瓦片。");
                return;
            }

            // 合并所有结构组，记录元素与其归属的组关系
            List<(StructureElement element, StructureGroup group)> allElements = new List<(StructureElement, StructureGroup)>();
            foreach (var group in _preset.StructureGroups)
            {
                if (group == null || group.Elements == null) continue;
                foreach (var elem in group.Elements)
                {
                    if (elem != null && elem.Prefab != null)
                    {
                        allElements.Add((elem, group));
                    }
                }
            }

            // 遍历并分析每个体素空气柱
            foreach (var coord in _activeGridCoords)
            {
                int x = coord.x;
                int z = coord.y;

                // 1. 生成天花板底面 (朝向向上暴露，即顶面贴面)
                SpawnStructureFace(x, z, _roofHeight, Quaternion.identity, VoxelFaceDirection.Up, allElements);

                // 2. 生成地板面 (朝向向下暴露，即底面贴面)
                SpawnStructureFace(x, z, 0f, Quaternion.identity, VoxelFaceDirection.Down, allElements);

                // 3. 四邻域空气边界检测以确定侧向墙壁贴面
                bool southEmpty = !_activeGridCoords.Contains(new Vector2Int(x, z - 1));
                bool westEmpty = !_activeGridCoords.Contains(new Vector2Int(x - 1, z));
                bool northEmpty = !_activeGridCoords.Contains(new Vector2Int(x, z + 1));
                bool eastEmpty = !_activeGridCoords.Contains(new Vector2Int(x + 1, z));
                bool isDoorCell = _doorGridCoords.Contains(coord);

                // 南面墙体
                if (southEmpty && !isDoorCell)
                {
                    SpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 0f, 0f), VoxelFaceDirection.Back, allElements, new Vector3(0.5f, 0f, 0f));
                }
                // 西面墙体
                if (westEmpty && !isDoorCell)
                {
                    SpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 90f, 0f), VoxelFaceDirection.Left, allElements, new Vector3(0f, 0f, 0.5f));
                }
                // 北面墙体
                if (northEmpty && !isDoorCell)
                {
                    SpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 180f, 0f), VoxelFaceDirection.Forward, allElements, new Vector3(0.5f, 0f, 1.0f));
                }
                // 东面墙体
                if (eastEmpty && !isDoorCell)
                {
                    SpawnStructureFace(x, z, 0f, Quaternion.Euler(0f, 270f, 0f), VoxelFaceDirection.Right, allElements, new Vector3(1.0f, 0f, 0.5f));
                }
            }
        }

        /// <summary>
        /// 在指定的网格面上生成对应的结构瓦片，应用逻辑父锚点变换进行定位旋转。
        /// 对齐方向直接由所属结构组 (group.SupportedDirections) 进行位掩码过滤判定。
        /// </summary>
        private void SpawnStructureFace(
            int gridX,
            int gridZ,
            float height,
            Quaternion baseRotation,
            VoxelFaceDirection direction,
            List<(StructureElement element, StructureGroup group)> allElements,
            Vector3 offsetFraction = default)
        {
            if (offsetFraction == default)
            {
                offsetFraction = new Vector3(0.5f, 0f, 0.5f);
            }

            // 筛选适用此暴露方向的结构件（根据组的支持朝向位掩码判定）
            List<(StructureElement element, StructureGroup group)> candidates = new List<(StructureElement, StructureGroup)>();
            foreach (var item in allElements)
            {
                if ((item.group.SupportedDirections & direction) != VoxelFaceDirection.None)
                {
                    candidates.Add(item);
                }
            }

            if (candidates.Count == 0) return;

            // 加权随机选择一个结构瓦片
            (StructureElement element, StructureGroup group) selected = GetWeightedElement(candidates);
            if (selected.element == null || selected.element.Prefab == null) return;

            StructureElement config = selected.element;

            // 1. 计算出此面在局部空间中的物理位置
            Vector3 localPos = new Vector3(
                (gridX + offsetFraction.x) * _tileSize,
                height,
                (gridZ + offsetFraction.z) * _tileSize
            );
            Vector3 worldPos = transform.TransformPoint(localPos);

            // 2. 创建用于解耦坐标旋转与偏移的空“逻辑父锚点”
            string pivotName = $"{direction}_{gridX}_{gridZ}_Pivot";
            GameObject pivotObj = new GameObject(pivotName);

            // 分类整理：将 Pivot 逻辑锚点挂载在对应的结构组容器下
            Transform groupContainer = _structureGroupContainers.ContainsKey(selected.group) ?
                                       _structureGroupContainers[selected.group] : _roomContainer.transform;
            pivotObj.transform.parent = groupContainer;
            pivotObj.transform.position = worldPos;

            // 处理地板或天花板等对齐网格的 90 度步进离散随机旋转
            Quaternion targetRot = transform.rotation * baseRotation;
            if (config.Random90DegreeRotation && (direction == VoxelFaceDirection.Up || direction == VoxelFaceDirection.Down))
            {
                int rotIndex = Random.Range(0, 4);
                targetRot *= Quaternion.Euler(0f, rotIndex * 90f, 0f);
            }
            pivotObj.transform.rotation = targetRot;

            // 3. 将美术 Prefab 实例化为子物体，并应用本地偏差 (localPosition/localRotation)
            GameObject artObj;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                artObj = UnityEditor.PrefabUtility.InstantiatePrefab(config.Prefab) as GameObject;
            }
            else
            {
                artObj = Instantiate(config.Prefab);
            }
#else
            artObj = Instantiate(config.Prefab);
#endif
            if (artObj != null)
            {
                artObj.transform.parent = pivotObj.transform;
                artObj.transform.localPosition = config.PositionOffset;
                artObj.transform.localRotation = Quaternion.Euler(config.RotationOffset);
                artObj.transform.localScale = Vector3.one;

                // 4. 将缩放覆盖 (scaleOverride) 直接赋予父锚点，保证拼接严密无缝
                pivotObj.transform.localScale = config.ScaleOverride;

                // 5. 递归应用组指定的 Layer 图层
                int layerId = GetLayerFromMask(selected.group.Layer);
                if (layerId != -1)
                {
                    SetLayerRecursively(pivotObj, layerId);
                }

                _spawnedObjects.Add(pivotObj);
                _spawnedObjects.Add(artObj);
                _spawnedStructures.Add((artObj, selected.group));
            }
        }

        /// <summary>
        /// 装饰品生成阶段 (Decoration Pass)。
        /// 扫描所有已放置结构体，并在内存中通过包围盒切分动态推导首代虚拟插槽，多轮迭代平行化摆放与空间重叠排斥校验。
        /// </summary>
        private void SpawnDecorations()
        {
            if (_preset == null || _preset.DecorationGroups == null || _preset.DecorationGroups.Count == 0)
            {
                return;
            }

            // 合并所有引入装饰组中的摆件规则并提取归属组
            List<(DecorationElement config, DecorationGroup group)> allDecorConfigs = new List<(DecorationElement, DecorationGroup)>();
            foreach (var group in _preset.DecorationGroups)
            {
                if (group == null || group.Decorations == null) continue;
                foreach (var d in group.Decorations)
                {
                    if (d != null && d.Prefab != null)
                    {
                        allDecorConfigs.Add((d, group));
                    }
                }
            }

            if (allDecorConfigs.Count == 0) return;

            // 记录当前 Preset 下每种装饰品已经生成的数量统计缓存
            Dictionary<DecorationElement, int> spawnCounts = new Dictionary<DecorationElement, int>();
            foreach (var item in allDecorConfigs)
            {
                spawnCounts[item.config] = 0;
            }

            // 多轮 Pass 递归生成虚拟插槽列表
            List<VirtualSlot> currentSlots = new List<VirtualSlot>();

            // 递归首层：扫描已生成的结构瓦片并自动计算虚拟插槽
            foreach (var item in _spawnedStructures)
            {
                if (item.artObj == null || item.group == null || item.group.ProvidedSlotType == null) continue;
                List<VirtualSlot> slots = GenerateVirtualSlotsForObject(item.artObj, item.group.ProvidedSlotType);
                currentSlots.AddRange(slots);
            }

            SpawnDecorationsRecursive(currentSlots, allDecorConfigs, spawnCounts, 1);
        }

        /// <summary>
        /// 递归扫描并生成装饰件。
        /// 所有生成的装饰件物理坐标与插槽世界坐标完美对齐，但物理层级直接挂在其所属组容器下，扁平不嵌套。
        /// 朝向与切分步长均由组和插槽类型规范在内存中统一维持。
        /// </summary>
        private void SpawnDecorationsRecursive(
            List<VirtualSlot> slotsToProcess,
            List<(DecorationElement config, DecorationGroup group)> allDecorConfigs,
            Dictionary<DecorationElement, int> spawnCounts,
            int currentDepth)
        {
            if (currentDepth > _maxRecursionDepth || slotsToProcess.Count == 0) return;

            List<VirtualSlot> nextLevelSlots = new List<VirtualSlot>();

            // 按照配置列表顺序顺次处理每一种装饰配置。列表前方的装饰品自然获得更高的插槽抢占优先级。
            foreach (var item in allDecorConfigs)
            {
                DecorationElement decorConfig = item.config;
                DecorationGroup decorGroup = item.group;

                // 1. 在配置的生成数量范围内，定额期望的生成总数
                int minAmt = Mathf.Min(decorConfig.AmountRange.x, decorConfig.AmountRange.y);
                int maxAmt = Mathf.Max(decorConfig.AmountRange.x, decorConfig.AmountRange.y);
                int targetCount = Random.Range(minAmt, maxAmt + 1);

                // 计算本种摆件在当前房间中还需要生成的数量
                int neededCount = targetCount - spawnCounts[decorConfig];
                if (neededCount <= 0) continue;

                // 2. 从当前层级尚未占用的插槽列表中，过滤出支持挂载当前物品的候选插槽（由组级 AllowedSlots 判定）
                List<VirtualSlot> eligibleSlots = new List<VirtualSlot>();
                foreach (var slot in slotsToProcess)
                {
                    if (slot == null || slot.SlotType == null) continue;

                    bool typeMatched = decorGroup.AllowedSlots != null && decorGroup.AllowedSlots.Contains(slot.SlotType);
                    if (typeMatched)
                    {
                        eligibleSlots.Add(slot);
                    }
                }

                if (eligibleSlots.Count == 0) continue;

                // 3. 将候选插槽顺序打乱，确保同一种物品在匹配插槽中的分布是随机自然的
                for (int i = eligibleSlots.Count - 1; i > 0; i--)
                {
                    int r = Random.Range(0, i + 1);
                    var temp = eligibleSlots[i];
                    eligibleSlots[i] = eligibleSlots[r];
                    eligibleSlots[r] = temp;
                }

                // 4. 尝试向这批候选插槽中塞入需要的物体，直到满足 neededCount
                int spawnedThisTurn = 0;
                foreach (var slot in eligibleSlots)
                {
                    if (spawnedThisTurn >= neededCount) break;

                    // 插槽本身的天然挂载方向
                    SlotDirection slotDir = slot.SlotType.DefaultDirection;

                    // 靠墙安全区检查 (仅当地板上 Up 朝向的插槽起效)
                    if (slotDir == SlotDirection.Up)
                    {
                        Vector3 localSlotPos = transform.InverseTransformPoint(slot.Position);
                        int gridX = Mathf.FloorToInt(localSlotPos.x / _tileSize);
                        int gridZ = Mathf.FloorToInt(localSlotPos.z / _tileSize);

                        if (GetChebyshevDistanceToWall(gridX, gridZ) < _decorSafeArea)
                        {
                            continue;
                        }
                    }

                    // 本地偏移坐标换算成预期世界坐标
                    Vector3 localSpawnPos = slot.Position + slot.Rotation * decorConfig.PositionOffset;

                    // 5. 跨种类物理体积避让/同类隔离判定
                    bool spacingOverlapped = false;
                    foreach (var occupied in _occupiedDecorPositions)
                    {
                        float dist = Vector3.Distance(localSpawnPos, occupied.position);

                        // A. 同类互斥：同一种摆件且配置了有效 spacing 间距
                        if (occupied.config == decorConfig && decorConfig.Spacing > 0f)
                        {
                            if (dist < decorConfig.Spacing)
                            {
                                spacingOverlapped = true;
                                break;
                            }
                        }

                        // B. 实体体积避让：如果自身为需要避开体积物，且对方是实体体积摆件
                        if (decorConfig.AvoidVolumeObjects && occupied.isVolumeObject)
                        {
                            if (dist < decorConfig.Spacing)
                            {
                                spacingOverlapped = true;
                                break;
                            }
                        }
                    }

                    if (spacingOverlapped) continue;

                    // 6. 执行物理实例化，使用父轴点逻辑定位
                    GameObject pivotObj = new GameObject($"{decorConfig.Prefab.name}_Pivot");
                    Transform groupContainer = _decorationGroupContainers.ContainsKey(decorGroup) ?
                                               _decorationGroupContainers[decorGroup] : _roomContainer.transform;
                    pivotObj.transform.parent = groupContainer;

                    pivotObj.transform.position = slot.Position;
                    pivotObj.transform.rotation = slot.Rotation;

                    GameObject artObj;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        artObj = UnityEditor.PrefabUtility.InstantiatePrefab(decorConfig.Prefab) as GameObject;
                    }
                    else
                    {
                        artObj = Instantiate(decorConfig.Prefab);
                    }
#else
                    artObj = Instantiate(decorConfig.Prefab);
#endif
                    if (artObj != null)
                    {
                        artObj.transform.parent = pivotObj.transform;
                        artObj.transform.localPosition = decorConfig.PositionOffset;

                        // 应用局部基础旋转偏置 + Y 轴无级旋转
                        float randomY = Random.Range(0f, decorConfig.RandomRotationY);
                        Quaternion localRot = Quaternion.Euler(decorConfig.RotationOffset) * Quaternion.Euler(0f, randomY, 0f);
                        artObj.transform.localRotation = localRot;

                        // 应用随机等比缩放
                        float randomScale = Random.Range(decorConfig.ScaleRange.x, decorConfig.ScaleRange.y);
                        artObj.transform.localScale = Vector3.one * randomScale;

                        // 递归应用指定的层级 Layer
                        int layerId = GetLayerFromMask(decorGroup.Layer);
                        if (layerId != -1)
                        {
                            SetLayerRecursively(pivotObj, layerId);
                        }

                        // 缓存与记录状态
                        _spawnedObjects.Add(pivotObj);
                        _spawnedObjects.Add(artObj);

                        _occupiedDecorPositions.Add(new OccupiedDecorData
                        {
                            position = localSpawnPos,
                            spacing = decorConfig.Spacing,
                            config = decorConfig,
                            isVolumeObject = decorConfig.IsVolumeObject
                        });

                        spawnCounts[decorConfig]++;
                        spawnedThisTurn++;

                        // 将该插槽从当前层的待处理插槽列表 slotsToProcess 中移除，确保不被后续低优先级物品占用
                        slotsToProcess.Remove(slot);

                        // 7. 若该摆件所属组能提供下级虚拟插槽，利用包围盒切分动态生成，加入下级递归
                        if (decorGroup.ProvidedSlotType != null)
                        {
                            List<VirtualSlot> childSlots = GenerateVirtualSlotsForObject(artObj, decorGroup.ProvidedSlotType);
                            nextLevelSlots.AddRange(childSlots);
                        }
                    }
                }
            }

            // 进入下一层级级联生成
            SpawnDecorationsRecursive(nextLevelSlots, allDecorConfigs, spawnCounts, currentDepth + 1);
        }

        /// <summary>
        /// 计算指定 GameObject 及其所有子 Renderer 在物体本地空间中的合并包围盒（Bounds）。
        /// </summary>
        private Bounds CalculateLocalBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds localBounds = new Bounds();
            bool hasBounds = false;
            Matrix4x4 worldToLocal = target.transform.worldToLocalMatrix;

            foreach (var r in renderers)
            {
                if (r == null) continue;

                Bounds worldBounds = r.bounds;
                Vector3[] worldCorners = GetBoundsCorners(worldBounds);
                foreach (var corner in worldCorners)
                {
                    Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localCorner);
                    }
                }
            }

            return localBounds;
        }

        /// <summary>
        /// 获取 Bounds 的 8 个世界顶点。
        /// </summary>
        private Vector3[] GetBoundsCorners(Bounds b)
        {
            return new Vector3[]
            {
                new Vector3(b.min.x, b.min.y, b.min.z),
                new Vector3(b.min.x, b.min.y, b.max.z),
                new Vector3(b.min.x, b.max.y, b.min.z),
                new Vector3(b.min.x, b.max.y, b.max.z),
                new Vector3(b.max.x, b.min.y, b.min.z),
                new Vector3(b.max.x, b.min.y, b.max.z),
                new Vector3(b.max.x, b.max.y, b.min.z),
                new Vector3(b.max.x, b.max.y, b.max.z)
            };
        }

        /// <summary>
        /// 针对非空提供的插槽类型，切分指定物体的包围盒并生成一组虚拟插槽。
        /// 会对每个虚拟插槽点在世界水平面上叠加微小的随机抖动。
        /// 融合了旧版的射线对齐思路：对于挂载了碰撞体的物体，从理想切分点朝物体内部/表面发射探测射线，
        /// 若未命中该物体的任何碰撞体则视为镂空区域，丢弃该插槽；若命中则贴合实际物理表面并修正位置与法线朝向。
        /// 若物体未挂载任何碰撞体，则安全降级为纯几何包围盒切分生成。
        /// </summary>
        private List<VirtualSlot> GenerateVirtualSlotsForObject(GameObject obj, SlotType providedSlotType)
        {
            List<VirtualSlot> slots = new List<VirtualSlot>();
            if (providedSlotType == null || obj == null) return slots;

            // 强制同步物理变换，确保刚刚生成的物体的 Collider 在 PhysX 物理场景中同步就绪
            Physics.SyncTransforms();

            // 获取当前物体及其子级的所有碰撞体
            Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
            bool hasColliders = colliders != null && colliders.Length > 0;

            Bounds localBounds = CalculateLocalBounds(obj);
            Vector3 size = localBounds.size;
            Vector3 min = localBounds.min;
            Vector3 max = localBounds.max;

            SlotDirection dir = providedSlotType.DefaultDirection;
            Transform trans = obj.transform;

            // 1. 若方向为 Up，在物体顶面切分生成虚拟插槽
            if ((dir & SlotDirection.Up) != 0)
            {
                int countX = Mathf.FloorToInt(size.x / _slotStepSize);
                int countZ = Mathf.FloorToInt(size.z / _slotStepSize);

                float startX = countX > 0 ? min.x + (size.x - (countX - 1) * _slotStepSize) * 0.5f : (min.x + max.x) * 0.5f;
                float startZ = countZ > 0 ? min.z + (size.z - (countZ - 1) * _slotStepSize) * 0.5f : (min.z + max.z) * 0.5f;

                int loopX = Mathf.Max(1, countX);
                int loopZ = Mathf.Max(1, countZ);

                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopZ; j++)
                    {
                        float localX = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float localZ = startZ + (countZ > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(localX, max.y, localZ);

                        Quaternion localRot = Quaternion.identity;
                        Vector3 worldPos = trans.TransformPoint(localPos);
                        Quaternion worldRot = trans.rotation * localRot;

                        // 射线检测有效性与位置贴合
                        if (hasColliders)
                        {
                            // 从理想坐标上方 0.5 米处垂直向下发射探测射线，距离设为包围盒高度加 1.0 米
                            Vector3 rayStart = worldPos + trans.up * 0.5f;
                            Vector3 rayDir = -trans.up;
                            float maxDist = size.y + 1.0f;

                            if (PerformObjectRaycast(rayStart, rayDir, maxDist, obj, out RaycastHit hit))
                            {
                                worldPos = hit.point;
                                worldRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * (trans.rotation * localRot);
                            }
                            // 降级兜底：若物理引擎在此处打空（如在 Prefab 隔离模式中），不进行 continue 剔除，直接保留几何默认坐标
                        }

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = worldRot
                        });
                    }
                }
            }

            // 2. 若方向为 Down，在物体底面切分生成虚拟插槽
            if ((dir & SlotDirection.Down) != 0)
            {
                int countX = Mathf.FloorToInt(size.x / _slotStepSize);
                int countZ = Mathf.FloorToInt(size.z / _slotStepSize);

                float startX = countX > 0 ? min.x + (size.x - (countX - 1) * _slotStepSize) * 0.5f : (min.x + max.x) * 0.5f;
                float startZ = countZ > 0 ? min.z + (size.z - (countZ - 1) * _slotStepSize) * 0.5f : (min.z + max.z) * 0.5f;

                int loopX = Mathf.Max(1, countX);
                int loopZ = Mathf.Max(1, countZ);

                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopZ; j++)
                    {
                        float localX = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float localZ = startZ + (countZ > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(localX, min.y, localZ);

                        Quaternion localRot = Quaternion.Euler(180f, 0f, 0f);
                        Vector3 worldPos = trans.TransformPoint(localPos);
                        Quaternion worldRot = trans.rotation * localRot;

                        // 射线检测有效性与位置贴合
                        if (hasColliders)
                        {
                            // 从理想坐标下方 0.5 米处垂直向上发射探测射线
                            Vector3 rayStart = worldPos - trans.up * 0.5f;
                            Vector3 rayDir = trans.up;
                            float maxDist = size.y + 1.0f;

                            if (PerformObjectRaycast(rayStart, rayDir, maxDist, obj, out RaycastHit hit))
                            {
                                worldPos = hit.point;
                                worldRot = Quaternion.FromToRotation(-Vector3.up, hit.normal) * (trans.rotation * localRot);
                            }
                        }

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = worldRot
                        });
                    }
                }
            }

            // 3. 若方向为 Horizontal，在物体四个侧面生成虚拟插槽
            if ((dir & SlotDirection.Horizontal) != 0)
            {
                int countX = Mathf.FloorToInt(size.x / _slotStepSize);
                int countY = Mathf.FloorToInt(size.y / _slotStepSize);
                int countZ = Mathf.FloorToInt(size.z / _slotStepSize);

                float startX = countX > 0 ? min.x + (size.x - (countX - 1) * _slotStepSize) * 0.5f : (min.x + max.x) * 0.5f;
                float startY = countY > 0 ? min.y + (size.y - (countY - 1) * _slotStepSize) * 0.5f : (min.y + max.y) * 0.5f;
                float startZ = countZ > 0 ? min.z + (size.z - (countZ - 1) * _slotStepSize) * 0.5f : (min.z + max.z) * 0.5f;

                int loopX = Mathf.Max(1, countX);
                int loopY = Mathf.Max(1, countY);
                int loopZ = Mathf.Max(1, countZ);

                // A. 前面 (Z = max.z)
                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float localX = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float localY = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(localX, localY, max.z);
                        Quaternion localRot = Quaternion.FromToRotation(Vector3.up, Vector3.forward);

                        Vector3 worldPos = trans.TransformPoint(localPos);
                        Quaternion worldRot = trans.rotation * localRot;

                        if (hasColliders)
                        {
                            // 从理想坐标前方 0.5 米处向物体内部（后方）发射射线
                            Vector3 rayStart = worldPos + trans.forward * 0.5f;
                            Vector3 rayDir = -trans.forward;
                            float maxDist = size.z + 1.0f;

                            if (PerformObjectRaycast(rayStart, rayDir, maxDist, obj, out RaycastHit hit))
                            {
                                worldPos = hit.point;
                                worldRot = Quaternion.FromToRotation(Vector3.forward, hit.normal) * (trans.rotation * localRot);
                            }
                        }

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = worldRot
                        });
                    }
                }

                // B. 后面 (Z = min.z)
                for (int i = 0; i < loopX; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float localX = startX + (countX > 0 ? i * _slotStepSize : 0f);
                        float localY = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(localX, localY, min.z);
                        Quaternion localRot = Quaternion.FromToRotation(Vector3.up, Vector3.back);

                        Vector3 worldPos = trans.TransformPoint(localPos);
                        Quaternion worldRot = trans.rotation * localRot;

                        if (hasColliders)
                        {
                            // 从理想坐标后方 0.5 米处向物体内部（前方）发射射线
                            Vector3 rayStart = worldPos - trans.forward * 0.5f;
                            Vector3 rayDir = trans.forward;
                            float maxDist = size.z + 1.0f;

                            if (PerformObjectRaycast(rayStart, rayDir, maxDist, obj, out RaycastHit hit))
                            {
                                worldPos = hit.point;
                                worldRot = Quaternion.FromToRotation(Vector3.back, hit.normal) * (trans.rotation * localRot);
                            }
                        }

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = worldRot
                        });
                    }
                }

                // C. 左面 (X = min.x)
                for (int i = 0; i < loopZ; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float localZ = startZ + (countZ > 0 ? i * _slotStepSize : 0f);
                        float localY = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(min.x, localY, localZ);
                        Quaternion localRot = Quaternion.FromToRotation(Vector3.up, Vector3.left);

                        Vector3 worldPos = trans.TransformPoint(localPos);
                        Quaternion worldRot = trans.rotation * localRot;

                        if (hasColliders)
                        {
                            // 从理想坐标左方 0.5 米处向物体内部（右方）发射射线
                            Vector3 rayStart = worldPos - trans.right * 0.5f;
                            Vector3 rayDir = trans.right;
                            float maxDist = size.x + 1.0f;

                            if (PerformObjectRaycast(rayStart, rayDir, maxDist, obj, out RaycastHit hit))
                            {
                                worldPos = hit.point;
                                worldRot = Quaternion.FromToRotation(Vector3.left, hit.normal) * (trans.rotation * localRot);
                            }
                        }

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = worldRot
                        });
                    }
                }

                // D. 右面 (X = max.x)
                for (int i = 0; i < loopZ; i++)
                {
                    for (int j = 0; j < loopY; j++)
                    {
                        float localZ = startZ + (countZ > 0 ? i * _slotStepSize : 0f);
                        float localY = startY + (countY > 0 ? j * _slotStepSize : 0f);
                        Vector3 localPos = new Vector3(max.x, localY, localZ);
                        Quaternion localRot = Quaternion.FromToRotation(Vector3.up, Vector3.right);

                        Vector3 worldPos = trans.TransformPoint(localPos);
                        Quaternion worldRot = trans.rotation * localRot;

                        if (hasColliders)
                        {
                            // 从理想坐标右方 0.5 米处向物体内部（左方）发射射线
                            Vector3 rayStart = worldPos + trans.right * 0.5f;
                            Vector3 rayDir = -trans.right;
                            float maxDist = size.x + 1.0f;

                            if (PerformObjectRaycast(rayStart, rayDir, maxDist, obj, out RaycastHit hit))
                            {
                                worldPos = hit.point;
                                worldRot = Quaternion.FromToRotation(Vector3.right, hit.normal) * (trans.rotation * localRot);
                            }
                        }

                        worldPos.x += Random.Range(-_slotJitter, _slotJitter);
                        worldPos.z += Random.Range(-_slotJitter, _slotJitter);

                        slots.Add(new VirtualSlot
                        {
                            SlotType = providedSlotType,
                            Position = worldPos,
                            Rotation = worldRot
                        });
                    }
                }
            }

            return slots;
        }

        /// <summary>
        /// 执行一次射线探测，并且只筛选打在 target 物体自身（或其子物体）碰撞体上的最近撞击点。
        /// </summary>
        private bool PerformObjectRaycast(Vector3 start, Vector3 direction, float maxDistance, GameObject target, out RaycastHit closestHit)
        {
            closestHit = new RaycastHit();
            RaycastHit[] hits = Physics.RaycastAll(start, direction, maxDistance);
            if (hits == null || hits.Length == 0) return false;

            float minDistance = float.MaxValue;
            bool hitFound = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                // 判断被碰物体是否是 target 本身，或者是其子节点
                if (hit.collider.gameObject == target || hit.collider.transform.IsChildOf(target.transform))
                {
                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        closestHit = hit;
                        hitFound = true;
                    }
                }
            }

            return hitFound;
        }

        /// <summary>
        /// 基于权重加权，从候选列表中随机筛选出一个元素。
        /// 用于在生成结构暴露面时加权选择不同的结构件变体。
        /// </summary>
        private (StructureElement config, StructureGroup group) GetWeightedElement(List<(StructureElement config, StructureGroup group)> candidates)
        {
            int totalWeight = 0;
            foreach (var item in candidates)
            {
                totalWeight += item.config.Weight;
            }

            if (totalWeight <= 0)
            {
                return candidates[Random.Range(0, candidates.Count)];
            }

            int choice = Random.Range(0, totalWeight);
            int currentSum = 0;
            foreach (var item in candidates)
            {
                currentSum += item.config.Weight;
                if (choice < currentSum)
                {
                    return item;
                }
            }

            return candidates[candidates.Count - 1];
        }

        /// <summary>
        /// 递归设置当前游戏对象及其所有子节点的游戏图层层级。
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        /// <summary>
        /// 从 LayerMask 取得第一个被选中的 Layer 索引。
        /// </summary>
        private int GetLayerFromMask(LayerMask mask)
        {
            int maskValue = mask.value;
            if (maskValue == 0) return -1;
            for (int i = 0; i < 32; i++)
            {
                if (((maskValue >> i) & 1) == 1)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 计算指定坐标格子与所有外墙边界格子的切比雪夫距离。
        /// </summary>
        private int GetChebyshevDistanceToWall(int gridX, int gridZ)
        {
            int minDistance = int.MaxValue;

            for (int x = -1; x <= _canvasWidth; x++)
            {
                for (int z = -1; z <= _canvasHeight; z++)
                {
                    Vector2Int targetCoord = new Vector2Int(x, z);

                    if (!_activeGridCoords.Contains(targetCoord))
                    {
                        int distance = Mathf.Max(Mathf.Abs(gridX - x), Mathf.Abs(gridZ - z));
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }
                }
            }

            return minDistance;
        }

        #endregion
    }

    /// <summary>
    /// 虚拟出的插槽数据结构，用于扁平化非侵入性的自动插槽生成。
    /// </summary>
    public class VirtualSlot
    {
        /// <summary>
        /// 插槽语义类型。
        /// </summary>
        public SlotType SlotType;

        /// <summary>
        /// 虚拟出的世界坐标点。
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// 虚拟出的世界朝向旋转。
        /// </summary>
        public Quaternion Rotation;
    }
}
