# Unity_RootBeardAIFramework

一个为 Unity 打造的轻量级、数据驱动、带有可视化编辑器的 AI 框架。集成了**有限状态机 (FSM)**、**行为树 (BT)** 和 **效用 AI (Utility AI)**，帮助你轻松构建复杂的游戏 AI 逻辑。

## ✨ 核心特性
- **数据驱动**：AI 逻辑与代码分离，使用 ScriptableObject 和 GUID 管理节点与状态。
- **行为树 (Behavior Tree)**：支持 Selector、Sequence、Inverter、Repeater 等节点，内置黑板 (BlackBoard) 系统。
- **有限状态机 (FSM)**：支持状态注册、转换条件配置，与行为树无缝结合。
- **效用 AI (Utility AI)**：基于 Consideration 和 AnimationCurve 动态打分，让 AI 做出最“划算”的决策。
- **可视化节点编辑器**：内置 GraphWindow，支持缩放、拖拽、连线、搜索、注释、实时运行状态高亮。
- **黑板实时调试**：运行时可在 Inspector 中实时查看和修改黑板变量。

## 🚀 快速开始
1. 点击右侧的 [Releases](你的Releases链接) 下载最新的 `.unitypackage`，并导入你的 Unity 项目。
   *(或者 Clone 本仓库，将 `Runtime` 和 `Editor` 文件夹放入你的项目)*。
2. 在场景中创建一个空物体，挂载 `AIController` 组件。
3. 点击 `AIController` 面板上的 **"打开 AI 编辑器"**。
4. 右键创建节点，连线，配置条件，并设置根节点。
5. 配置 `BlackBoardDefs`（黑板变量）与 `StateMachineAsset`。
6. 运行游戏，在 GraphWindow 中观察 AI 的实时执行路径！

## 📄 开源协议
本项目基于 [MIT License](LICENSE) 开源。
