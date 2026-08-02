using System;
using UnityEngine;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态路径标识结构体，包装路径字符串与预计算 Hash，支持隐式类型转换为 string 和 int。
    /// 适合在项目中定义静态状态路径常量类，兼顾单字段封装与零 GC 切换性能。
    /// </summary>
    [Serializable]
    public readonly struct StatePathInfo : IEquatable<StatePathInfo>
    {
        #region 公共属性

        /// <summary>
        /// 原始状态路径字符串
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 预计算的状态路径 Hash 值
        /// </summary>
        public int Hash { get; }

        /// <summary>
        /// 状态路径是否为空
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(Path);

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造状态路径标识
        /// </summary>
        /// <param name="path">状态路径字符串</param>
        public StatePathInfo(string path)
        {
            Path = path ?? string.Empty;
            Hash = StatePathUtility.StringToHash(Path);
        }

        #endregion

        #region 隐式类型转换运算符

        /// <summary>
        /// 隐式转换为 int（提取 Hash 值，享受零 GC 匹配性能）
        /// </summary>
        public static implicit operator int(StatePathInfo info) => info.Hash;

        /// <summary>
        /// 隐式转换为 string（提取路径字符串）
        /// </summary>
        public static implicit operator string(StatePathInfo info) => info.Path;

        /// <summary>
        /// 隐式从 string 构造 StatePathInfo
        /// </summary>
        public static implicit operator StatePathInfo(string path) => new StatePathInfo(path);

        #endregion

        #region 相等性重载与辅助方法

        public bool Equals(StatePathInfo other)
        {
            return Hash == other.Hash && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is StatePathInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash;
        }

        public static bool operator ==(StatePathInfo left, StatePathInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatePathInfo left, StatePathInfo right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Path;
        }

        #endregion
    }
}
