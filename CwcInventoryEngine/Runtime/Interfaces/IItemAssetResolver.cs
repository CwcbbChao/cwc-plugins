namespace Cwc.InventoryEngine
{
    /// <summary>
    /// 可插拔物品资产解析器接口。
    /// 允许开发者解耦底层加载机制（如 Addressables, Resources, AssetDatabase GUID 或全局配置表）。
    /// </summary>
    public interface IItemAssetResolver
    {
        /// <summary>
        /// 根据物品 ScriptableObject 资产获取持久化存盘 Token（AssetKey）。
        /// </summary>
        /// <param name="definition">静态物品定义资产</param>
        /// <returns>资产标识 Token</returns>
        string GetAssetKey(ItemDefinition definition);

        /// <summary>
        /// 根据存盘 Token（AssetKey）解析并还原物品 ScriptableObject 资产。
        /// </summary>
        /// <param name="assetKey">资产标识 Token</param>
        /// <returns>静态物品定义资产</returns>
        ItemDefinition ResolveDefinition(string assetKey);
    }
}
