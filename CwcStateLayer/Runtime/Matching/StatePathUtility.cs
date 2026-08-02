using System;

namespace Cwcbb.Tools.CwcStateLayer
{
    /// <summary>
    /// 状态路径哈希工具类，提供高效稳定的 32 位 FNV-1a 字符串哈希计算，用于消除运行期路径匹配的字符串开销。
    /// </summary>
    public static class StatePathUtility
    {
        #region 常量定义

        private const uint FnvOffsetBasis32 = 0x811C9DC5;
        private const uint FnvPrime32 = 0x01000193;

        #endregion

        #region 公共静态哈希计算接口

        /// <summary>
        /// 将状态路径或 ID 转换为确定性的 32 位整数哈希值（大小写不敏感，统一转为大写比对）
        /// </summary>
        /// <param name="path">路径或 ID 字符串</param>
        /// <returns>计算出的整数哈希值</returns>
        public static int StringToHash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return 0;
            }

            uint hash = FnvOffsetBasis32;
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                // 统一转换为大写，保证忽略大小写的路径匹配逻辑
                if (c >= 'a' && c <= 'z')
                {
                    c = (char)(c - 32);
                }

                hash ^= c;
                hash *= FnvPrime32;
            }

            return (int)hash;
        }

        #endregion
    }
}
