namespace Cwcbb.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 全局调度遥控管理器单例，作为所有遥控对象（RemoteControlObject）的对象池容器。
    /// </summary>
    [DisallowMultipleComponent]
    public class CwcRemoteControlManager : MonoBehaviour
    {
        #region 单例声明
        private static CwcRemoteControlManager _instance;
        public static CwcRemoteControlManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CwcRemoteControlManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CwcRemoteControlManager");
                        _instance = go.AddComponent<CwcRemoteControlManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 序列化字段
        [Header("渲染相机配置")]
        [Tooltip("负责渲染所有受控 3D 对象的独立相机")]
        [SerializeField] private Camera _renderCamera;
        #endregion

        #region 私有字段
        private readonly Dictionary<CwcRemoteControlObjectConfig, List<CwcRemoteControlObject>> _pool = 
            new Dictionary<CwcRemoteControlObjectConfig, List<CwcRemoteControlObject>>();
        #endregion

        #region 生命周期
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            UpdateCameraState();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 从池中复用或实例化一个遥控对象。
        /// 实例化的对象作为 Manager 的子物体生成并托管，共享其跨场景不销毁的生命周期。
        /// </summary>
        public CwcRemoteControlObject RequestObject(CwcRemoteControlObjectConfig config)
        {
            if (config == null || config.Prefab == null) return null;

            if (!_pool.TryGetValue(config, out List<CwcRemoteControlObject> list))
            {
                list = new List<CwcRemoteControlObject>();
                _pool[config] = list;
            }

            CwcRemoteControlObject availableObject = null;

            // 逆序检查，清理可能因为外部误删除而失效的 Null 引用，防止池子无限膨胀
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (!list[i].IsOccupied && availableObject == null)
                {
                    availableObject = list[i];
                }
            }

            if (availableObject == null)
            {
                // 池内无空闲，在 Manager 下创建新实例
                GameObject obj = Instantiate(config.Prefab, transform);
                availableObject = obj.GetComponent<CwcRemoteControlObject>();
                if (availableObject == null)
                {
                    availableObject = obj.AddComponent<CwcRemoteControlObject>();
                }
                availableObject.Init(config);
                list.Add(availableObject);
            }

            // 标记为已被占用
            availableObject.IsOccupied = true;

            // 重置变换准备交给 Controller 控制
            availableObject.transform.SetParent(transform);
            availableObject.transform.localPosition = Vector3.zero;
            availableObject.transform.localRotation = Quaternion.identity;
            availableObject.transform.localScale = Vector3.one;

            return availableObject;
        }

        /// <summary>
        /// 回收遥控对象，隐藏并收纳到 Manager 节点下。
        /// </summary>
        public void RecycleObject(CwcRemoteControlObject obj)
        {
            if (obj == null) return;

            obj.SetVisible(false);
            obj.IsOccupied = false;
            obj.transform.SetParent(transform);
            
            UpdateCameraState();
        }

        /// <summary>
        /// 根据池内所有对象的激活状态，全局调度并更新渲染相机的启用状态。
        /// 只要有一个受控对象是处于激活状态，则启用渲染相机；否则关闭渲染相机。
        /// </summary>
        public void UpdateCameraState()
        {
            if (_renderCamera == null) return;

            bool anyActive = false;
            foreach (var kvp in _pool)
            {
                var list = kvp.Value;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].IsVisible)
                        {
                            anyActive = true;
                            break;
                        }
                    }
                }
                if (anyActive) break;
            }

            if (_renderCamera.enabled != anyActive)
            {
                _renderCamera.enabled = anyActive;
            }
        }
        #endregion
    }
}
