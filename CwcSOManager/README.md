# Cwc SO Manager

`Cwc SO Manager` 是一个为 Unity 开发者打造的轻量、高性能 **ScriptableObject (SO) 可视化管理与编辑工具**。基于 Unity 新一代 **UI Toolkit** 构建，旨在解决项目后期 SO 实例过多、散落各处、难以统一修改与分类的问题。

## 🌟 主要功能
* **无限层级分类**：支持在类上添加特性，自动在左侧生成直观的分类折叠树。
* **延迟加载表格**：基于 MultiColumnListView 开发，即使有成千上万个 SO 实例，在表格中滑动或切换也是丝滑流畅。
* **双向数据绑定**：修改数据表中的任意属性，会自动通过 `SerializedObject` 同步回 SO 实例。
* **图片缩略图预览**：支持拖拽外部纹理/精灵（Drag & Drop）至表格单元格内直接完成属性赋值。
* **性能极佳**：通过预提取反射字段、使用原生索引等方式，彻底消除了排序卡顿与运行期 GC 抖动。

## 🔧 安装方法
在 Unity 菜单中选择 `Window` -> `Package Manager`。点击左上角 `+` 号并选择 `Add package from git URL...`，输入下方链接即可完成自动安装：
```text
https://github.com/您的Github用户名/CwcSOManager.git
```

## 🚀 快速上手
1. 在您的 `ScriptableObject` 类上加入管理特性，并声明其折叠层级路径：
   ```csharp
   using CwcSOManager;

   [CwcSOManageable("Characters/Enemy")]
   public class EnemyConfigSO : ScriptableObject {
       [CwcSOColumn(displayName: "怪物生命", width: 120)]
       public float maxHealth;

       [CwcSOPreview(size: 40)]
       public Sprite avatarIcon;
   }
   ```
2. 在 Unity 菜单栏打开：`Tools` -> `CwcSOManager` -> `Open Manager Window` 即可使用。

## 📄 开源许可证
本项目采用 [MIT License](LICENSE) 协议开源。
