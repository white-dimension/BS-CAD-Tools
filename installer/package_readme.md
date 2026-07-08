# BS-CAD-Tools 发布包

## 适用版本

| 项目 | 版本 |
|------|------|
| AutoCAD | **2027**（R26.0） |
| .NET | 10.0（AutoCAD 2027 内置） |
| OS | Windows 10 / 11 64-bit |

> `PackageContents.xml` 中 `SeriesMin="R26.0"` 对应 AutoCAD 2027。
> 不兼容 AutoCAD 2024 及更早版本。

## 部署方式

### ApplicationBundle（推荐）

将 `BS-CAD-Tools.bundle` 整个目录复制到：

```text
%ProgramData%\Autodesk\ApplicationPlugins\
```

重启 AutoCAD 后插件自动加载。

### NETLOAD 手动加载

1. AutoCAD 中运行 `NETLOAD`
2. 选择 `Contents\BS.CAD.Tools.dll`
3. 运行 `ShowPanel` 打开主面板

## Bundle 目录结构

```
BS-CAD-Tools.bundle/
├─ PackageContents.xml     ← AutoCAD 自动加载配置
└─ Contents/
   ├─ BS.CAD.Tools.dll     ← 插件主程序
   ├─ AutoCAD.NET.dll      ← 依赖（自动引用）
   ├─ AutoCAD.NET.Core.dll
   └─ AutoCAD.NET.Model.dll
```

## 打包方法

在仓库根目录运行：

```powershell
.\installer\build_bundle.ps1
```

输出：`dist\BS-CAD-Tools.bundle\`

## 常用命令

| 命令 | 用途 |
|------|------|
| `ShowPanel` | 打开 CAD 助手主工具面板 |
| `LY` | 打开图层管理器 |
| `FIXFONTS` | 修复全图文字样式 |
| `TS` | 标准化文字（微软雅黑） |
| `SETBYLAYER` | 全图对象属性设置为随层 |
| `BZ` | 标准环境初始化入口 |
