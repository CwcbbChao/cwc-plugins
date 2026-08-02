using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    /// <summary>
    /// 网格瓷砖类型枚举。
    /// </summary>
    public enum TileType
    {
        Floor,
        Wall,
        WallCorner,
        Roof,
        Door
    }

    /// <summary>
    /// 装饰点投放类型枚举。
    /// </summary>
    public enum PointType
    {
        Floor,
        Wall,
        Roof
    }

    /// <summary>
    /// 核心房间生成器组件。
    /// 根据画板上激活的网格坐标，自动进行边界包边、生成墙体地板、范围门销剔除、并在网格表面均匀投放防重叠的装饰道具。
    /// </summary>
    [AddComponentMenu("Cwcbb/Room Builder/Room Generator")]
    public class RoomGenerator : MonoBehaviour
    {
        #region 1. 常量与静态字段
        // 当前组件无常量与静态字段
        #endregion

        #region 2. 序列化属性与字段 (Inspector 中显示的字段)

        [Header("核心数据配置")]
        [Tooltip("房间美术资源配置预设")]
        public RoomPreset preset;

        [Tooltip("关卡物理图层配置")]
        public RoomLayerConfig layerConfig;

        [Header("画板网格尺寸")]
        [Tooltip("画板的宽度（格子数量）")]
        [Range(1, 100)]
        public int canvasWidth = 10;

        [Tooltip("画板的高度（格子数量）")]
        [Range(1, 100)]
        public int canvasHeight = 10;

        [Tooltip("网格中单个格子的物理边长（单位：米）")]
        public float tileSize = 3.0f;

        [Tooltip("屋顶瓦片的局部高度物理偏移量")]
        public float roofHeight = 3.0f;

        [Tooltip("当网格绘制了门但未配置门预制件时，是否降级生成常规墙面。若为 false 则在此边界处留空（作为空气通道）")]
        public bool fallbackToWallIfNoDoor = false;

        [Header("装点物理配置")]
        [Tooltip("墙壁挂载件相对墙面的向前偏移量，避免贴图闪烁（Z-Fighting）")]
        public float wallDecorOffset = 0.2f;

        [Tooltip("地板挂载件相对地板的向上偏移量")]
        public float floorDecorOffset = 0.0f;

        [Tooltip("地板饰品到墙壁物体的最小安全距离（格数）。安全距离越大，饰品离外墙越远")]
        [Range(0, 5)]
        public int decorSafeArea = 1;

        [Tooltip("用于计算装饰点生成的密度等级，越高点越密")]
        [Range(1, 3)]
        public int pointSpacing = 1;

        [Tooltip("调试模式下是否在场景中绘制装饰点的物理坐标")]
        public bool debug = true;

        [Tooltip("房间生成器标识 ID，用于与 DoorPin 的 generatorId 进行匹配")]
        public int generatorId = 0;

        [HideInInspector]
        [Tooltip("可视化画板直接操作的扁平化网格一维数组数据源")]
        public int[] gridData;

        #endregion

        #region 3. 非序列化受保护字段 (Protected Fields)

        /// <summary>
        /// 激活的网格坐标集合。
        /// </summary>
        protected HashSet<Vector2Int> _activeGridCoords = new HashSet<Vector2Int>();

        /// <summary>
        /// 激活的门网格坐标集合。
        /// </summary>
        protected HashSet<Vector2Int> _doorGridCoords = new HashSet<Vector2Int>();

        /// <summary>
        /// 已生成的所有游戏对象列表缓存。
        /// </summary>
        protected List<GameObject> _spawnedObjects = new List<GameObject>();

        /// <summary>
        /// 已生成的墙壁对象列表，用于装饰品的安全区距离检测。
        /// </summary>
        protected List<GameObject> _spawnedWallObjects = new List<GameObject>();

        /// <summary>
        /// 计算出的可用装饰点列表。
        /// </summary>
        protected List<DecoratorPoint> _decoratorPoints = new List<DecoratorPoint>();

        /// <summary>
        /// 所有生成内容的本地父级托管节点。
        /// </summary>
        protected GameObject _roomContainer;

        /// <summary>
        /// 地板瓦片托管节点。
        /// </summary>
        protected GameObject _floorTileParent;

        /// <summary>
        /// 墙体瓦片托管节点。
        /// </summary>
        protected GameObject _wallTileParent;

        /// <summary>
        /// 屋顶瓦片托管节点。
        /// </summary>
        protected GameObject _roofTileParent;

        /// <summary>
        /// 门瓦片托管节点。
        /// </summary>
        protected GameObject _doorParent;

        /// <summary>
        /// 地板装饰托管节点。
        /// </summary>
        protected GameObject _floorDecorParent;

        /// <summary>
        /// 墙壁装饰托管节点。
        /// </summary>
        protected GameObject _wallDecorParent;

        /// <summary>
        /// 屋顶装饰托管节点。
        /// </summary>
        protected GameObject _roofDecorParent;

        /// <summary>
        /// 角色托管节点。
        /// </summary>
        protected GameObject _characterParent;

        /// <summary>
        /// 用于在生成过程中暂存待计算装饰点物体的信息结构。
        /// </summary>
        protected struct PendingDecorPointData
        {
            public GameObject tileObj;
            public Tile tileConfig;
            public PointType pointType;
        }

        protected List<PendingDecorPointData> _pendingFloorRoofDecors = new List<PendingDecorPointData>();
        protected List<PendingDecorPointData> _pendingWallDecors = new List<PendingDecorPointData>();

        #endregion

        #region 4. 公共属性 (Properties)

        /// <summary>
        /// 获取当前已生成的所有对象。
        /// </summary>
        public List<GameObject> SpawnedObjects => _spawnedObjects;

        #endregion

        #region 5. 生命周期方法 (Unity Lifecycle)

        /// <summary>
        /// 调试模式下在 SceneView 中绘制当前计算出的装饰挂接点。
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            if (!debug || _decoratorPoints == null) return;

            foreach (var pt in _decoratorPoints)
            {
                if (pt.pointType == PointType.Floor)
                {
                    Gizmos.color = pt.occupied ? Color.red : Color.green;
                    Gizmos.DrawWireSphere(pt.position, 0.12f);
                }
                else if (pt.pointType == PointType.Wall)
                {
                    Gizmos.color = pt.occupied ? Color.red : Color.yellow;
                    Gizmos.DrawWireSphere(pt.position, 0.12f);
                }
                else if (pt.pointType == PointType.Roof)
                {
                    Gizmos.color = pt.occupied ? Color.red : Color.blue;
                    Gizmos.DrawWireSphere(pt.position, 0.12f);
                }
            }
        }

        /// <summary>
        /// 选中该物体时，在 Scene 视图中绘制地板的逻辑网格以及已激活格子的线框。
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            // 保存原有的 Gizmos 矩阵并应用当前组件的局部到世界转换矩阵，以适应旋转和偏移
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            // 1. 绘制网格背景线（棋盘格）
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);

            // 绘制横线 (沿 X 轴方向延伸，分布在不同 Z 上)
            for (int z = 0; z <= canvasHeight; z++)
            {
                Vector3 start = new Vector3(0f, 0f, z * tileSize);
                Vector3 end = new Vector3(canvasWidth * tileSize, 0f, z * tileSize);
                Gizmos.DrawLine(start, end);
            }

            // 绘制竖线 (沿 Z 轴方向延伸，分布在不同 X 上)
            for (int x = 0; x <= canvasWidth; x++)
            {
                Vector3 start = new Vector3(x * tileSize, 0f, 0f);
                Vector3 end = new Vector3(x * tileSize, 0f, canvasHeight * tileSize);
                Gizmos.DrawLine(start, end);
            }

            // 2. 绘制激活格子的线框（区分普通格子与门格子，不绘制填充面以避免遮挡视线）
            if (gridData != null && gridData.Length == canvasWidth * canvasHeight)
            {
                for (int x = 0; x < canvasWidth; x++)
                {
                    for (int z = 0; z < canvasHeight; z++)
                    {
                        int index = x + z * canvasWidth;
                        int cellValue = gridData[index];
                        if (cellValue != 0)
                        {
                            Vector3 center = new Vector3((x + 0.5f) * tileSize, 0f, (z + 0.5f) * tileSize);
                            Vector3 size = new Vector3(tileSize * 0.95f, 0f, tileSize * 0.95f); // 高度设为 0 以绘制纯平面矩形线框

                            if (cellValue == 2) // 门通道
                            {
                                Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);  // 橙红色轮廓
                                Gizmos.DrawWireCube(center, size);
                            }
                            else // 普通房间格子
                            {
                                Gizmos.color = new Color(0f, 0.8f, 1f, 0.7f);  // 亮蓝色轮廓
                                Gizmos.DrawWireCube(center, size);
                            }
                        }
                    }
                }
            }

            // 恢复原有的 Gizmos 矩阵
            Gizmos.matrix = oldMatrix;
        }

        #endregion

        #region 6. 公共方法 (Public Methods)

        /// <summary>
        /// 核心生成流水线入口。
        /// </summary>
        public virtual void Generate()
        {
            Clear();
            InitContainer();
            ParseGridData();
            SpawnFloorsAndWalls();
            CollectAllDecoratorPoints(); // 所有结构件放置完毕且墙体列表完整后，统一计算并收集装饰点
            SpawnDecorations();
        }

        /// <summary>
        /// 清理上次生成的房间瓦片与装饰物，重置所有内部状态。
        /// </summary>
        public virtual void Clear()
        {
            if (_roomContainer != null)
            {
                DestroyImmediate(_roomContainer);
                _roomContainer = null;
            }

            // 防御性清理：在编辑器或重新加载预制件后，私有引用 _roomContainer 会变为 null，但子节点仍然存在。
            // 我们通过遍历子物体，将所有名为 "Room_Content_Container" 的残留容器彻底销毁
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
                DestroyImmediate(orphan);
            }

            _floorTileParent = null;
            _wallTileParent = null;
            _roofTileParent = null;
            _doorParent = null;
            _floorDecorParent = null;
            _wallDecorParent = null;
            _roofDecorParent = null;
            _characterParent = null;

            _spawnedObjects.Clear();
            _spawnedWallObjects.Clear();
            _decoratorPoints.Clear();
            _activeGridCoords.Clear();

            _pendingFloorRoofDecors.Clear();
            _pendingWallDecors.Clear();
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
                for (int x = 0; x < Mathf.Min(canvasWidth, newWidth); x++)
                {
                    for (int z = 0; z < Mathf.Min(canvasHeight, newHeight); z++)
                    {
                        newGrid[x + z * newWidth] = gridData[x + z * canvasWidth];
                    }
                }
            }

            gridData = newGrid;
            canvasWidth = newWidth;
            canvasHeight = newHeight;
        }

        #endregion

        #region 7. 私有与受保护的虚方法 (Protected Virtual Methods)

        /// <summary>
        /// 初始化挂接容器。
        /// </summary>
        protected virtual void InitContainer()
        {
            _roomContainer = new GameObject("Room_Content_Container");
            _roomContainer.transform.parent = this.transform;
            _roomContainer.transform.localPosition = Vector3.zero;
            _roomContainer.transform.localRotation = Quaternion.identity;

            _floorTileParent = new GameObject("floor tiles");
            _floorTileParent.transform.parent = _roomContainer.transform;
            _floorTileParent.transform.localPosition = Vector3.zero;
            _floorTileParent.transform.localRotation = Quaternion.identity;

            _wallTileParent = new GameObject("wall tiles");
            _wallTileParent.transform.parent = _roomContainer.transform;
            _wallTileParent.transform.localPosition = Vector3.zero;
            _wallTileParent.transform.localRotation = Quaternion.identity;

            _roofTileParent = new GameObject("roof tiles");
            _roofTileParent.transform.parent = _roomContainer.transform;
            _roofTileParent.transform.localPosition = Vector3.zero;
            _roofTileParent.transform.localRotation = Quaternion.identity;

            _doorParent = new GameObject("doors");
            _doorParent.transform.parent = _roomContainer.transform;
            _doorParent.transform.localPosition = Vector3.zero;
            _doorParent.transform.localRotation = Quaternion.identity;

            _floorDecorParent = new GameObject("floor decor");
            _floorDecorParent.transform.parent = _roomContainer.transform;
            _floorDecorParent.transform.localPosition = Vector3.zero;
            _floorDecorParent.transform.localRotation = Quaternion.identity;

            _wallDecorParent = new GameObject("wall decor");
            _wallDecorParent.transform.parent = _roomContainer.transform;
            _wallDecorParent.transform.localPosition = Vector3.zero;
            _wallDecorParent.transform.localRotation = Quaternion.identity;

            _roofDecorParent = new GameObject("roof decor");
            _roofDecorParent.transform.parent = _roomContainer.transform;
            _roofDecorParent.transform.localPosition = Vector3.zero;
            _roofDecorParent.transform.localRotation = Quaternion.identity;

            _characterParent = new GameObject("characters");
            _characterParent.transform.parent = _roomContainer.transform;
            _characterParent.transform.localPosition = Vector3.zero;
            _characterParent.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// 将画板一维 bool 数组解析为具体的网格坐标集合。
        /// </summary>
        protected virtual void ParseGridData()
        {
            _activeGridCoords.Clear();
            _doorGridCoords.Clear();
            if (gridData == null || gridData.Length != canvasWidth * canvasHeight)
            {
                gridData = new int[canvasWidth * canvasHeight];
            }

            for (int x = 0; x < canvasWidth; x++)
            {
                for (int z = 0; z < canvasHeight; z++)
                {
                    int index = x + z * canvasWidth;
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

            // 结合场景中的 DoorPin，将覆盖的格子自动识别为 Door 格子
            IdentifyDoorPinsGrid();
        }

        /// <summary>
        /// 物理边界判定与主结构件（地板、墙体、屋顶）生成逻辑。
        /// </summary>
        protected virtual void SpawnFloorsAndWalls()
        {
            if (preset == null)
            {
                Debug.LogWarning("未指定 RoomPreset 资源，无法生成结构瓦片。");
                return;
            }

            List<RoomNode> structureNodes = new List<RoomNode>();

            // 1. 生成预备节点
            foreach (var coord in _activeGridCoords)
            {
                int x = coord.x;
                int z = coord.y;

                // 地板节点（格子几何中心）
                structureNodes.Add(new RoomNode
                {
                    position = new Vector3((x + 0.5f) * tileSize, 0f, (z + 0.5f) * tileSize),
                    rotation = Quaternion.identity,
                    tileType = TileType.Floor,
                    gridPos = coord
                });

                // 屋顶节点（格子几何中心）
                structureNodes.Add(new RoomNode
                {
                    position = new Vector3((x + 0.5f) * tileSize, roofHeight, (z + 0.5f) * tileSize),
                    rotation = Quaternion.identity,
                    tileType = TileType.Roof,
                    gridPos = coord
                });

                // 四邻域检测确定是否需要包裹外壁
                bool southEmpty = !_activeGridCoords.Contains(new Vector2Int(x, z - 1));
                bool westEmpty = !_activeGridCoords.Contains(new Vector2Int(x - 1, z));
                bool northEmpty = !_activeGridCoords.Contains(new Vector2Int(x, z + 1));
                bool eastEmpty = !_activeGridCoords.Contains(new Vector2Int(x + 1, z));

                bool hasCorners = (preset.wallCorners != null && preset.wallCorners.Count > 0 && preset.wallCorners.Exists(w => w.prefab != null));

                // 精准拐角检测
                bool isSW = southEmpty && westEmpty && !northEmpty && !eastEmpty;
                bool isSE = southEmpty && eastEmpty && !northEmpty && !westEmpty;
                bool isNE = northEmpty && eastEmpty && !southEmpty && !westEmpty;
                bool isNW = northEmpty && westEmpty && !southEmpty && !eastEmpty;

                bool isDoorCell = _doorGridCoords.Contains(coord);

                if (hasCorners && !isDoorCell && (isSW || isSE || isNE || isNW))
                {
                    float angle = 0f;
                    Vector3 cornerPos = Vector3.zero;

                    if (isSW)
                    {
                        angle = 0f;
                        cornerPos = new Vector3(x * tileSize, 0f, z * tileSize); // 左下角顶点
                    }
                    else if (isNW)
                    {
                        angle = 90f;
                        cornerPos = new Vector3(x * tileSize, 0f, (z + 1.0f) * tileSize); // 左上角顶点
                    }
                    else if (isNE)
                    {
                        angle = 180f;
                        cornerPos = new Vector3((x + 1.0f) * tileSize, 0f, (z + 1.0f) * tileSize); // 右上角顶点
                    }
                    else if (isSE)
                    {
                        angle = 270f;
                        cornerPos = new Vector3((x + 1.0f) * tileSize, 0f, z * tileSize); // 右下角顶点
                    }

                    structureNodes.Add(new RoomNode
                    {
                        position = cornerPos,
                        rotation = Quaternion.Euler(0f, angle, 0f),
                        tileType = TileType.WallCorner,
                        gridPos = coord
                    });
                }
                else
                {
                    // 铺设常规平面墙壁或门通道，将其精准放置在对应边界线的中点
                    if (southEmpty)
                    {
                        structureNodes.Add(new RoomNode
                        {
                            position = new Vector3((x + 0.5f) * tileSize, 0f, z * tileSize), // 南边界中点
                            rotation = Quaternion.Euler(0f, 0f, 0f),
                            tileType = isDoorCell ? TileType.Door : TileType.Wall,
                            directionLabel = "South",
                            gridPos = coord
                        });
                    }
                    if (westEmpty)
                    {
                        structureNodes.Add(new RoomNode
                        {
                            position = new Vector3(x * tileSize, 0f, (z + 0.5f) * tileSize), // 西边界中点
                            rotation = Quaternion.Euler(0f, 90f, 0f),
                            tileType = isDoorCell ? TileType.Door : TileType.Wall,
                            directionLabel = "West",
                            gridPos = coord
                        });
                    }
                    if (northEmpty)
                    {
                        structureNodes.Add(new RoomNode
                        {
                            position = new Vector3((x + 0.5f) * tileSize, 0f, (z + 1.0f) * tileSize), // 北边界中点
                            rotation = Quaternion.Euler(0f, 180f, 0f),
                            tileType = isDoorCell ? TileType.Door : TileType.Wall,
                            directionLabel = "North",
                            gridPos = coord
                        });
                    }
                    if (eastEmpty)
                    {
                        structureNodes.Add(new RoomNode
                        {
                            position = new Vector3((x + 1.0f) * tileSize, 0f, (z + 0.5f) * tileSize), // 东边界中点
                            rotation = Quaternion.Euler(0f, 270f, 0f),
                            tileType = isDoorCell ? TileType.Door : TileType.Wall,
                            directionLabel = "East",
                            gridPos = coord
                        });
                    }
                }
            }

            // 2. 开始物理生成
            foreach (var node in structureNodes)
            {
                if (!node.isAvailable) continue;

                int floorLayer = layerConfig != null ? GetLayerFromMask(layerConfig.floorLayer) : -1;
                int wallLayer = layerConfig != null ? GetLayerFromMask(layerConfig.wallLayer) : -1;

                if (node.tileType == TileType.Floor)
                {
                    Floor selectedFloor = GetWeightedItem(preset.floorTiles);
                    if (selectedFloor != null && selectedFloor.prefab != null)
                    {
                        // 创建空的逻辑父级轴点，仅负责逻辑网格的坐标与旋转
                        GameObject pivotObj = new GameObject($"Floor_{node.gridPos.x}_{node.gridPos.y}_Pivot");
                        pivotObj.transform.parent = _floorTileParent.transform;
                        
                        int rotIdx = Random.Range(0, selectedFloor.randomRotation + 1);
                        Quaternion localPivotRot = node.rotation * Quaternion.Euler(0f, rotIdx * 90f, 0f);
                        
                        pivotObj.transform.position = transform.TransformPoint(node.position);
                        pivotObj.transform.rotation = transform.rotation * localPivotRot;
                        
                        GameObject obj = CreateObject(selectedFloor.prefab, pivotObj.transform.position, pivotObj.transform.rotation, floorLayer, pivotObj.transform);
                        if (obj != null)
                        {
                            // 位置偏移与自转纠偏全部在子物体上生效，绕着父物体（格子中心）自转
                            obj.transform.localPosition = selectedFloor.positionOffset;
                            obj.transform.localRotation = Quaternion.Euler(selectedFloor.rotationOffset);

                            if (selectedFloor.allowDecor)
                            {
                                _pendingFloorRoofDecors.Add(new PendingDecorPointData { tileObj = obj, tileConfig = selectedFloor, pointType = PointType.Floor });
                            }

                            // 实例化并局部偏移后，最后在父锚点上应用缩放覆盖
                            pivotObj.transform.localScale = selectedFloor.scaleOverride;
                        }
                    }
                }
                else if (node.tileType == TileType.Roof)
                {
                    Roof selectedRoof = GetWeightedItem(preset.roofTiles);
                    if (selectedRoof != null && selectedRoof.prefab != null)
                    {
                        GameObject pivotObj = new GameObject($"Roof_{node.gridPos.x}_{node.gridPos.y}_Pivot");
                        pivotObj.transform.parent = _roofTileParent.transform;
                        
                        int rotIdx = Random.Range(0, selectedRoof.randomRotation + 1);
                        Quaternion localPivotRot = node.rotation * Quaternion.Euler(0f, rotIdx * 90f, 0f);
                        
                        pivotObj.transform.position = transform.TransformPoint(node.position);
                        pivotObj.transform.rotation = transform.rotation * localPivotRot;
                        
                        GameObject obj = CreateObject(selectedRoof.prefab, pivotObj.transform.position, pivotObj.transform.rotation, floorLayer, pivotObj.transform);
                        if (obj != null)
                        {
                            obj.transform.localPosition = selectedRoof.positionOffset;
                            obj.transform.localRotation = Quaternion.Euler(selectedRoof.rotationOffset);

                            if (selectedRoof.allowDecor)
                            {
                                _pendingFloorRoofDecors.Add(new PendingDecorPointData { tileObj = obj, tileConfig = selectedRoof, pointType = PointType.Roof });
                            }

                            // 实例化并局部偏移后，最后在父锚点上应用缩放覆盖
                            pivotObj.transform.localScale = selectedRoof.scaleOverride;
                        }
                    }
                }
                else if (node.tileType == TileType.Wall)
                {
                    Wall selectedWall = GetWeightedItem(preset.wallTiles);
                    if (selectedWall != null && selectedWall.prefab != null)
                    {
                        GameObject pivotObj = new GameObject($"Wall_{node.gridPos.x}_{node.gridPos.y}_Pivot");
                        pivotObj.transform.parent = _wallTileParent.transform;
                        
                        pivotObj.transform.position = transform.TransformPoint(node.position);
                        pivotObj.transform.rotation = transform.rotation * node.rotation;
                        
                        GameObject obj = CreateObject(selectedWall.prefab, pivotObj.transform.position, pivotObj.transform.rotation, wallLayer, pivotObj.transform);
                        if (obj != null)
                        {
                            obj.transform.localPosition = selectedWall.positionOffset;
                            obj.transform.localRotation = Quaternion.Euler(selectedWall.rotationOffset);

                            _spawnedWallObjects.Add(obj);

                            if (selectedWall.allowDecor)
                            {
                                _pendingWallDecors.Add(new PendingDecorPointData { tileObj = obj, tileConfig = selectedWall });
                            }

                            // 实例化并局部偏移后，最后在父锚点上应用缩放覆盖
                            pivotObj.transform.localScale = selectedWall.scaleOverride;
                        }
                    }
                }
                else if (node.tileType == TileType.WallCorner)
                {
                    Wall selectedCorner = GetWeightedItem(preset.wallCorners);
                    if (selectedCorner != null && selectedCorner.prefab != null)
                    {
                        GameObject pivotObj = new GameObject($"WallCorner_{node.gridPos.x}_{node.gridPos.y}_Pivot");
                        pivotObj.transform.parent = _wallTileParent.transform;
                        
                        pivotObj.transform.position = transform.TransformPoint(node.position);
                        pivotObj.transform.rotation = transform.rotation * node.rotation;
                        
                        GameObject obj = CreateObject(selectedCorner.prefab, pivotObj.transform.position, pivotObj.transform.rotation, wallLayer, pivotObj.transform);
                        if (obj != null)
                        {
                            obj.transform.localPosition = selectedCorner.positionOffset;
                            obj.transform.localRotation = Quaternion.Euler(selectedCorner.rotationOffset);

                            _spawnedWallObjects.Add(obj);

                            // 实例化并局部偏移后，最后在父锚点上应用缩放覆盖
                            pivotObj.transform.localScale = selectedCorner.scaleOverride;
                        }
                    }
                }
                else if (node.tileType == TileType.Door)
                {
                    Door selectedDoor = null;
                    if (preset.doorTiles != null && preset.doorTiles.Count > 0)
                    {
                        selectedDoor = GetWeightedItem(preset.doorTiles);
                    }

                    if (selectedDoor != null && selectedDoor.prefab != null)
                    {
                        GameObject pivotObj = new GameObject($"Door_{node.gridPos.x}_{node.gridPos.y}_Pivot");
                        pivotObj.transform.parent = _doorParent.transform;
                        
                        pivotObj.transform.position = transform.TransformPoint(node.position);
                        pivotObj.transform.rotation = transform.rotation * node.rotation;
                        
                        GameObject obj = CreateObject(selectedDoor.prefab, pivotObj.transform.position, pivotObj.transform.rotation, wallLayer, pivotObj.transform);
                        if (obj != null)
                        {
                            obj.transform.localPosition = selectedDoor.positionOffset;
                            obj.transform.localRotation = Quaternion.Euler(selectedDoor.rotationOffset);

                            // 实例化并局部偏移后，最后在父锚点上应用缩放覆盖
                            pivotObj.transform.localScale = selectedDoor.scaleOverride;
                        }
                    }
                    else if (fallbackToWallIfNoDoor)
                    {
                        // 降级生成常规墙面
                        Wall selectedWall = GetWeightedItem(preset.wallTiles);
                        if (selectedWall != null && selectedWall.prefab != null)
                        {
                            GameObject pivotObj = new GameObject($"Wall_Fallback_{node.gridPos.x}_{node.gridPos.y}_Pivot");
                            pivotObj.transform.parent = _wallTileParent.transform;
                            
                            pivotObj.transform.position = transform.TransformPoint(node.position);
                            pivotObj.transform.rotation = transform.rotation * node.rotation;
                            
                            GameObject obj = CreateObject(selectedWall.prefab, pivotObj.transform.position, pivotObj.transform.rotation, wallLayer, pivotObj.transform);
                            if (obj != null)
                            {
                                obj.transform.localPosition = selectedWall.positionOffset;
                                obj.transform.localRotation = Quaternion.Euler(selectedWall.rotationOffset);

                                _spawnedWallObjects.Add(obj);

                                // 实例化并局部偏移后，最后在父锚点上应用缩放覆盖
                                pivotObj.transform.localScale = selectedWall.scaleOverride;
                            }
                        }
                    }
                    // else: 直接保留为空白通道（不生成任何门或墙）
                }
            }
        }

        /// <summary>
        /// 结合场景中的 DoorPin，检测并把与其正向包围盒接触的激活网格格子识别为门格子。
        /// </summary>
        protected virtual void IdentifyDoorPinsGrid()
        {
            List<DoorPin> pins = new List<DoorPin>();
            
            // 1. 获取场景中所有活动状态的 DoorPin
            DoorPin[] activePins = FindObjectsByType<DoorPin>(FindObjectsSortMode.None);
            if (activePins != null)
            {
                pins.AddRange(activePins);
            }

            // 2. 如果在编辑器且处于非播放模式下，尝试从预制件树中递归查找所有的 DoorPin（支持 Prefab Mode 隔离运行）
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // 查找自身及子节点的 DoorPin
                foreach (var pin in GetComponentsInChildren<DoorPin>(true))
                {
                    if (pin != null && !pins.Contains(pin))
                    {
                        pins.Add(pin);
                    }
                }

                // 查找同属一个预制件树根节点下的所有组件 (兄弟或父辈节点)
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

            foreach (var pin in pins)
            {
                if (pin == null) continue;
                if (pin.generatorId != generatorId) continue;

                for (int x = 0; x < canvasWidth; x++)
                {
                    for (int z = 0; z < canvasHeight; z++)
                    {
                        Vector2Int coord = new Vector2Int(x, z);

                        // 仅当该格子原本是画板绘制的激活格子时，才判定其是否需要变成门
                        if (_activeGridCoords.Contains(coord))
                        {
                            if (IsGridOverlappingWithDoorPin(x, z, pin))
                            {
                                _doorGridCoords.Add(coord);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断指定的网格格子是否与范围门销 (DoorPin) 的包围盒发生重合 (相交)，且水平占用超过瓦片边长的 20%。
        /// </summary>
        /// <param name="x">网格 X 坐标</param>
        /// <param name="z">网格 Z 坐标</param>
        /// <param name="pin">范围门销组件</param>
        /// <returns>若符合判定且重叠占用达标则返回 true，否则返回 false</returns>
        protected virtual bool IsGridOverlappingWithDoorPin(int x, int z, DoorPin pin)
        {
            if (pin == null) return false;

            // 1. 计算格子在 RoomGenerator 局部空间下的 AABB 8 个顶点
            float xMin = x * tileSize;
            float xMax = (x + 1f) * tileSize;
            float yMin = 0f;
            float yMax = roofHeight;
            float zMin = z * tileSize;
            float zMax = (z + 1f) * tileSize;

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

            // 4. 水平占用比例判定阈值：占用超过瓦片边长的 20%
            float threshold = tileSize * 0.2f;

            // 为防 DoorPin 自身某轴尺寸特别小（比如极细门框），我们与 DoorPin 本身尺寸取 Min，避免因阈值比 DoorPin 大而恒定判定为未重叠
            float requiredOverlapX = Mathf.Min(pin.boundsSize.x * 0.99f, threshold);
            float requiredOverlapZ = Mathf.Min(pin.boundsSize.z * 0.99f, threshold);

            return (overlapX >= requiredOverlapX && overlapZ >= requiredOverlapZ);
        }

        /// <summary>
        /// 在所有结构件生成完毕后，统一收集并计算所有的装饰点，以确保避墙检测能够获取到完整的墙体列表。
        /// </summary>
        protected virtual void CollectAllDecoratorPoints()
        {
            foreach (var pending in _pendingFloorRoofDecors)
            {
                if (pending.tileObj != null)
                {
                    GenerateFloorRoofPoints(pending.tileObj, pending.tileConfig, pending.pointType);
                }
            }

            foreach (var pending in _pendingWallDecors)
            {
                if (pending.tileObj != null)
                {
                    GenerateWallPoints(pending.tileObj, pending.tileConfig);
                }
            }

            _pendingFloorRoofDecors.Clear();
            _pendingWallDecors.Clear();
        }

        /// <summary>
        /// 房间道具装点及怪点、宝箱加权随机摆放核心处理。
        /// </summary>
        protected virtual void SpawnDecorations()
        {
            if (preset == null) return;

            // 分组实例化各类道具
            SpawnDecorationGroup(preset.floorDecorations, PointType.Floor, _floorDecorParent.transform);
            SpawnDecorationGroup(preset.wallDecorations, PointType.Wall, _wallDecorParent.transform);
            SpawnDecorationGroup(preset.roofDecorations, PointType.Roof, _roofDecorParent.transform);
            SpawnDecorationGroup(preset.characters, PointType.Floor, _characterParent.transform);
        }

        /// <summary>
        /// 对特定的装饰列表在相应的点类型表面进行物理投放摆放。
        /// </summary>
        /// <param name="decorations">装饰属性配置表</param>
        /// <param name="pointType">支持的投放点类型</param>
        /// <param name="parent">挂接的子父节点</param>
        protected virtual void SpawnDecorationGroup(List<Decoration> decorations, PointType pointType, Transform parent)
        {
            if (decorations == null || decorations.Count == 0) return;

            foreach (var decor in decorations)
            {
                if (decor.prefab == null) continue;

                // 1. 确定生成数量范围随机值
                int minAmt = Mathf.Min(decor.amountRange.x, decor.amountRange.y);
                int maxAmt = Mathf.Max(decor.amountRange.x, decor.amountRange.y);
                int count = Random.Range(minAmt, maxAmt + 1);

                // 2. 筛选在该垂直高度和点类型的可用点
                List<DecoratorPoint> validPoints = new List<DecoratorPoint>();
                foreach (var pt in _decoratorPoints)
                {
                    if (pt.occupied || pt.pointType != pointType) continue;

                    // 高度偏移限制相对于当前物体 Y 轴高度
                    float relativeY = pt.position.y - transform.position.y;
                    if (relativeY >= decor.verticalRange.x && relativeY <= decor.verticalRange.y)
                    {
                        validPoints.Add(pt);
                    }
                }

                // 3. 摆放资产
                for (int i = 0; i < count; i++)
                {
                    if (validPoints.Count == 0) break;

                    int rndIdx = Random.Range(0, validPoints.Count);
                    DecoratorPoint selectedPoint = validPoints[rndIdx];

                    validPoints.RemoveAt(rndIdx);
                    selectedPoint.occupied = true;

                    int decorLayer = layerConfig != null ? GetLayerFromMask(layerConfig.decorLayer) : -1;
                    
                    // 创建专属的装饰品父锚点
                    string pivotName = $"Decor_{decor.prefab.name}_{i}_Pivot";
                    GameObject pivotObj = new GameObject(pivotName);
                    pivotObj.transform.parent = parent;
                    pivotObj.transform.position = selectedPoint.position;
                    pivotObj.transform.rotation = selectedPoint.rotation;
                    
                    // 应用随机旋转（缩放留到最后）
                    float randRot = Random.Range(0f, decor.randomRotation);
                    if (pointType == PointType.Wall)
                    {
                        pivotObj.transform.Rotate(pivotObj.transform.forward * randRot, Space.World);
                    }
                    else
                    {
                        pivotObj.transform.Rotate(Vector3.up * randRot, Space.World);
                    }

                    // 实例化预制件挂载在父锚点下 (此时 pivotObj 缩放为 1,1,1，防 Unity 挂载抵消)
                    GameObject obj = CreateObject(decor.prefab, pivotObj.transform.position, pivotObj.transform.rotation, decorLayer, pivotObj.transform);
                    if (obj != null)
                    {
                        // 相对位置与旋转偏移在本地坐标系下生效
                        obj.transform.localPosition = decor.positionOffset;
                        obj.transform.localRotation = Quaternion.Euler(decor.rotationOffset);

                        // 最后在父锚点上应用基础缩放覆盖与随机缩放的累乘值
                        float randScale = Random.Range(decor.scaleRange.x, decor.scaleRange.y);
                        Vector3 baseScale = decor.scaleOverride;
                        pivotObj.transform.localScale = baseScale * randScale;
                    }

                    // 4. 间距剔除：排除距离过近的待投放点
                    for (int j = validPoints.Count - 1; j >= 0; j--)
                    {
                        if (Vector3.Distance(validPoints[j].position, selectedPoint.position) <= decor.spacing)
                        {
                            validPoints.RemoveAt(j);
                        }
                    }

                    // 5. 安全区占位标记：任何类型的其他饰品都不允许落在 safeArea 半径内
                    foreach (var pt in _decoratorPoints)
                    {
                        if (pt.pointType == pointType && !pt.occupied)
                        {
                            if (Vector3.Distance(pt.position, selectedPoint.position) <= decor.safeArea)
                            {
                                pt.occupied = true;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在地板或天花板物理面上切分格子并生成初始装饰点。
        /// </summary>
        /// <param name="tileObj">物理网格对象</param>
        /// <param name="tileConfig">瓦片属性配置</param>
        /// <param name="pointType">地板或屋顶类型</param>
        protected virtual void GenerateFloorRoofPoints(GameObject tileObj, Tile tileConfig, PointType pointType)
        {
            int pointsMax = Mathf.Max(1, (int)tileSize + 1);
            int pSpacing = Mathf.Max(1, pointSpacing);
            int numPoints = (pointsMax * pSpacing) - (pSpacing - 1);

            // 获取逻辑锚点父级，如果父级不存在则降级使用物体本身
            Transform pivotTrans = tileObj.transform.parent != null ? tileObj.transform.parent : tileObj.transform;

            for (int x = 0; x < numPoints; x++)
            {
                for (int z = 0; z < numPoints; z++)
                {
                    // 使用半步长偏移进行内缩划分，避免装饰点落在格子边界上
                    float tX = (float)(x + 0.5f) / numPoints;
                    float tZ = (float)(z + 0.5f) / numPoints;

                    float localX = Mathf.Lerp(-tileSize / 2f, tileSize / 2f, tX);
                    float localZ = Mathf.Lerp(-tileSize / 2f, tileSize / 2f, tZ);
                    Vector3 localPos = new Vector3(localX, floorDecorOffset, localZ);

                    // 使用逻辑锚点的世界坐标转换以保证装饰点对齐格子逻辑中心
                    Vector3 worldPos = pivotTrans.TransformPoint(localPos);

                    if (tileConfig.alignToSurface)
                    {
                        Vector3 rayOrigin = (pointType == PointType.Roof) ? Vector3.down * 3f : Vector3.up * 1f;
                        // 射线检测起点同样使用逻辑锚点进行位置转换
                        Vector3 rayStart = pivotTrans.TransformPoint(new Vector3(localX, 0f, localZ)) + rayOrigin;
                        Ray ray = (pointType == PointType.Roof) ? new Ray(rayStart, Vector3.up) : new Ray(rayStart, Vector3.down);

                        if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileConfig.tileLayer))
                        {
                            worldPos = hit.point;
                        }
                    }

                    // 安全距离过滤
                    if (pointType == PointType.Floor && !CheckSafeArea(worldPos))
                    {
                        continue;
                    }

                    // 记录装饰点时，其朝向对齐逻辑锚点旋转以确保一致性
                    _decoratorPoints.Add(new DecoratorPoint(worldPos, pivotTrans.rotation, pointType));
                }
            }
        }

        /// <summary>
        /// 在墙面上切分格子并生成装饰点。
        /// </summary>
        /// <param name="tileObj">墙体瓦片对象</param>
        /// <param name="tileConfig">瓦片属性配置</param>
        protected virtual void GenerateWallPoints(GameObject tileObj, Tile tileConfig)
        {
            int pointsMax = Mathf.Max(1, (int)tileSize + 1);
            int pSpacing = Mathf.Max(1, pointSpacing);
            int numPoints = (pointsMax * pSpacing) - (pSpacing - 1);

            // 获取逻辑锚点父级，如果父级不存在则降级使用物体本身
            Transform pivotTrans = tileObj.transform.parent != null ? tileObj.transform.parent : tileObj.transform;

            for (int x = 0; x < numPoints - 1; x++)
            {
                for (int y = 0; y < numPoints; y++)
                {
                    // 同样使用半步长偏移进行内缩划分，防止在墙体物理最边缘生成
                    float tX = (float)(x + 0.5f) / numPoints;
                    float tY = (float)(y + 0.5f) / numPoints;

                    float localX = Mathf.Lerp(-tileSize / 2f, tileSize / 2f, tX);
                    float localY = tY * tileSize;

                    Vector3 localPos = new Vector3(localX, localY, 0f);
                    // 坐标转换和法向位移均使用逻辑锚点以确保在墙体边界平面的居中性
                    Vector3 worldPos = pivotTrans.TransformPoint(localPos) + pivotTrans.forward * wallDecorOffset;

                    // 记录墙面装饰点旋转时对齐逻辑锚点
                    _decoratorPoints.Add(new DecoratorPoint(worldPos, pivotTrans.rotation, PointType.Wall));
                }
            }
        }

        /// <summary>
        /// 检查投放点是否在墙体安全区域外（防止穿插）。
        /// </summary>
        /// <param name="pos">投放点世界坐标</param>
        /// <returns>True 表示安全可以生成</returns>
        protected virtual bool CheckSafeArea(Vector3 pos)
        {
            if (decorSafeArea <= 0) return true;

            float minDistance = decorSafeArea * (tileSize * 0.5f);
            foreach (var wall in _spawnedWallObjects)
            {
                if (wall == null) continue;
                
                // 优先使用逻辑锚点父物体进行本地坐标逆转换，以不受美术预制件自身 Pivot 偏置的干扰
                Transform pivotTrans = wall.transform.parent != null ? wall.transform.parent : wall.transform;
                Vector3 localPos = pivotTrans.InverseTransformPoint(pos);
                
                float halfWidth = tileSize * 0.5f;
                
                // 计算点在 XZ 水平投影面上到墙体有限线段的最近点 X 轴坐标
                float closestX = Mathf.Clamp(localPos.x, -halfWidth, halfWidth);
                
                // 计算点到最近点 (closestX, 0) 的水平投影距离差异向量
                float dx = localPos.x - closestX;
                float dz = localPos.z;
                float distanceXZ = Mathf.Sqrt(dx * dx + dz * dz);
                
                // 如果最短距离小于最小安全避让距离，则判定为不安全并剔除
                if (distanceXZ < minDistance)
                {
                    return false; 
                }
            }

            return true;
        }

        /// <summary>
        /// 实例化预制件工厂方法，支持在编辑和播放模式下安全处理。
        /// </summary>
        protected virtual GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, int layer, Transform parent = null)
        {
            GameObject obj = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                obj = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            }
            else
            {
                obj = Instantiate(prefab, position, rotation);
            }
#else
            obj = Instantiate(prefab, position, rotation);
#endif
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.transform.parent = (parent != null) ? parent : _roomContainer.transform;

                if (layer >= 0)
                {
                    SetLayerRecursively(obj, layer);
                }
                _spawnedObjects.Add(obj);
            }

            return obj;
        }

        /// <summary>
        /// 递归设置物体的物理碰撞图层。
        /// </summary>
        protected virtual void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 从 LayerMask 取得首个被设为 True 的 Layer 索引。
        /// </summary>
        protected virtual int GetLayerFromMask(LayerMask mask)
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
        /// 执行加权项随机选择算法。
        /// </summary>
        protected virtual T GetWeightedItem<T>(List<T> items) where T : Tile
        {
            if (items == null || items.Count == 0) return null;

            int totalWeight = 0;
            foreach (var item in items)
            {
                if (item != null && item.prefab != null)
                {
                    totalWeight += Mathf.Max(0, item.weight);
                }
            }

            if (totalWeight <= 0) return null;

            int randomValue = Random.Range(0, totalWeight);
            foreach (var item in items)
            {
                if (item == null || item.prefab == null) continue;
                int itemWeight = Mathf.Max(0, item.weight);
                if (randomValue < itemWeight)
                {
                    return item;
                }
                randomValue -= itemWeight;
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// 用于记录拟生成网格节点信息的数据缓存类。
    /// </summary>
    public class RoomNode
    {
        public Vector3 position;
        public Quaternion rotation;
        public TileType tileType;
        public bool isAvailable = true;
        public string directionLabel;
        public Vector2Int gridPos;
    }

    /// <summary>
    /// 可用装饰点属性数据类。
    /// </summary>
    public class DecoratorPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public PointType pointType;
        public bool occupied;

        public DecoratorPoint(Vector3 pos, Quaternion rot, PointType type)
        {
            position = pos;
            rotation = rot;
            pointType = type;
            occupied = false;
        }
    }
}
