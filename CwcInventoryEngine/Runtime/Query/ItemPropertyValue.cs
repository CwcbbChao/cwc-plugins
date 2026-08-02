using System;
using System.Collections;
using UnityEngine;

namespace Cwc.InventoryEngine.Query
{
    /// <summary>
    /// 物品属性值的存储数据类型。
    /// </summary>
    public enum ItemPropertyValueType
    {
        /// <summary>
        /// 空值/无效值。
        /// </summary>
        Empty = 0,

        /// <summary>
        /// 整数。
        /// </summary>
        Int = 1,

        /// <summary>
        /// 浮点数。
        /// </summary>
        Float = 2,

        /// <summary>
        /// 字符串。
        /// </summary>
        String = 3,

        /// <summary>
        /// 布尔值。
        /// </summary>
        Bool = 4,

        /// <summary>
        /// 复杂引用类型对象 (如 List/IEnumerable 标签或自定义对象)。
        /// </summary>
        Object = 5,
    }

    /// <summary>
    /// 背包物品属性轻量只读值结构体。
    /// 用于消除筛选与排序过程中的基础数据类型装箱 (Boxing) 开销，实现零 GC 分配。
    /// </summary>
    public readonly struct ItemPropertyValue : IComparable<ItemPropertyValue>, IEquatable<ItemPropertyValue>
    {
        #region Private Fields
        private readonly ItemPropertyValueType _type;
        private readonly int _intValue;
        private readonly float _floatValue;
        private readonly string _stringValue;
        private readonly bool _boolValue;
        private readonly object _objectValue;
        #endregion

        #region Public Properties
        /// <summary>
        /// 属性值的存储数据类型。
        /// </summary>
        public ItemPropertyValueType Type => _type;

        /// <summary>
        /// 是否为空值。
        /// </summary>
        public bool IsEmpty => _type == ItemPropertyValueType.Empty;

        /// <summary>
        /// 读取整数值（若非 Int 类型，若为 Float 则强制转换，否则返回 0）。
        /// </summary>
        public int IntValue => _type == ItemPropertyValueType.Int ? _intValue : (_type == ItemPropertyValueType.Float ? (int)_floatValue : 0);

        /// <summary>
        /// 读取浮点数值（若非 Float 类型，若为 Int 则强制转换，否则返回 0f）。
        /// </summary>
        public float FloatValue => _type == ItemPropertyValueType.Float ? _floatValue : (_type == ItemPropertyValueType.Int ? _intValue : 0f);

        /// <summary>
        /// 读取字符串值。
        /// </summary>
        public string StringValue => _stringValue ?? string.Empty;

        /// <summary>
        /// 读取布尔值。
        /// </summary>
        public bool BoolValue => _boolValue;

        /// <summary>
        /// 读取复杂引用对象。
        /// </summary>
        public object ObjectValue => _objectValue;
        #endregion

        #region Static Readonly Fields
        /// <summary>
        /// 表示空的 ItemPropertyValue。
        /// </summary>
        public static readonly ItemPropertyValue Empty = new(ItemPropertyValueType.Empty);
        #endregion

        #region Constructors
        private ItemPropertyValue(ItemPropertyValueType type)
        {
            _type = type;
            _intValue = 0;
            _floatValue = 0f;
            _stringValue = null;
            _boolValue = false;
            _objectValue = null;
        }

        /// <summary>
        /// 构造一个 Int 类型的属性值。
        /// </summary>
        public ItemPropertyValue(int value)
        {
            _type = ItemPropertyValueType.Int;
            _intValue = value;
            _floatValue = 0f;
            _stringValue = null;
            _boolValue = false;
            _objectValue = null;
        }

        /// <summary>
        /// 构造一个 Float 类型的属性值。
        /// </summary>
        public ItemPropertyValue(float value)
        {
            _type = ItemPropertyValueType.Float;
            _intValue = 0;
            _floatValue = value;
            _stringValue = null;
            _boolValue = false;
            _objectValue = null;
        }

        /// <summary>
        /// 构造一个 String 类型的属性值。
        /// </summary>
        public ItemPropertyValue(string value)
        {
            _type = string.IsNullOrEmpty(value) ? ItemPropertyValueType.Empty : ItemPropertyValueType.String;
            _intValue = 0;
            _floatValue = 0f;
            _stringValue = value;
            _boolValue = false;
            _objectValue = null;
        }

        /// <summary>
        /// 构造一个 Bool 类型的属性值。
        /// </summary>
        public ItemPropertyValue(bool value)
        {
            _type = ItemPropertyValueType.Bool;
            _intValue = 0;
            _floatValue = 0f;
            _stringValue = null;
            _boolValue = false;
            _objectValue = value;
        }

        /// <summary>
        /// 构造一个 Object 或通用类型的属性值（自动识别基础数据类型或装箱）。
        /// </summary>
        public ItemPropertyValue(object value)
        {
            if (value == null)
            {
                _type = ItemPropertyValueType.Empty;
                _intValue = 0;
                _floatValue = 0f;
                _stringValue = null;
                _boolValue = false;
                _objectValue = null;
                return;
            }

            switch (value)
            {
                case int intVal:
                    _type = ItemPropertyValueType.Int;
                    _intValue = intVal;
                    _floatValue = 0f;
                    _stringValue = null;
                    _boolValue = false;
                    _objectValue = null;
                    break;

                case float floatVal:
                    _type = ItemPropertyValueType.Float;
                    _intValue = 0;
                    _floatValue = floatVal;
                    _stringValue = null;
                    _boolValue = false;
                    _objectValue = null;
                    break;

                case double doubleVal:
                    _type = ItemPropertyValueType.Float;
                    _intValue = 0;
                    _floatValue = (float)doubleVal;
                    _stringValue = null;
                    _boolValue = false;
                    _objectValue = null;
                    break;

                case string strVal:
                    _type = ItemPropertyValueType.String;
                    _intValue = 0;
                    _floatValue = 0f;
                    _stringValue = strVal;
                    _boolValue = false;
                    _objectValue = null;
                    break;

                case bool boolVal:
                    _type = ItemPropertyValueType.Bool;
                    _intValue = 0;
                    _floatValue = 0f;
                    _stringValue = null;
                    _boolValue = boolVal;
                    _objectValue = null;
                    break;

                case Enum enumVal:
                    _type = ItemPropertyValueType.String;
                    _intValue = 0;
                    _floatValue = 0f;
                    _stringValue = enumVal.ToString();
                    _boolValue = false;
                    _objectValue = null;
                    break;

                default:
                    _type = ItemPropertyValueType.Object;
                    _intValue = 0;
                    _floatValue = 0f;
                    _stringValue = null;
                    _boolValue = false;
                    _objectValue = value;
                    break;
            }
        }
        #endregion

        #region Implicit Conversion Operators
        public static implicit operator ItemPropertyValue(int value) => new(value);
        public static implicit operator ItemPropertyValue(float value) => new(value);
        public static implicit operator ItemPropertyValue(string value) => new(value);
        public static implicit operator ItemPropertyValue(bool value) => new(value);
        public static implicit operator ItemPropertyValue(UnityEngine.Object value) => new(value);
        #endregion

        #region Public Methods & Interface Implementations
        /// <summary>
        /// 零 GC 对比两个属性值的大小关系。
        /// </summary>
        public int CompareTo(ItemPropertyValue other)
        {
            if (_type == ItemPropertyValueType.Empty && other._type == ItemPropertyValueType.Empty) return 0;
            if (_type == ItemPropertyValueType.Empty) return 1;
            if (other._type == ItemPropertyValueType.Empty) return -1;

            // 数值类型跨类型对比 (Int 与 Float 兼容)
            if ((_type == ItemPropertyValueType.Int || _type == ItemPropertyValueType.Float) &&
                (other._type == ItemPropertyValueType.Int || other._type == ItemPropertyValueType.Float))
            {
                float valA = FloatValue;
                float valB = other.FloatValue;
                return valA.CompareTo(valB);
            }

            if (_type == ItemPropertyValueType.String && other._type == ItemPropertyValueType.String)
            {
                return string.Compare(_stringValue, other._stringValue, StringComparison.OrdinalIgnoreCase);
            }

            if (_type == ItemPropertyValueType.Bool && other._type == ItemPropertyValueType.Bool)
            {
                return _boolValue.CompareTo(other._boolValue);
            }

            if (_type == ItemPropertyValueType.Object && other._type == ItemPropertyValueType.Object)
            {
                if (_objectValue is IComparable compA && other._objectValue is IComparable)
                {
                    return compA.CompareTo(other._objectValue);
                }
            }

            return 0;
        }

        public bool Equals(ItemPropertyValue other)
        {
            if (_type != other._type)
            {
                // 数值交叉比较
                if ((_type == ItemPropertyValueType.Int || _type == ItemPropertyValueType.Float) &&
                    (other._type == ItemPropertyValueType.Int || other._type == ItemPropertyValueType.Float))
                {
                    return Mathf.Approximately(FloatValue, other.FloatValue);
                }
                return false;
            }

            switch (_type)
            {
                case ItemPropertyValueType.Empty:
                    return true;
                case ItemPropertyValueType.Int:
                    return _intValue == other._intValue;
                case ItemPropertyValueType.Float:
                    return Mathf.Approximately(_floatValue, other._floatValue);
                case ItemPropertyValueType.String:
                    return string.Equals(_stringValue, other._stringValue, StringComparison.OrdinalIgnoreCase);
                case ItemPropertyValueType.Bool:
                    return _boolValue == other._boolValue;
                case ItemPropertyValueType.Object:
                    return Equals(_objectValue, other._objectValue);
                default:
                    return false;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is ItemPropertyValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)_type;
                hash = (hash * 397) ^ _intValue;
                hash = (hash * 397) ^ _floatValue.GetHashCode();
                if (_stringValue != null) hash = (hash * 397) ^ _stringValue.GetHashCode();
                hash = (hash * 397) ^ _boolValue.GetHashCode();
                if (_objectValue != null) hash = (hash * 397) ^ _objectValue.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            switch (_type)
            {
                case ItemPropertyValueType.Int:
                    return _intValue.ToString();
                case ItemPropertyValueType.Float:
                    return _floatValue.ToString("F2");
                case ItemPropertyValueType.String:
                    return _stringValue;
                case ItemPropertyValueType.Bool:
                    return _boolValue.ToString();
                case ItemPropertyValueType.Object:
                    return _objectValue != null ? _objectValue.ToString() : "null";
                case ItemPropertyValueType.Empty:
                default:
                    return "Empty";
            }
        }
        #endregion
    }
}
