// FileName: CwcAddressableKeyProcessor.cs
using System;
using System.Collections;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CwcAddressable.Editor
{
    /// <summary>
    /// 编辑器自动化处理器：
    /// 1. 监听资产导入与变化 (AssetPostprocessor)；
    /// 2. 监听 Addressables 全局修改事件 (OnModificationGlobal)，全量兼容 SmartAddresser 等批量工具；
    /// 3. 项目编译/启动时全自动排查并静默补全历史旧资产 (Auto Catch-Up)。
    /// </summary>
    [InitializeOnLoad]
    public class CwcAddressableKeyProcessor : AssetPostprocessor
    {
        static CwcAddressableKeyProcessor()
        {
            // 1. 绑定 Addressables 全局修改事件
            AddressableAssetSettings.OnModificationGlobal -= OnAddressableModification;
            AddressableAssetSettings.OnModificationGlobal += OnAddressableModification;

            // 2. 延时全自动排查补齐
            EditorApplication.delayCall += AutoCatchUpMissingKeys;
        }

        #region 全自动补全历史旧资产 (Auto Catch-Up)

        private static void AutoCatchUpMissingKeys()
        {
            SyncAllAddressableKeysInternal(silent: true);
        }

        #endregion

        #region Addressables 事件监听

        private static void OnAddressableModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent ev, object postEventObj)
        {
            bool isDirty = false;

            if (postEventObj is AddressableAssetEntry entry)
            {
                if (ProcessSingleEntry(entry)) isDirty = true;
            }
            else if (postEventObj is IEnumerable collection)
            {
                foreach (var item in collection)
                {
                    if (item is AddressableAssetEntry e)
                    {
                        if (ProcessSingleEntry(e)) isDirty = true;
                    }
                }
            }
            else
            {
                // 如果是组或配置全局变动，静默同步一次
                SyncAllAddressableKeysInternal(silent: true);
            }

            if (isDirty)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static bool ProcessSingleEntry(AddressableAssetEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.AssetPath)) return false;

            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.AssetPath);
            if (so != null && CwcAddressableUtility.IsAutoAddressableKeyTarget(so.GetType()))
            {
                if (CwcAddressableUtility.SetAddressableKeyToAsset(so, entry.address))
                {
                    EditorUtility.SetDirty(so);
                    Debug.Log($"[CwcAddressable] (Addressables 事件触发) 已自动为 '{so.name}' 注入 Key: '{entry.address}'");
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 菜单与全量同步

        /// <summary>
        /// 菜单项：一键全量扫描并同步项目中所有带有 [AutoAddressableKey] 的 SO 资产寻址 Key。
        /// </summary>
        [MenuItem("Tools/CwcAddressable/Sync All Addressable Keys")]
        public static void SyncAllAddressableKeys()
        {
            SyncAllAddressableKeysInternal(silent: false);
        }

        private static void SyncAllAddressableKeysInternal(bool silent)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var targetTypes = TypeCache.GetTypesWithAttribute<AutoAddressableKeyAttribute>();
            int processedCount = 0;

            foreach (Type type in targetTypes)
            {
                if (!typeof(ScriptableObject).IsAssignableFrom(type)) continue;

                if (!CwcAddressableUtility.HasAddressableKeyField(type, out _))
                {
                    if (!silent)
                    {
                        Debug.LogWarning($"[CwcAddressable 结构警告] 类 '{type.Name}' 声明了 [AutoAddressableKey] 特性，但未定义 AddressableKey 字段。");
                    }
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (ProcessSingleAsset(path, guid, settings, type))
                    {
                        processedCount++;
                    }
                }
            }

            if (processedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[CwcAddressable] {(silent ? "(全自动静默补齐)" : "")} 全量同步完成，共修复补齐了 {processedCount} 个资产的 Addressable Key。");
            }
            else if (!silent)
            {
                Debug.Log("[CwcAddressable] 检查完成，所有受托管的 SO 资产 Addressable Key 已是最新。");
            }
        }

        #endregion

        #region 资产管线回调

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            bool isDirty = false;

            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid)) continue;

                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so != null && CwcAddressableUtility.IsAutoAddressableKeyTarget(so.GetType()))
                {
                    if (ProcessSingleAsset(path, guid, settings, so.GetType()))
                    {
                        isDirty = true;
                    }
                }
            }

            if (isDirty)
            {
                AssetDatabase.SaveAssets();
            }
        }

        #endregion

        #region 私有辅助方法

        private static bool ProcessSingleAsset(string path, string guid, AddressableAssetSettings settings, Type assetType)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null) return false;

            ScriptableObject so = AssetDatabase.LoadAssetAtPath(path, assetType) as ScriptableObject;
            if (so == null) return false;

            if (!CwcAddressableUtility.HasAddressableKeyField(assetType, out _))
            {
                return false;
            }

            string expectedKey = entry.address;
            if (CwcAddressableUtility.SetAddressableKeyToAsset(so, expectedKey))
            {
                EditorUtility.SetDirty(so);
                Debug.Log($"[CwcAddressable] 已自动为资产 '{so.name}' ({assetType.Name}) 注入 Key: '{expectedKey}'");
                return true;
            }

            return false;
        }

        #endregion
    }
}
