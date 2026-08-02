using System;

namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 物品实例唯一标识结构体（零 GC 值类型）。
    /// 封装底层的实际标识逻辑（当前基于 Guid），并提供相等等价比较与隐式/显式转换，
    /// 方便未来向 ulong 网络 ID 或发号器拓展。
    /// </summary>
    public readonly struct ItemId : IEquatable<ItemId>, IComparable<ItemId>
    {
        #region Private Fields
        private readonly Guid _guid;
        #endregion

        #region Public Properties
        /// <summary>
        /// 是否为无效/未初始化的 ID。
        /// </summary>
        public bool IsEmpty => _guid == Guid.Empty;

        /// <summary>
        /// 获取底层对应的 Guid。
        /// </summary>
        public Guid GuidValue => _guid;
        #endregion

        #region Constructors
        /// <summary>
        /// 使用指定的 Guid 构造 ItemId。
        /// </summary>
        public ItemId(Guid guid)
        {
            _guid = guid;
        }

        /// <summary>
        /// 生成一个新的随机 ItemId。
        /// </summary>
        public static ItemId NewId()
        {
            return new ItemId(Guid.NewGuid());
        }

        /// <summary>
        /// 获取一个空的 ItemId。
        /// </summary>
        public static ItemId Empty => default;
        #endregion

        #region String Parsing & Output
        /// <summary>
        /// 从字符串解析为 ItemId。若解析失败则返回 Empty。
        /// </summary>
        public static ItemId Parse(string value)
        {
            if (Guid.TryParse(value, out Guid guid))
            {
                return new ItemId(guid);
            }
            return Empty;
        }

        public override string ToString()
        {
            return _guid.ToString();
        }
        #endregion

        #region Equality & Comparison
        public bool Equals(ItemId other)
        {
            return _guid.Equals(other._guid);
        }

        public override bool Equals(object obj)
        {
            return obj is ItemId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _guid.GetHashCode();
        }

        public int CompareTo(ItemId other)
        {
            return _guid.CompareTo(other._guid);
        }

        public static bool operator ==(ItemId left, ItemId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ItemId left, ItemId right)
        {
            return !left.Equals(right);
        }

        public static implicit operator ItemId(Guid guid) => new ItemId(guid);
        public static implicit operator Guid(ItemId itemId) => itemId._guid;
        #endregion
    }
}
