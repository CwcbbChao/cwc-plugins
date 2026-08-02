# 独立可扩展房间生成插件设计方案 (命名空间: Cwcbb.Tools.RoomBuilder)

本设计方案旨在作为您未来**完全自主开发独立房间生成插件**的参考蓝图。该自研插件在**编译期与运行期完全不依赖原版 RoomGen 插件 of 任何代码与程序集**。

通过使用独立的命名空间 `Cwcbb.Tools.RoomBuilder`，我们可以在**保留原版类名**以方便参考的同时，确保在同一个 Unity 工程中与原版 RoomGen **零命名冲突、零依赖**共存。

---

## 一、 架构与定位 (Architecture & Namespace Strategy)

* **独立命名空间**：自研插件的所有代码均包裹在 `Cwcbb.Tools.RoomBuilder` 命名空间内。
* **保留原版类名**：为了阅读与重构方便，核心类名（如 `RoomGenerator`、`RoomPreset`、`Decoration`、`Tools` 等）与原版完全一致。由于命名空间完全隔离，它们与 `RoomGen` 命名空间下的同名类不会产生任何冲突。
* **双轨参考模式**：项目工程中可暂时保留原 RoomGen 代码供算法和逻辑查阅，但自研代码完全独立自包容。

---

## 二、 核心数据模型重构 (Custom Data Models)

为了摆脱对 RoomGen 数据结构的依赖，我们需要在自研命名空间下自主定义一套更轻量、更直观的数据模型，全部采用 ScriptableObject 和可序列化类：

### 1. 基础结构定义 (`Tile.cs` / `Wall.cs` / `Floor.cs` 等)
```csharp
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    [System.Serializable]
    public class Tile
    {
        public GameObject prefab;
        [Tooltip("加权生成概率")]
        public int weight = 100;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        [Tooltip("是否允许在其表面生成装饰品")]
        public bool allowDecor = true;
    }

    [System.Serializable]
    public class Floor : Tile { }

    [System.Serializable]
    public class Wall : Tile { }

    [System.Serializable]
    public class Roof : Tile { }
}
```

### 2. 装饰品配置数据模型 (`Decoration.cs`)
```csharp
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    [System.Serializable]
    public class Decoration
    {
        public GameObject prefab;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;
        [Tooltip("自身占用半径，防止同类道具重叠")]
        public float spacing = 1.0f;
        [Tooltip("安全半径，防止在此范围内生成其他道具")]
        public float safeArea = 1.0f;
        [Tooltip("随机缩放区间")]
        public Vector2 scaleRange = new Vector2(0.9f, 1.1f);
        [Tooltip("随机旋转的最大偏角")]
        public float randomRotation = 360f;
        [Tooltip("数量范围")]
        public Vector2Int amountRange = new Vector2Int(1, 3);
        [Tooltip("高度限制（相对于地板）")]
        public Vector2 verticalRange = new Vector2(0f, 5f);
    }
}
```

