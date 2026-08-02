// FileName: CwcAddressableUtility.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CwcAddressable
{
    /// <summary>
    /// CwcAddressable 扩展方法类。
    /// 允许任何 ScriptableObject 无需继承接口即可便捷获取其 Addressable Key。
    /// </summary>
    public static class CwcAddressableExtensions
    {
        /// <summary>
        /// 扩展方法：获取该 ScriptableObject 的 Addressable 寻址 Key（若 Key 为空则回退返回 so.name）。
        /// </summary>
        public static string GetAddressableKey(this ScriptableObject so)
        {
            if (so == null) return string.Empty;
            string key = CwcAddressableUtility.GetAddressableKeyFromAsset(so);
            return !string.IsNullOrEmpty(key) ? key : so.name;
        }
    }

    /// <summary>
    /// CwcAddressable 核心工具类。
    /// </summary>
    public static class CwcAddressableUtility
    {
        // 缓存各 Type 的 AddressableKey 字段信息，将反射检索开销降至极限
        private static readonly Dictionary<Type, FieldInfo> _keyFieldCache = new Dictionary<Type, FieldInfo>();
        private static readonly Dictionary<Type, bool> _hasFieldCache = new Dictionary<Type, bool>();

        /// <summary>
        /// 检查目标 ScriptableObject 的类类型是否声明了 [AutoAddressableKey] 特性。
        /// </summary>
        public static bool IsAutoAddressableKeyTarget(Type type)
        {
            if (type == null) return false;
            return type.GetCustomAttribute<AutoAddressableKeyAttribute>() != null;
        }

        /// <summary>
        /// 校验目标 Type 中是否定义了类型为 AddressableKey 的字段（带高效字典缓存）。
        /// </summary>
        public static bool HasAddressableKeyField(Type type, out FieldInfo foundField)
        {
            foundField = null;
            if (type == null) return false;

            if (_hasFieldCache.TryGetValue(type, out bool hasField))
            {
                if (hasField)
                {
                    _keyFieldCache.TryGetValue(type, out foundField);
                }
                return hasField;
            }

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(AddressableKey))
                {
                    foundField = field;
                    _keyFieldCache[type] = field;
                    _hasFieldCache[type] = true;
                    return true;
                }
            }

            _hasFieldCache[type] = false;
            return false;
        }

        /// <summary>
        /// 从目标 ScriptableObject 实例中提取 AddressableKey 值（采用高速 Field 缓存）。
        /// </summary>
        public static string GetAddressableKeyFromAsset(ScriptableObject target)
        {
            if (target == null) return string.Empty;

            Type type = target.GetType();
            if (HasAddressableKeyField(type, out FieldInfo field) && field != null)
            {
                var keyStruct = (AddressableKey)field.GetValue(target);
                return keyStruct.Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// 根据 Key 使用 Addressables 同步加载解析指定的 UnityEngine.Object 资产。
        /// </summary>
        /// <typeparam name="T">目标资产类型</typeparam>
        /// <param name="key">Addressable 寻址 Key</param>
        /// <returns>加载成功的资产实例，若失败返回 null</returns>
        public static T LoadAssetSync<T>(string key) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(key)) return null;

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                return handle.WaitForCompletion();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CwcAddressable] 通过 Key '{key}' 同步加载类型 {typeof(T).Name} 失败：{ex.Message}");
                return null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器工具：向目标 ScriptableObject 中的 AddressableKey 字段自动注入新的 Key。
        /// </summary>
        public static bool SetAddressableKeyToAsset(ScriptableObject target, string newKey)
        {
            if (target == null) return false;

            Type type = target.GetType();
            if (HasAddressableKeyField(type, out FieldInfo field) && field != null)
            {
                var currentKey = (AddressableKey)field.GetValue(target);
                if (currentKey.Value != newKey)
                {
                    field.SetValue(target, new AddressableKey(newKey));
                    return true;
                }
                return false;
            }

            return false;
        }
#endif
    }
}
