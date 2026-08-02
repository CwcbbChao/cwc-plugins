// FileName: AddressableKey.cs
using System;
using UnityEngine;

namespace CwcAddressable
{
    /// <summary>
    /// 表示持久化的 Addressable 逻辑寻址 Key 值结构。
    /// 字段无需添加任何 Attribute 特性，会在 Inspector 中由专属 Drawer 自动渲染为只读预览，防止手动误修改。
    /// </summary>
    [Serializable]
    public struct AddressableKey
    {
        [SerializeField]
        private string _value;

        /// <summary>
        /// 获取寻址 Key 的字符串值。
        /// </summary>
        public string Value => _value ?? string.Empty;

        public AddressableKey(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        // 隐式转换为 string，方便代码中无缝作为字符串传参使用
        public static implicit operator string(AddressableKey key)
        {
            return key.Value;
        }
    }
}
