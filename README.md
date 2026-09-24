# Unity_RootBeardAIFramework

一个为 Unity 打造的轻量级、数据驱动的 AI 框架。集成了**行为树 (BT)**、**有限状态机 (FSM)** 和 **效用 AI (Utility AI)**，并通过三层架构协同工作，帮助你构建分层的游戏 AI 决策。

## ✨ 核心特性

- **三层架构**
  - **行为树**：负责宏观决策，根据黑板数据切换状态（巡逻 / 追击 / 攻击）。
  - **状态机**：管理状态的进入、更新与退出。
  - **WeightedState**：在单个状态内部，基于加权公式在多个行为间实时选优。
- **效用 AI (Utility AI)**：行为通过 `WeightedBehavior` 定义，每个行为由若干 `WeightTerm`（Key × 系数）组成，运行时计算得分并执行最高分行为，无需 if-else 硬编码。
- **黑板 (BlackBoard)**：统一的数据容器，行为树条件、状态转换、权重公式均从黑板读取。
- **黑板驱动 Tick**：行为树仅在黑板数据变化时更新，避免每帧无谓计算。
- **可视化节点编辑器**：内置 GraphWindow，支持缩放、拖拽、连线、搜索、注释、复制粘贴，运行时高亮节点状态（Success / Failure / Running）。
- **黑板实时调试**：运行时可在 Inspector 中查看和修改黑板变量，无需重启即可观察 AI 反应。

![Demo]![Uploading GIF 2026-9-24 21-59-06.gif…]()


## 🏗️ 架构概览

- **BlackBoard**：数据层，存放 Distance、Nearness 等变量
- **BehaviorTree**：决策层，Selector + Condition + Action
- **StateMachine**：执行层，管理状态列表
- **WeightedState**：行为层，劈砍 / 冲刺 / 撤退按权重选优

数据流向：感知脚本写入 BlackBoard → 行为树 Tick → 切换状态 → WeightedState 按权重执行行为。

## 🚀 快速开始

1. 从右侧 [Releases](https://github.com/InabaKuma/Unity_RootBeardAIFramework/releases/latest) 下载最新的 `.unitypackage` 导入项目，或 Clone 本仓库。
2. 场景中创建物体并挂载 `AIController`。
3. 在 `AIController` 面板的 **BlackBoardDefs** 中定义黑板变量（如 `Distance`、`Nearness`）。
4. 在 **States** 列表中添加状态，并把继承自 `IState` 或 `WeightedState` 的脚本拖入。
   - 普通状态：直接实现 `Enter / Update / Exit`。
   - 权重状态：在 `ExecuteBehavior` 中用 `switch (b.Name)` 编写每个行为对应的逻辑。
5. 点击 **"打开 AI 编辑器"**，右键创建节点、连线、配置条件，并设置根节点。
6. 运行游戏，在 GraphWindow 中观察实时执行路径。

## 📖 使用示例：WeightedState

编辑器里只配置**行为的名字、权重公式、Target**；具体做什么，写在状态类的代码里。

编辑器配置（AIController → States → Behaviors）：

| Name | Target | 公式 |
|------|--------|------|
| 劈砍 | 空 | Nearness × 1.2 |
| 冲刺 | 空 | Nearness × 0.6 + Distance × 0.7 |
| 撤退 | 空 | Distance × 1.0 |

状态类代码：

    public class AttackState : WeightedState
    {
        protected override void ExecuteBehavior(WeightedBehavior b)
        {
            switch (b.Name)
            {
                case "劈砍": /* 播放劈砍动画 */ break;
                case "冲刺": /* 向玩家位移 */    break;
                case "撤退": /* 后退 */          break;
            }
        }
    }

两边通过 `b.Name` 字符串对齐。策划改公式不必碰代码，程序加行为不必碰 Inspector。

## 📄 开源协议

本项目基于 [MIT License](LICENSE) 开源，可自由用于商业项目。
