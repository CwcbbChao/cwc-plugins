using System.Collections.Generic;
using UnityEngine;

namespace Cwcbb.Tools.CwcUIFramework
{
    /// <summary>
    /// UI 协同打开组件。当拥有该组件的面板打开时，同步打开配置的协同面板。
    /// </summary>
    [AddComponentMenu("CwcUIFramework/Components/CwcUI Sync Open Component")]
    public class CwcUISyncOpenComponent : CwcUIComponentBase
    {
        #region 序列化属性与字段

        [Header("协同打开的 UIEntry 配置")]
        [SerializeField] private List<UIEntrySO> syncOpenEntries = new List<UIEntrySO>();

        [Header("关闭时同步关闭协同面板")]
        [SerializeField] private bool syncClose = true;

        #endregion

        #region 公共方法重写

        /// <summary>
        /// 宿主面板开始打开时，同步打开配置的协同面板。
        /// </summary>
        public override void OnOpen()
        {
            if (Owner == null || Owner.UIFrame == null)
            {
                Debug.LogError($"[CwcUIFramework] 协同打开失败：宿主面板或关联的 UIFrame 为 Null！");
                return;
            }

            if (syncOpenEntries == null) return;

            for (int i = 0; i < syncOpenEntries.Count; i++)
            {
                var entry = syncOpenEntries[i];
                if (entry != null)
                {
                    Owner.UIFrame.Open(entry);
                }
            }
        }

        /// <summary>
        /// 宿主面板开始关闭时，如果开启了 syncClose，则同步关闭协同面板。
        /// </summary>
        public override void OnClose()
        {
            if (!syncClose || Owner == null || Owner.UIFrame == null || syncOpenEntries == null) return;

            for (int i = 0; i < syncOpenEntries.Count; i++)
            {
                var entry = syncOpenEntries[i];
                if (entry != null)
                {
                    Owner.UIFrame.Close(entry);
                }
            }
        }

        #endregion
    }
}
