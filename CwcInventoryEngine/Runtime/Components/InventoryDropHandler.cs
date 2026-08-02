using System;
using UnityEngine;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 开箱即用的组合式物品掉落处理组件。
    /// 自动监听全局请求总线中的 InventoryDropRequest 请求，
    /// 自动在场景中解析玩家实体位置，完成规则校验、背包数据扣减与场景实体 Instantiate / BindInstance。
    /// 挂载在场景管理器或玩家实体上即可生效。
    /// </summary>
    [AddComponentMenu("Cwc/Inventory/Inventory Drop Handler")]
    public class InventoryDropHandler : MonoBehaviour
    {
        #region 序列化字段
        [Header("默认掉落配置")]
        [SerializeField]
        [Tooltip("全局默认掉落物预制件 (当物品自身未在 ComponentDefinitions 中单独配置 CustomDropPrefab 时使用)。需挂载 ItemPickup 组件。")]
        private GameObject _defaultDropPrefab;

        [SerializeField]
        [Tooltip("掉落离散半径 (以玩家为中心在半径范围内随机散射)。")]
        [Min(0f)]
        private float _spawnRadius = 1.0f;

        [SerializeField]
        [Tooltip("用于定位玩家位置的 GameObject Tag 标签。若为空将默认使用 'Player'。")]
        private string _playerTag = "Player";

        [Header("调试与安全控制")]
        [SerializeField]
        [Tooltip("当丢弃被拦截 (如不可丢弃物品) 时是否在 Console 输出 Warning 警告。")]
        private bool _logWarnings = true;
        #endregion

        #region 生命周期
        private void OnEnable()
        {
            InventoryRequestPipeline.Subscribe<InventoryDropRequest>(HandleDropRequest);
        }

        private void OnDisable()
        {
            InventoryRequestPipeline.Unsubscribe<InventoryDropRequest>(HandleDropRequest);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 核心掉落请求响应回调。
        /// </summary>
        public virtual void HandleDropRequest(InventoryDropRequest request)
        {
            if (request == null) return;
            if (string.IsNullOrEmpty(request.TargetInventoryId)) return;

            // 1. 查找目标背包容器
            if (!InventoryRegistry.TryGetContainer(request.TargetInventoryId, out var container))
            {
                if (_logWarnings) Debug.LogWarning($"[InventoryDropHandler] 未能找到唯一 ID 为 '{request.TargetInventoryId}' 的背包容器。");
                return;
            }

            // 2. 校验槽位与物品
            if (request.SlotIndex < 0 || request.SlotIndex >= container.Capacity) return;
            var slot = container.Slots[request.SlotIndex];
            if (slot.IsEmpty || slot.Item == null) return;

            ItemInstance originalItem = slot.Item;

            // 3. 优先检查运行时 ItemDropComponent 组件 (支持动态覆盖)，次要检索静态定义
            bool canDrop = true;
            GameObject customPrefab = null;

            if (originalItem.TryGetComponent<ItemDropComponent>(out var dropComp))
            {
                canDrop = dropComp.CanDrop;
                customPrefab = dropComp.CustomDropPrefab;
            }
            else if (originalItem.Definition.TryGetComponentDefinition<ItemDropComponentDefinition>(out var dropDef))
            {
                canDrop = dropDef.CanDrop;
                customPrefab = dropDef.CustomDropPrefab;
            }

            if (!canDrop)
            {
                if (_logWarnings) Debug.LogWarning($"[InventoryDropHandler] 物品 '{originalItem.Definition.name}' 标记为不可丢弃 (CanDrop = false)。");
                return;
            }

            // 4. 计算扣减数量与实际剥离的 ItemInstance
            int dropCount = request.Count <= 0 ? originalItem.StackCount : Math.Min(request.Count, originalItem.StackCount);
            ItemInstance dropInstance = null;

            if (dropCount >= originalItem.StackCount)
            {
                // 全量丢弃：清空槽位并提取实例
                dropInstance = originalItem;
                container.RemoveItemFromSlot(request.SlotIndex, dropCount);
            }
            else
            {
                // 部分丢弃：拆分堆叠并通知容器更新
                dropInstance = originalItem.Split(dropCount);
                container.RemoveItemFromSlot(request.SlotIndex, 0);
            }

            if (dropInstance == null) return;

            // 5. 自动解析生成点位置 (优先使用 request 显式指定的，否则自动定位玩家实体)
            Vector3 spawnPos = request.CustomSpawnPosition ?? ResolvePlayerPosition();

            // 应用离散平移散射
            if (_spawnRadius > 0f)
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _spawnRadius;
                spawnPos += new Vector3(randomCircle.x, 0f, randomCircle.y);
            }

            // 6. 确定 Spawn 使用的 Prefab
            GameObject prefabToSpawn = _defaultDropPrefab;
            if (customPrefab != null)
            {
                prefabToSpawn = customPrefab;
            }

            if (prefabToSpawn == null)
            {
                if (_logWarnings) Debug.LogError($"[InventoryDropHandler] 无法生成掉落物！物品 '{dropInstance.Definition.name}' 未配置专属 Prefab，且 Handler 未设置默认 Prefab。");
                return;
            }

            // 7. 实例化掉落物并绑定数据
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // 尝试与 CwcInventoryEngine 的 ItemPickup 绑定
            if (spawnedObject.TryGetComponent<ItemPickup>(out var pickup))
            {
                pickup.BindInstance(dropInstance);
            }
            else
            {
                if (_logWarnings) Debug.LogWarning($"[InventoryDropHandler] 生成的掉落预制件 '{spawnedObject.name}' 根节点缺少 ItemPickup 组件，物品数据未能动态 BindInstance。");
            }
        }
        #endregion

        #region 私有辅助方法
        /// <summary>
        /// 自动寻找当前场景中玩家实体的坐标
        /// </summary>
        private Vector3 ResolvePlayerPosition()
        {
            // 1. 优先通过 Tag 查找玩家 GameObject
            string tagToSearch = string.IsNullOrEmpty(_playerTag) ? "Player" : _playerTag;
            try
            {
                GameObject playerObj = GameObject.FindWithTag(tagToSearch);
                if (playerObj != null)
                {
                    return playerObj.transform.position;
                }
            }
            catch
            {
                // 忽略 Tag 未定义的异常
            }

            // 2. 备用策略：尝试获取主相机 Transform
            if (Camera.main != null)
            {
                return Camera.main.transform.position;
            }

            // 3. 极简兜底：当前 Handler 自身的位置
            return transform.position;
        }
        #endregion
    }
}
