using System;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 背包容器批处理作用域 (Batch Scope)。
    /// 实现 IDisposable 接口，配合 using 语句使用。
    /// 能够在批量添加、整理、拾取物品时防范频繁触发 UI 更新事件引发事件风暴掉帧。
    /// </summary>
    public readonly struct BatchScope : IDisposable
    {
        #region Private Fields
        private readonly InventoryContainer _container;
        #endregion

        #region Constructors
        /// <summary>
        /// 构造批处理作用域。
        /// </summary>
        /// <param name="container">目标容器</param>
        public BatchScope(InventoryContainer container)
        {
            _container = container;
            _container?.BeginBatch();
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// 离开作用域时结束批处理，并统一刷出被修改的槽位更新事件。
        /// </summary>
        public void Dispose()
        {
            _container?.EndBatch();
        }
        #endregion
    }
}
