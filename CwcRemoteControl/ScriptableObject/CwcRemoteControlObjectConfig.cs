namespace Cwcbb.Tools
{
    using UnityEngine;

    /// <summary>
    /// 遥控物体的配置源，使用 ScriptableObject 承载预制件和初始局部变换微调。
    /// </summary>
    [CreateAssetMenu(fileName = "CwcRemoteControlObjectConfig", menuName = "Cwc/RemoteControl/ObjectConfig")]
    public class CwcRemoteControlObjectConfig : ScriptableObject
    {
        #region 序列化字段
        [Header("基本配置")]
        [Tooltip("遥控对象的原始预制件")]
        [SerializeField] private GameObject _prefab;

        [Header("空间微调偏移")]
        [Tooltip("对象在 Manager 节点下的局部位置偏移")]
        [SerializeField] private Vector3 _defaultPositionOffset = Vector3.zero;

        [Tooltip("对象在 Manager 节点下的局部旋转角度")]
        [SerializeField] private Vector3 _defaultRotation = Vector3.zero;

        [Tooltip("对象在 Manager 节点下的局部缩放比例")]
        [SerializeField] private Vector3 _defaultScale = Vector3.one;
        #endregion

        #region 公共属性
        public GameObject Prefab => _prefab;
        public Vector3 DefaultPositionOffset => _defaultPositionOffset;
        public Vector3 DefaultRotation => _defaultRotation;
        public Vector3 DefaultScale => _defaultScale;
        #endregion
    }
}
