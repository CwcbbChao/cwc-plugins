# CwcInventoryEngine 独立 Demo 模块使用指南

本 Demo 模块放置在独立的 `Assets/CwcPlugins/CwcInventoryEngine/Demo/` 目录中，旨在为用户提供**组件配置、道具生成、组件动态内容、序列化存盘反序列化读盘、以及 UI 全套操作**的快速功能验证。

> 💡 **提示（零侵入提示）**：
> 如果你的项目中不需要此 Demo，或者已完成测试，可以**直接删除整个 `Demo/` 文件夹**，绝对不会影响 `Assets/CwcPlugins/CwcInventoryEngine/Runtime/` 中的核心插件代码！

---

## 📁 目录结构

```
Assets/CwcPlugins/CwcInventoryEngine/Demo/
├── Scripts/
│   ├── Components/
│   │   ├── DemoDurabilityComponentDefinition.cs # 静态耐久度组件定义 (ScriptableObject)
│   │   └── DemoDurabilityComponent.cs           # 运行时动态耐久度组件 (动态修改/堆叠干预)
│   ├── Save/
│   │   ├── DemoItemAssetResolver.cs             # 资产解析器 (用于序列化与反序列化)
│   │   └── DemoSaveLoadManager.cs               # 存盘与读盘管理器 (PlayerPrefs 存储)
│   └── UI/
│       ├── DemoInventoryController.cs           # 背包测试主控制器 (生成道具/组件交互)
│       └── DemoUIWindow.cs                      # 可视化 OnGUI 调试小窗 (一键测试)
└── README_DEMO.md                               # 本测试指南文档
```

---

## 🛠️ 快速搭建测试场景 (3 分钟)

1. **新建/准备测试道具资产 (ItemDefinition)**：
   - 在 Project 窗口右键 `Create -> Cwc -> Inventory -> Component Definition -> Durability Component Definition`，新建一个耐久度组件定义（如设置最大耐久为 100）。
   - 在 Project 窗口右键 `Create -> Cwc -> Inventory -> Item Definition`，新建 3 个测试道具：
     - `Item_Sword`（大剑）：在 `Component Definitions` 列表中引入刚创建的耐久度组件定义。
     - `Item_Potion`（药水）：设置 `MaxStack = 99`。
     - `Item_Gem`（宝石）：设置 `MaxStack = 20`。

2. **搭建场景物体**：
   - 在场景中创建一个 Empty GameObject，命名为 `[InventoryDemo]`。
   - 为其挂载以下组件：
     1. `InventoryComponent`（将 Capacity 设为 20）。
     2. `DemoItemAssetResolver`（将上面创建的 3 个 ItemDefinition 拖入其 `Registered Items` 列表中）。
     3. `DemoSaveLoadManager`（自动关联组件）。
     4. `DemoInventoryController`（将 3 个 ItemDefinition 分别拖入对应的 Serialized 槽位）。
     5. `DemoUIWindow`（可视操控小窗）。

3. **关联/绑定 UI 视图（可选）**：
   - 如果已拉起 `UIInventoryListView` 面板，只需将其拖拽赋值给 `DemoInventoryController` 或 `DemoUIWindow` 的 `UI ListView` 字段即可。

---

## 🧪 验证与测试步骤

启动 Scene 运行项目，Game 视图左上角将自动弹出 **Cwc Inventory Engine 测试面板**：

1. **测试道具动态生成 (Item Spawning)**：
   - 点击面板中的 `+ 大剑`、`+ 药水 x5`、`+ 宝石 x10` 按钮，验证道具成功压入 `InventoryComponent` 容器中。
2. **测试组件动态内容修改 (Dynamic Components & Stacking)**：
   - 在 UI 列表中选中大剑，点击 `扣除选中物品 15 耐久` 按钮。
   - 观察大剑组件内部 `CurrentDurability` 被修改。
   - **堆叠干预验证**：再添加一把满耐久的大剑，验证因为耐久度不一致，系统拒绝将两把大剑合并堆叠。
3. **测试序列化与反序列化 (Save / Load Autonomy)**：
   - 点击 `Save (存盘)` 按钮，控制台打印出的 JSON 字符串中包含了物品 Guid、AssetKey 以及 `DemoDurabilityComponent` 的增量 `CurrentDurability`。
   - 更改/清空当前背包，再点击 `Load (读盘)` 按钮。
   - 验证**所有物品槽位及动态扣除后的耐久度数值被 100% 完美还原**！
4. **测试 UI 全套视图与交互 (UI Sorting & Compact Mode)**：
   - 点击 `按名称` / `按数量` 按钮测试视图无 GC 重新映射排序。
   - 点击 `紧凑模式` 开关，验证空槽位的显示与隐藏。
