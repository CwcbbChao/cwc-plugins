using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CwcSOManager
{
    /// <summary>
    /// 树路径节点，用于在左侧以多级文件夹方式折叠显示。
    /// </summary>
    public class CwcPathNode
    {
        #region 序列化属性与字段

        public string Name;
        public string FullPath;
        public Dictionary<string, CwcPathNode> Children = new Dictionary<string, CwcPathNode>();
        public List<Type> SOTypes = new List<Type>();

        #endregion
    }

    /// <summary>
    /// 用于缓存数据表列信息的结构。
    /// </summary>
    public class CwcColumnInfo
    {
        #region 序列化属性与字段

        public FieldInfo Field;
        public string DisplayName;
        public float Width;
        public bool IsPreview;

        #endregion
    }

    /// <summary>
    /// 包含 CwcSOManager 所需的底层反射扫描、排序、搬移等辅助逻辑的静态工具类。
    /// </summary>
    public static class CwcSOManagerHelper
    {
        #region 非序列化私有字段

        // 反射缓存列信息字典
        private static readonly Dictionary<Type, List<CwcColumnInfo>> _columnCache = new Dictionary<Type, List<CwcColumnInfo>>();

        // 缓存已经构建的路径树，在 Domain Reload 时会自动失效清空
        private static CwcPathNode _cachedPathTree;

        #endregion

        #region 公共方法

        /// <summary>
        /// 扫描所有程序集，获取标有 CwcSOManageable 特性的 ScriptableObject 类型，并构建层级多叉树。
        /// </summary>
        /// <returns>路径树根节点列表</returns>
        public static CwcPathNode BuildPathTree(bool forceRefresh = false)
        {
            if (_cachedPathTree != null && !forceRefresh)
            {
                return _cachedPathTree;
            }

            var root = new CwcPathNode { Name = "Root", FullPath = "" };
            var soType = typeof(ScriptableObject);

            // 使用 UnityEditor.TypeCache 快速扫描，避免遍历程序集以极大提升效率
            var manageableTypes = TypeCache.GetTypesWithAttribute<CwcSOManageableAttribute>();

            foreach (var type in manageableTypes)
            {
                if (type == null || type.IsAbstract || !type.IsSubclassOf(soType))
                {
                    continue;
                }

                var manageableAttr = type.GetCustomAttribute<CwcSOManageableAttribute>();
                if (manageableAttr == null)
                {
                    continue;
                }

                string rawPath = string.IsNullOrEmpty(manageableAttr.Path) ? "未分类" : manageableAttr.Path;
                string[] pathParts = rawPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                CwcPathNode currentNode = root;
                string accumulatedPath = "";

                for (int i = 0; i < pathParts.Length; i++)
                {
                    string part = pathParts[i].Trim();
                    accumulatedPath = string.IsNullOrEmpty(accumulatedPath) ? part : $"{accumulatedPath}/{part}";

                    if (!currentNode.Children.TryGetValue(part, out CwcPathNode child))
                    {
                        child = new CwcPathNode
                        {
                            Name = part,
                            FullPath = accumulatedPath
                        };
                        currentNode.Children.Add(part, child);
                    }
                    currentNode = child;
                }

                currentNode.SOTypes.Add(type);
            }

            _cachedPathTree = root;
            return root;
        }

        /// <summary>
        /// 搜寻项目内所有该类型的 ScriptableObject 实例。
        /// </summary>
        /// <param name="type">SO类类型</param>
        /// <returns>实例列表</returns>
        public static List<ScriptableObject> FindAllInstances(Type type)
        {
            var list = new List<ScriptableObject>();
            if (type == null)
            {
                return list;
            }

            string[] guids = AssetDatabase.FindAssets($"t:{type.Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // 优化：检查资源的主类型，避免在模糊匹配时加载无关的同名类型资源
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType == null || (assetType != type && !assetType.IsSubclassOf(type)))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        /// <summary>
        /// 缓存并获取该类型标有 [CwcSOColumn] 或 [CwcSOPreview] 的字段列表。
        /// </summary>
        /// <param name="type">SO类类型</param>
        /// <returns>列信息列表</returns>
        public static List<CwcColumnInfo> GetCachedColumns(Type type)
        {
            if (type == null)
            {
                return new List<CwcColumnInfo>();
            }

            if (_columnCache.TryGetValue(type, out List<CwcColumnInfo> cachedList))
            {
                // 如果缓存中包含字段，直接返回。
                // 若数量为 0，可能是早期编译残留或失效缓存，我们在开发期允许其重新扫描以防锁死
                if (cachedList != null && cachedList.Count > 0)
                {
                    return cachedList;
                }
            }

            var list = new List<CwcColumnInfo>();
            var fields = GetAllFields(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                var previewAttr = field.GetCustomAttribute<CwcSOPreviewAttribute>();
                if (previewAttr != null)
                {
                    list.Add(new CwcColumnInfo
                    {
                        Field = field,
                        DisplayName = field.Name,
                        Width = previewAttr.Size + 10f, // 预留少许边距
                        IsPreview = true
                    });
                    continue;
                }

                var columnAttr = field.GetCustomAttribute<CwcSOColumnAttribute>();
                if (columnAttr != null)
                {
                    list.Add(new CwcColumnInfo
                    {
                        Field = field,
                        DisplayName = string.IsNullOrEmpty(columnAttr.DisplayName) ? field.Name : columnAttr.DisplayName,
                        Width = columnAttr.Width,
                        IsPreview = false
                    });
                }
            }

            _columnCache[type] = list;
            return list;
        }

        /// <summary>
        /// 创建并保存一个 ScriptableObject 实例。
        /// </summary>
        /// <param name="type">SO类类型</param>
        /// <param name="folderPath">目标文件夹路径</param>
        /// <param name="assetName">拟定的资源文件名</param>
        /// <param name="original">复制源（若为复制创建）</param>
        /// <returns>创建后的实例</returns>
        public static ScriptableObject CreateAndSaveAsset(Type type, string folderPath, string assetName, ScriptableObject original = null)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = "Assets/Data";
            }

            CreateDirectoryIfNeeded(folderPath);

            string safeName = string.IsNullOrEmpty(assetName) ? type.Name : assetName;
            string extension = ".asset";
            string path = Path.Combine(folderPath, safeName + extension).Replace("\\", "/");
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);

            ScriptableObject asset;
            if (original != null)
            {
                asset = ScriptableObject.Instantiate(original);
            }
            else
            {
                asset = ScriptableObject.CreateInstance(type);
            }

            AssetDatabase.CreateAsset(asset, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        /// <summary>
        /// 整理零散的实例，将目标类型的所有实例全部搬移到其默认保存文件夹。
        /// </summary>
        /// <param name="type">SO类类型</param>
        /// <param name="defaultFolder">默认保存文件夹路径</param>
        public static void MoveAllSOInstancesToDefaultFolder(Type type, string defaultFolder)
        {
            if (type == null || string.IsNullOrEmpty(defaultFolder))
            {
                return;
            }

            CreateDirectoryIfNeeded(defaultFolder);
            List<ScriptableObject> assets = FindAllInstances(type);

            try
            {
                AssetDatabase.StartAssetEditing(); // 优化：暂停刷新以提高移动性能

                foreach (var asset in assets)
                {
                    if (asset == null)
                    {
                        continue;
                    }

                    string currentPath = AssetDatabase.GetAssetPath(asset);
                    string currentDir = Path.GetDirectoryName(currentPath).Replace("\\", "/");
                    string targetDir = defaultFolder.Replace("\\", "/");

                    // 如果已经在默认文件夹下，就不需要挪动了
                    if (string.Equals(currentDir, targetDir, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string targetPath = Path.Combine(defaultFolder, asset.name + ".asset").Replace("\\", "/");
                    string uniqueTargetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

                    string moveError = AssetDatabase.MoveAsset(currentPath, uniqueTargetPath);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        Debug.LogError($"CwcSOManager: 移动文件 {asset.name} 失败！错误信息: {moveError}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing(); // 优化：恢复并统一刷新
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 对实例列表进行就地排序。
        /// </summary>
        /// <param name="assets">要排序的实例列表</param>
        /// <param name="type">SO类类型</param>
        /// <param name="sortKey">排序的键名。如果是资源名称则传入 "Asset Name"，否则传入具体字段名</param>
        /// <param name="isAscending">是否为升序排序</param>
        public static void SortInstances(List<ScriptableObject> assets, Type type, string sortKey, bool isAscending)
        {
            if (assets == null || assets.Count == 0)
            {
                return;
            }

            // 预先缓存对应的 FieldInfo，避免在 Sort 的循环比较中进行高频反射查找，优化排序响应性能
            FieldInfo cachedFieldInfo = null;
            bool isAssetName = string.Equals(sortKey, "Asset Name", StringComparison.OrdinalIgnoreCase);

            if (!isAssetName && type != null)
            {
                cachedFieldInfo = type.GetField(sortKey, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (cachedFieldInfo == null)
                {
                    // 尝试从基类及全字段中查找
                    var allFields = GetAllFields(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    cachedFieldInfo = allFields.FirstOrDefault(f => f.Name == sortKey);
                }
            }

            assets.Sort((asset1, asset2) =>
            {
                if (asset1 == null && asset2 == null) return 0;
                if (asset1 == null) return isAscending ? -1 : 1;
                if (asset2 == null) return isAscending ? 1 : -1;

                object value1, value2;

                if (isAssetName)
                {
                    value1 = asset1.name;
                    value2 = asset2.name;
                }
                else
                {
                    if (cachedFieldInfo != null)
                    {
                        value1 = cachedFieldInfo.GetValue(asset1);
                        value2 = cachedFieldInfo.GetValue(asset2);
                    }
                    else
                    {
                        value1 = null;
                        value2 = null;
                    }
                }

                if (value1 == null && value2 == null) return 0;
                if (value1 == null) return isAscending ? -1 : 1;
                if (value2 == null) return isAscending ? 1 : -1;

                int comparison;
                if (value1 is IComparable comparable1)
                {
                    comparison = comparable1.CompareTo(value2);
                }
                else
                {
                    comparison = string.Compare(value1.ToString(), value2.ToString(), StringComparison.Ordinal);
                }

                return isAscending ? comparison : -comparison;
            });
        }

        /// <summary>
        /// 格式化全局系统路径为 Assets 相对路径。
        /// </summary>
        public static string GlobalPathToLocal(string globalPath)
        {
            if (string.IsNullOrEmpty(globalPath))
            {
                return "";
            }

            string cleanPath = globalPath.Replace("\\", "/");
            int assetsIndex = cleanPath.IndexOf("/Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return cleanPath.Substring(assetsIndex + 1);
            }

            assetsIndex = cleanPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return cleanPath.Substring(assetsIndex);
            }

            if (cleanPath.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            return "";
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取类的所有字段（包括私有字段 and 基类字段）。
        /// </summary>
        private static IEnumerable<FieldInfo> GetAllFields(Type type, BindingFlags bindingFlags)
        {
            if (type == null || type == typeof(ScriptableObject) || type == typeof(UnityEngine.Object))
            {
                return Enumerable.Empty<FieldInfo>();
            }

            BindingFlags flags = bindingFlags | BindingFlags.DeclaredOnly;
            var currentFields = type.GetFields(flags);
            var baseFields = GetAllFields(type.BaseType, bindingFlags);

            return currentFields.Concat(baseFields);
        }

        /// <summary>
        /// 如果目录不存在，则在 Assets 下创建对应文件夹。
        /// </summary>
        private static void CreateDirectoryIfNeeded(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string cleanPath = path.Replace("\\", "/");
            if (Directory.Exists(cleanPath))
            {
                return;
            }

            // 递归创建 Unity 资产文件夹，这不仅创建操作系统物理文件夹，而且通知 AssetDatabase，防止生成 Meta 文件出错
            string[] folders = cleanPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string parent = folders[0]; // 必须是 Assets

            for (int i = 1; i < folders.Length; i++)
            {
                string child = folders[i];
                string fullParentPath = parent;
                string currentPath = $"{parent}/{child}";

                if (!Directory.Exists(currentPath))
                {
                    AssetDatabase.CreateFolder(fullParentPath, child);
                }
                parent = currentPath;
            }
        }

        #endregion
    }
}
