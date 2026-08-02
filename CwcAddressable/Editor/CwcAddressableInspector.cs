// FileName: CwcAddressableInspector.cs
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CwcAddressable.Editor
{
    /// <summary>
    /// 为所有带有 [AutoAddressableKey] 特性的 ScriptableObject，
    /// 在 Inspector 顶部绘制单行极简 Addressable 状态标签与微型同步按钮，并在选中时静默自动修正 Key。
    /// 采用 Unity Editor 原生全局事件 UnityEditor.Editor.finishedDefaultHeaderGUI 实现完全非排斥、零侵入的无感绘制。
    /// </summary>
    [InitializeOnLoad]
    public static class CwcAddressableHeaderOverlay
    {
        static CwcAddressableHeaderOverlay()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnFinishedDefaultHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGUI;
        }

        private static void OnFinishedDefaultHeaderGUI(UnityEditor.Editor editor)
        {
            if (editor == null || editor.target == null) return;

            UnityEngine.Object target = editor.target;
            Type targetType = target.GetType();

            if (!CwcAddressableUtility.IsAutoAddressableKeyTarget(targetType)) return;
            ScriptableObject soTarget = target as ScriptableObject;
            if (soTarget == null) return;

            bool hasKeyField = CwcAddressableUtility.HasAddressableKeyField(targetType, out _);

            // 自动排查静默修复新建/复制产生的 Key 不一致问题
            if (hasKeyField)
            {
                AutoFixKeyIfOutSync(soTarget);
            }

            DrawAddressableKeyHeader(soTarget, hasKeyField);
        }

        private static void AutoFixKeyIfOutSync(ScriptableObject soTarget)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            string assetPath = AssetDatabase.GetAssetPath(soTarget);
            if (string.IsNullOrEmpty(assetPath)) return;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = !string.IsNullOrEmpty(guid) ? settings.FindAssetEntry(guid) : null;

            if (entry != null)
            {
                string currentKey = soTarget.GetAddressableKey();
                if (currentKey != entry.address)
                {
                    if (CwcAddressableUtility.SetAddressableKeyToAsset(soTarget, entry.address))
                    {
                        EditorUtility.SetDirty(soTarget);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[CwcAddressable] (选中自动校验) 已成功修复新建/复制资产 '{soTarget.name}' 的 Key: '{entry.address}'");
                    }
                }
            }
        }

        private static void DrawAddressableKeyHeader(ScriptableObject soTarget, bool hasKeyField)
        {
            // 1. 结构错误提示（单行）
            if (!hasKeyField)
            {
                EditorGUILayout.HelpBox($"❌ 缺少 AddressableKey 字段！请在类中添加 private AddressableKey _addressableKey;", MessageType.Error);
                return;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            string assetPath = AssetDatabase.GetAssetPath(soTarget);
            string guid = !string.IsNullOrEmpty(assetPath) ? AssetDatabase.AssetPathToGUID(assetPath) : string.Empty;
            AddressableAssetEntry entry = (!string.IsNullOrEmpty(guid) && settings != null) ? settings.FindAssetEntry(guid) : null;

            string currentKey = soTarget.GetAddressableKey();

            // 2. 极致紧凑单行状态栏
            EditorGUILayout.BeginHorizontal();

            if (entry != null)
            {
                bool isSynced = currentKey == entry.address;
                string groupName = entry.parentGroup != null ? entry.parentGroup.Name : "Group";
                string statusText = isSynced ? $"🔑 [{groupName}] Addressable 状态: 已同步" : $"⚠ [{groupName}] 状态: Key 需要同步";

                GUI.color = isSynced ? new Color(0.7f, 0.95f, 0.7f) : new Color(1.0f, 0.85f, 0.5f);
                GUILayout.Label(statusText, EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
                GUI.color = Color.white;

                if (GUILayout.Button("Sync Key", EditorStyles.miniButton, GUILayout.Width(65)))
                {
                    if (CwcAddressableUtility.SetAddressableKeyToAsset(soTarget, entry.address))
                    {
                        EditorUtility.SetDirty(soTarget);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[CwcAddressable] 已手动为 '{soTarget.name}' 同步 Key: '{entry.address}'");
                    }
                }
            }
            else
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label("🔑 Addressable 状态: 未在 Addressables 中注册", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }
    }
}
