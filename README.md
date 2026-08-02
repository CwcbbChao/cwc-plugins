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

## 🚀 使用方法

在其他 Unity 项目的 `Packages/manifest.json` 中添加依赖：

```json
{
  "dependencies": {
    "com.cwcbb.plugins": "https://github.com/CwcbbChao/cwc-plugins.git#v1.0.1"
  }
}
```
