# Cwcbb Plugins Framework

个人通用 Unity 工具库与框架集合，支持跨项目复用与版本统一管理。兼容 **Unity 2021.3+** 及 **Unity 6 (6000.0+)** 等版本。

## 📦 模块概览

| 模块名称 | 功能说明 |
| :--- | :--- |
| **CwcUIFramework** | 界面栈与 UI 框架管理 |
| **CwcStateLayer** | 状态机与状态层级控制器 |
| **CwcInventoryEngine** | 背包系统与物品逻辑 |
| **CwcAddressable** | Addressable 可寻址资源加载扩展 |
| **CwcSOManager** | ScriptableObject 数据与资源配置管理器 |
| **CwcRemoteControl** | 远程控制与调试命令注入 |
| **CwcRoomBuilder / CwcNewRoomBuilder** | 关卡与房间快速构建工具 |
| **CwcVFX** | 特效工具与视觉辅助 |

## 🚀 安装与使用方法

您可以选择以下任意一种 Unity 标准方式将本项目导入到您的工程中：

### 方式一：通过 Package Manager 界面添加（推荐）

1. 打开 Unity 编辑器，点击顶部菜单栏 **`Window`** -> **`Package Manager`**。
2. 点击窗口左上角的 **`+`** 加号图标。
3. 在弹出的下拉菜单中选择 **`Add package from git URL...`**。
4. 在文本框中粘贴下方仓库 URL 并点击 **`Add`**：

```text
https://github.com/CwcbbChao/cwc-plugins.git
```

---

### 方式二：修改 manifest.json 配置文件

直接打开工程目录中的 `Packages/manifest.json` 文件，在 `dependencies` 节点下添加对应配置：

```json
{
  "dependencies": {
    "com.cwcbb.plugins": "https://github.com/CwcbbChao/cwc-plugins.git"
  }
}
```

## 🔄 更新检查与升级

本项目默认追踪 `main` 主分支：
* 当远程仓库有新功能发布或 Bug 修复时，在 Unity 的 **Package Manager** 窗口中选中 **Cwcbb Plugins**，直接点击右下角的 **`Update`** 按钮即可一键升级至最新代码。