### 3. 主预设资源定义 (`RoomPreset.cs` - ScriptableObject)
集中配置所有美术资产。
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    [CreateAssetMenu(fileName = "NewRoomPreset", menuName = "Cwcbb/Room Preset")]
    public class RoomPreset : ScriptableObject
    {
        public List<Floor> floorTiles;
        public List<Wall> wallTiles;
        public List<Wall> wallCorners;
        public List<Roof> roofTiles;
        
        public List<Decoration> floorDecorations;
        public List<Decoration> wallDecorations;
        public List<Decoration> roofDecorations;
        public List<Decoration> characters; // 怪点、NPC点、交互宝箱
    }
}
```

---

## 三、 自研核心模块实现 (Core Functionality)

### 1. 可视化像素画板与网格系统
通过自研的网格管理器实现异形房间，并高度精简交互代码（将点击视为 1x1 的矩形选区）：
* **网格坐标映射**：使用 `HashSet<Vector2Int>` 记录被画板激活的格子。
* **画板网格与物理空间的尺寸映射**：
  在 Unity 2D 编辑器面板上，每一个格子都绘制为 25x25 像素的正方形。但在 **3D 场景世界空间中，网格中一个格子的实际物理边长为 `tileSize`**。
  转换公式为：
  `Vector3 worldPosition = new Vector3(x * tileSize, 0, z * tileSize);`
  这能确保画板上相邻的两个格子，在 3D 场景生成时以 `tileSize` 为间距紧密拼接，实现完美平铺。
* **边界墙体自适应算法 (Neighbor Checks)**：
  遍历 `HashSet` 中的每一个坐标 `(x, z)`：
  1. 生成对应的地板。
  2. 检查 4 个邻接方向：`(x+1, z)`、`(x-1, z)`、`(x, z+1)`、`(x, z-1)`。
  3. 若某方向的坐标**不在 `HashSet` 集合中**，则在当前格子的该边缘生成一面**朝向该外侧邻居**的墙体，实现自动包边。

### 2. 全局统一图层设置 (`RoomLayerConfig.cs` - ScriptableObject)
独立管理关卡图层，消除生成器上的重复配置：
```csharp
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    [CreateAssetMenu(fileName = "RoomLayerConfig", menuName = "Cwcbb/Room Layer Config")]
    public class RoomLayerConfig : ScriptableObject
    {
        public LayerMask floorLayer;
        public LayerMask wallLayer;
        public LayerMask decorLayer;
    }
}
```

### 3. 范围门销系统 (Wide DoorPin System)
为了支持超过 1 格宽度的门通道（例如双扇大门、拱门或开放式通道），自研的 `DoorPin` 应当跳脱出“一次只能替换一个格子”的局限，设计为**带尺寸的范围检测盒（Bounds Check Box）**。

#### (1) 数据定义与 Gizmos 可视化绘制
在自研的 `DoorPin.cs` 中增加三维尺寸变量，并通过 Gizmos 在场景中画出蓝色半透明框，以便在编辑器中直观配置门通道的大小：
```csharp
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    public class DoorPin : MonoBehaviour
    {
        public RoomGenerator roomGenerator;
        [Tooltip("门销的影响范围（长宽高）")]
        public Vector3 boundsSize = new Vector3(3f, 3f, 1f);

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f); // 蓝色半透明
            Gizmos.DrawCube(transform.position, boundsSize);
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, boundsSize);
        }
    }
}
```

#### (2) 区域批量拆墙与通道开辟算法
在 `RoomGenerator` 的 `SpawnDoors` 逻辑中，采用**Bounds 包围盒碰撞检测**：
* 每次生成前，为 `DoorPin` 构建一个物理包围盒：
  `Bounds pinBounds = new Bounds(doorPin.transform.position, doorPin.boundsSize);`
* 遍历所有的墙壁网格节点（`nodes`），只要节点的坐标落在 `pinBounds` 内部，就自动将其设为“不可用”：
  ```csharp
  if (pinBounds.Contains(node.position))
  {
      node.isAvailable = false; // 强行在此处不开设墙体，留出空通道
      // 可选：在此处实例化一个门预制件，或者单纯留空作为空气通道
  }
  ```

### 4. 生成器自我层级托管与生命周期 (Self-Contained Container)
* 所有的生成内容全部放置在生成器本地持有的子 GameObject 容器中。
* 无论是编辑器模式、播放模式，还是在**预制件编辑模式（Prefab Mode）**下，因为挂载为直接子级，Unity 会原生无缝地支持资产保存与生命周期清理，再无跨场景或无限克隆的隐患。

---

## 四、 架构设计（高扩展性流水线基类）

定义核心生成基类 `RoomGenerator`，通过虚工厂方法让子类可以完全控制游戏物体的产生（如支持运行时对象池或特殊的层级挂载）：

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.RoomBuilder
{
    public class RoomGenerator : MonoBehaviour
    {
        [Header("数据与图层配置")]
        public RoomPreset preset;
        public RoomLayerConfig layerConfig;
        
        [Header("画板网格尺寸")]
        public int canvasWidth = 10;
        public int canvasHeight = 10;
        public float tileSize = 3.0f;

        [HideInInspector]
        public bool[] gridData; // 可视化画板直接操作的数据源

        // 内部受保护的临时生成缓存
        protected HashSet<Vector2Int> activeGridCoords = new HashSet<Vector2Int>();
        protected List<GameObject> spawnedObjects = new List<GameObject>();
        protected GameObject roomContainer;

        // --- 主生命周期流程 ---
        public virtual void Generate()
        {
            Clear();
            InitContainer();
            ParseGridData();         // 将画笔一维数组转为坐标 HashSet
            SpawnFloorsAndWalls();   // 计算边界并放置结构
            SpawnDecorations();      // 摆放道具与怪点
        }

        protected virtual void InitContainer()
        {
            roomContainer = new GameObject("Room_Content_Container");
            roomContainer.transform.parent = this.transform;
            roomContainer.transform.localPosition = Vector3.zero;
            roomContainer.transform.localRotation = Quaternion.identity;
        }

        protected virtual void ParseGridData()
        {
            activeGridCoords.Clear();
            for (int x = 0; x < canvasWidth; x++)
            {
                for (int z = 0; z < canvasHeight; z++)
                {
                    int index = x + z * canvasWidth;
                    if (gridData != null && index < gridData.Length && gridData[index])
                    {
                        activeGridCoords.Add(new Vector2Int(x, z));
                    }
                }
            }
        }

        protected virtual void SpawnFloorsAndWalls()
        {
            // 遍历 activeGridCoords 中的每个坐标 (x, z)，换算物理世界坐标并实例化：
            // Vector3 position = new Vector3(x * tileSize, 0, z * tileSize);
            // 并在此处调用 CreateObject 实例化墙壁和地板
        }

        protected virtual void SpawnDecorations()
        {
            // 在此实现道具加权随机摆放与安全间距过滤
        }

        // --- 核心工厂方法，允许子类拦截并重写实例化逻辑（如对象池、图层递归设置） ---
        protected virtual GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, int layer)
        {
            GameObject obj;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                obj = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            else
                obj = Instantiate(prefab, position, rotation);
#else
            obj = Instantiate(prefab, position, rotation);
#endif
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.parent = roomContainer.transform;

            SetLayerRecursively(obj, layer);
            spawnedObjects.Add(obj);
            return obj;
        }

        protected void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public virtual void Clear()
        {
            if (roomContainer != null)
            {
                DestroyImmediate(roomContainer);
            }
            spawnedObjects.Clear();
            activeGridCoords.Clear();
        }
    }
}
```

---

## 五、 二次开发与扩展路线

1. **宏观迷宫求解器对接**：
   如果您后期需要在生成大关卡时使用此插件，只需在拼装完房间骨架的后处理流程中，动态为房间挂载本插件的 `RoomGenerator` 并调用 `Generate()`，即可进行独立的室内装饰装点。
2. **异形与斜坡连接扩展**：
   通过在基类定义 `protected virtual void SpawnRampsAndStairs()`，可在网格层析检查到层高差时，自动实例化连通斜坡，完美解决原版插件无法处理高度差拼接的硬伤。
