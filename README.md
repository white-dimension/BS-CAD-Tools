# BS-CAD-Tools

AutoCAD 设计辅助插件。当前阶段定位为 **IME Core**：先把输入法自动切换和主面板基础壳打稳，其他会修改图纸的模块先保留代码、保留命令，但默认不在主面板开放。

## 当前阶段：v0.5-layout-polish

StandardTools 模块完成分组整理，将 BS 标准工具入口划分为：
- 初始化
- 图层标准
- 图层显示
- CTB / 模板

本阶段只优化面板结构，不新增业务功能。

## 前期阶段：v0.3-standard-status

StandardTools 模块新增 BS-CAD-Standard 加载状态检测：
- 面板显示 BS-CAD-Standard 是否已加载
- 未检测到时点击标准工具按钮会提示先 NETLOAD `BS_CAD_STANDARD_V10_Plugin.dll`
- 面板提供"刷新检测"按钮
- StandardTools 只检测程序集是否已加载，不校验插件版本，不自动加载 DLL

## 前期阶段：IME Core

当前默认只开放：

- `ShowPanel` 主面板入口
- 输入法检测
- 输入法自动切换
- 中文/英文输入法选择与保存
- 模块开关基础设置
- `Logger` / `TraceLog` 日志能力

暂时隐藏但保留代码和命令：

- 图层管理器 `LY`
- 字体修复 `FIXFONTS`
- 文字标准化 `TS`
- ByLayer 清理 `SETBYLAYER`
- 标准初始化入口 `BZ`

这些功能后续会按模块逐步接回。当前目标是先确认启动、输入法切换、主面板、设置保存和日志都稳定。

StandardTools 模块已完成第一版接入，可通过主面板按钮调用 BS-CAD-Standard 插件命令。需要同时 NETLOAD：
- `BS.CAD.Tools.dll`
- `BS_CAD_STANDARD_V10_Plugin.dll`

BS-CAD-Tools 仅通过 `SendStringToExecute` 发送命令，不直接实现标准化业务逻辑。

## 模块默认状态

首次运行或旧配置迁移后，模块默认值为：

| 模块 | 默认状态 |
| --- | --- |
| 输入法工具 `ImeTools` | 开启 |
| 图层工具 `LayerTools` | 关闭 |
| 字体工具 `FontTools` | 关闭 |
| 图纸清理 `CleanupTools` | 关闭 |
| 标准环境 `StandardTools` | 关闭 |

主面板的“模块管理”只控制显示/隐藏，不删除命令，也不删除代码。

## 构建

```bash
dotnet build src/BS.CAD.Tools/BS.CAD.Tools.csproj
```

输出位置：

```text
src/BS.CAD.Tools/bin/Debug/net10.0-windows/BS.CAD.Tools.dll
```

## AutoCAD 加载

```text
命令：NETLOAD
选择：BS.CAD.Tools.dll
主面板：ShowPanel
```

## 命令边界

| 命令 | 当前阶段说明 |
| --- | --- |
| `ShowPanel` | 默认核心入口，显示输入法设置与模块管理 |
| `LY` | 命令保留，主面板默认隐藏 |
| `FIXFONTS` | 命令保留，主面板默认隐藏 |
| `TS` | 命令保留，主面板默认隐藏 |
| `SETBYLAYER` | 命令保留，主面板默认隐藏，仅处理模型空间和图纸空间 |
| `BZ` | 安全占位，不修改图纸（StandardTools 面板已改用 BS-CAD-Standard 命令） |

## 后续路线

```text
v0.1 输入法核心
v0.2 字体修复
v0.3 ByLayer 清理
v0.4 图层管理器
v0.5 标准配置
v0.6 模板 / CTB / 标注
```
