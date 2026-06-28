# BS-CAD-Tools

AutoCAD 设计师辅助插件 — WPF 工具面板、图层管理器、字体修复、ByLayer 修复。

## 项目定位

**本仓库职责**：
- WPF 主工具面板（ShowPanel）
- 图层管理器（LY）
- 字体修复（FIXFONTS / TS）
- ByLayer 修复（SETBYLAYER）
- 标准环境初始化入口（BZ）
- 后续调用 BS-CAD-Standard 标准规则

**不属于本仓库的职责**（由 BS-CAD-Standard 维护）：
- 不维护图层标准源数据
- 不维护文字样式标准源数据
- 不维护标注样式标准源数据
- 不维护打印样式标准源数据

## 标准来源

图层标准、文字样式、标注样式、打印样式、图层迁移规则等标准源数据由 [BS-CAD-Standard](https://github.com/white-dimension/BS_CAD_STANDARD) 仓库维护。本仓库通过 `external/BS-CAD-Standard/config/` 路径引用。

## 当前开发阶段

**结构整理阶段** — 代码已拆分至 `src/BS.CAD.Tools/`，目录职责如下：

```
BS-CAD-Tools/
├─ src/BS.CAD.Tools/     ← 源代码
│   ├─ Commands/         ← AutoCAD 命令入口
│   ├─ Services/         ← 业务服务
│   ├─ Views/            ← WPF 界面
│   ├─ Models/           ← 数据模型
│   ├─ Utils/            ← 工具类
│   └─ Resources/        ← 资源文件（预留）
├─ installer/            ← 部署脚本与注册表
├─ docs/                 ← 文档
│   └─ archive/          ← 历史归档
└─ external/             ← 外部标准（预留）
```

## 构建

```bash
dotnet build src/BS.CAD.Tools/BS.CAD.Tools.csproj
```

输出：`src/BS.CAD.Tools/bin/Debug/net10.0-windows/BS.CAD.Tools.dll`

## AutoCAD 加载

```
命令: NETLOAD
→ 选择 BS.CAD.Tools.dll
→ 输入 ShowPanel / LY / FIXFONTS / TS / BZ / SETBYLAYER
```

## 命令参考

| 命令 | 用途 |
|------|------|
| `ShowPanel` | 显示主工具面板 |
| `LY` | 打开图层管理器 |
| `FIXFONTS` | 修复全图文字字体 |
| `TS` | 标准化文字（微软雅黑） |
| `BZ` | 建立标准环境 |
| `SETBYLAYER` | 全图对象设为 ByLayer |

## 后续计划

- 对接 BS-CAD-Standard 标准规则
- ConfigPathService 接入完整配置文件读取
- 面板功能持续迭代
