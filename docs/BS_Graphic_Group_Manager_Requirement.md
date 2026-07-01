# BS CAD Tools — 图形组管理器需求文档

## 1. 功能背景

当前图层管理器的"过滤标签"（FilterTagInfo）是扁平结构：
- 每次启动不恢复
- 不支持层级展开/折叠
- 只存图层名列表，无属性条件
- 不与 DWG 绑定，换了环境就要重新设

希望设计一个类似 3ds Max 的"组"管理器：
- 直观的树形结构，点击展开/收缩
- 和当前 DWG 绑定，换电脑打开同一张图，组还在
- 兼容 AutoCAD 原生图层过滤器
- 第一版 MVP 只做"图层组"，不做对象组

---

## 2. MVP 范围（第一版）

### 2.1 核心概念

**图形组（GraphicGroup）** 是一组图层的命名集合，以树形结构组织。

- 组可以嵌套（父组 → 子组）
- 展开/收缩状态持久化
- 与当前 DWG 绑定
- 不破坏 AutoCAD 原生图层过滤器

### 2.2 MVP 包含的功能

| 功能 | 说明 |
|------|------|
| 新建组 | 从选中图层创建组，或创建空组 |
| 删除组 | 删除组但不删除图层 |
| 重命名组 | 修改组名 |
| 展开/收缩 | 树形展开折叠，状态持久化 |
| 选中组 → 过滤显示 | 点击组自动过滤图层列表 |
| 右键菜单 | 添加/移除图层、重命名、删除 |
| 拖拽图层到组 | 支持将图层拖拽加入组 |
| 拖拽组嵌套 | 支持将一个组拖入另一个组 |
| 数据持久化 | 使用 Named Object Dictionary + XRecord |
| 兼容现有过滤器 | 不破坏 _quickFilter / 搜索 / 过滤标签 |

### 2.3 MVP 暂不实现

| 功能 | 原因 |
|------|------|
| 对象组（Entity Group） | 超出 MVP 范围，涉及 AutoCAD 对象选择集 |
| 组条件过滤（如"颜色=红色"的自动组） | 增加复杂度，后续版本实现 |
| 跨图纸复制组 | MVP 只绑定单 DWG |
| 组的可见性/冻结/锁定开关 | 以后可以在组上直接操作图层状态 |
| 组的图标/颜色标识 | UI 细节，后续优化 |

---

## 3. 技术方案

### 3.1 数据存储：AutoCAD Named Object Dictionary + XRecord

使用 AutoCAD 的 Named Object Dictionary（命名对象字典）存储组数据。

```
Database
└── NamedObjectsDictionary
    └── "BS_CAD_GRAPHIC_GROUPS" (XRecord)
        ├── Groups (List<GraphicGroupData>)
        │   ├── Group 1
        │   │   ├── Id (guid)
        │   │   ├── Name
        │   │   ├── ParentId (guid | null)
        │   │   ├── SortOrder (int)
        │   │   ├── IsExpanded (bool)
        │   │   └── LayerNames (List<string>)
        │   └── Group 2 ...
        └── Version (int)
```

**为什么选 Named Object Dictionary + XRecord：**
- 与 DWG 绑定，换电脑打开组还在
- 不依赖外部文件
- AutoCAD 原生支持，稳定性好
- 不需要额外的数据库/配置文件

**为什么不选 JSON 文件：**
- 换电脑/发图给别人，组信息丢失
- 需要额外维护文件路径
- 与 DWG 绑定是硬需求

### 3.2 数据结构设计

```csharp
// Engine/Group/GraphicGroupData.cs
namespace BS.CAD.Tools.Engine.Group
{
    /// <summary>单个图形组的数据（序列化到 XRecord）</summary>
    public class GraphicGroupData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string? ParentId { get; set; } // null = 根级组
        public int SortOrder { get; set; }
        public bool IsExpanded { get; set; } = true;
        public List<string> LayerNames { get; set; } = new();
    }
}
```

```csharp
// Engine/Group/GraphicGroupManager.cs
namespace BS.CAD.Tools.Engine.Group
{
    /// <summary>图形组管理器的核心引擎</summary>
    public class GraphicGroupManager
    {
        // ── 字段 ──
        private Dictionary<string, GraphicGroupData> _groups = new();
        private bool _dirty; // 是否有未保存更改

        // ── 组 CRUD ──
        public GraphicGroupData CreateGroup(string name, string? parentId = null);
        public bool DeleteGroup(string groupId);
        public bool RenameGroup(string groupId, string newName);
        public GraphicGroupData? GetGroup(string groupId);
        public List<GraphicGroupData> GetAllGroups();
        public List<GraphicGroupData> GetRootGroups(); // parentId == null

        // ── 树结构 ──
        public List<GraphicGroupData> GetChildGroups(string parentId);
        public bool MoveGroup(string groupId, string? newParentId);
        public bool ReorderGroup(string groupId, int newOrder);

        // ── 图层关系 ──
        public bool AddLayerToGroup(string groupId, string layerName);
        public bool RemoveLayerFromGroup(string groupId, string layerName);
        public List<string> GetGroupLayerNames(string groupId);
        public HashSet<string> GetAllGroupLayerNames(); // 所有组的图层名（去重）

        // ── 展开/折叠 ──
        public bool SetGroupExpanded(string groupId, bool expanded);

        // ── 持久化 ──
        public void LoadFromDatabase(Database db);
        public void SaveToDatabase(Database db);
        public bool HasUnsavedChanges => _dirty;

        // ── 兼容现有过滤器 ──
        public HashSet<string> GetActiveFilterLayerNames(string? activeGroupId);
    }
}
```

### 3.3 XRecord 序列化方案

AutoCAD XRecord 支持存储 `TypedValue` 列表。有两种序列化方案：

**方案 A：JSON → Buffer → XRecord（推荐）**

```
1. 将 List<GraphicGroupData> 序列化为 JSON string
2. 将 JSON string 转为 byte[]
3. 存入 XRecord 的多个 TypedValue (1005, buffer)
```

优点：
- 序列化/反序列化简单
- 修改数据不需要改 XRecord 结构
- 便于 debug（可以提取 JSON 查看）

缺点：
- JSON 包体略大（对组数据来说可忽略）

**方案 B：直接 TypedValue 映射**

```
每个组映射为一段 TypedValue 序列：
TypedValue(1005, "BS_GROUP")       // 标记开始
TypedValue(1000, groupId)
TypedValue(1000, name)
TypedValue(1000, parentId ?? "")
TypedValue(1070, sortOrder)
TypedValue(1070, isExpanded ? 1 : 0)
TypedValue(1000, layerName1)
TypedValue(1000, layerName2)
...
TypedValue(1005, "BS_GROUP_END")   // 标记结束
```

优点：
- 纯 AutoCAD 原生格式
- 其他 ARX/.NET 应用可以读取

缺点：
- 编码/解码代码量大
- 数据结构变更需要版本兼容

**推荐方案 A**，稳定简单。

### 3.4 类结构总览

```
src/BS.CAD.Tools/
├── Engine/
│   └── Group/
│       ├── GraphicGroupData.cs         # 数据模型
│       ├── GraphicGroupManager.cs      # 核心引擎
│       └── GraphicGroupXRecord.cs      # XRecord 序列化/反序列化
├── Views/
│   └── LayerGroupPanel.xaml            # 组面板控件（MVP用简单TreeView）
│   └── LayerGroupPanel.xaml.cs         # 组面板逻辑
└── ...
```

### 3.5 与现有系统的兼容

现有过滤系统（LayerManagerView）的处理管道：

```
_cacheList (所有图层)
  → _activeFilter (FilterTagInfo, 自定义过滤标签)
  → _quickFilter (On/Off/Frozen/Locked/Current)
  → TxtSearch (文字搜索)
  → GridLayers.ItemsSource (最终显示)
```

新增图形组后，管道变为：

```
_cacheList (所有图层)
  → _activeGroupFilter (GraphicGroup, 新增)
  → _activeFilter (FilterTagInfo, 保持不变)
  → _quickFilter (On/Off/...，保持不变)
  → TxtSearch (文字搜索，保持不变)
  → GridLayers.ItemsSource
```

要求：
- 组过滤优先级最高（先过滤组，再应用其他过滤）
- 组激活时，标签过滤和文字搜索仍在组内生效
- 组取消激活，恢复到全部图层

---

## 4. 命令名称

| 命令 | 用途 | 绑定 |
|------|------|------|
| `GGROUP` | 打开图形组管理器面板 | 临时使用 LayerManagerView 内嵌面板 |
| 组面板右键菜单 | 新建组 / 重命名 / 删除 | TreeView 右键 |

MVP 阶段组管理器不单独开窗口，在图层管理器右侧或底部嵌入。

---

## 5. 开发步骤

### Step 1: 数据模型 + XRecord 序列化

- 创建 `Engine/Group/GraphicGroupData.cs`
- 创建 `Engine/Group/GraphicGroupXRecord.cs`
- 实现 Save → XRecord / Load ← XRecord
- 单元测试：写入/读取 XRecord 验证数据完整性

### Step 2: GraphicGroupManager 核心逻辑

- 创建 `Engine/Group/GraphicGroupManager.cs`
- 实现 CRUD、树结构、图层关系
- 与 LayerEngine 集成（读取当前 Database）

### Step 3: 组面板 UI

- 创建 `Views/LayerGroupPanel.xaml` + `.cs`
- WPF TreeView 绑定组数据
- 展开/折叠、重命名、右键菜单
- 拖拽支持（MoveGroup）

### Step 4: 集成到 LayerManagerView

- 在图层管理器右侧加一个可折叠面板（GroupPanel）
- 点击组 → 过滤图层列表
- 保存/恢复展开状态
- 图层列表与组面板联动

### Step 5: 测试 + 兼容验证

- 验证不破坏现有过滤标签
- 验证不破坏 AutoCAD 原生过滤器
- 验证保存 → 关闭图纸 → 打开图纸 → 组还在
- 验证 NETLOAD → 卸载 → NETLOAD 后组数据仍在

---

## 6. 与现有过滤标签的关系

| 维度 | 现有过滤标签 (FilterTagInfo) | 图形组 (GraphicGroup) |
|------|-----------------------------|----------------------|
| 持久化 | JSON 文件（启动时不恢复） | DWG XRecord（始终恢复） |
| 层级 | 扁平 | 树形 |
| 展开/折叠 | 无 | 支持 |
| 跨图纸 | 不绑定 | 绑定 DWG |
| 拖拽 | 不支持 | 支持 |
| 与过滤器兼容 | — | 叠加过滤 |

**MVP 阶段两者并存**，后续可逐步将过滤标签迁移到图形组体系。

---

## 7. UI 交互参考（3ds Max 风格）

```
┌─────────────────────────────────┐
│ 图形组管理器  [+ 新建组]         │ ← 标题栏 + 按钮
├─────────────────────────────────┤
│ ▼ 建筑图层组                    │ ← 展开状态，蓝色选中
│   ├─ A-WALL                    │
│   ├─ A-DOOR                    │
│   └─ A-WINDOW                  │
│ ▶ 结构图层组                    │ ← 收缩状态
│ ▶ 机电图层组                    │
│   (拖拽图层/组到此处)           │ ← Drop hint
└─────────────────────────────────┘
```

---

## 8. 风险与注意事项

1. **XRecord 大小限制**：单个 XRecord 建议不超过 16KB。图层组数据通常很小（几十个组名+图层名），没问题。如果未来扩展到对象组，考虑分多个 XRecord 存储。

2. **DBObject 生命周期**：XRecord 随 DWG 保存，但需要确保在 `Database` 关闭前写入。建议在 `Document.Database.CloseSave` 或 `Document.CommandEnded` 事件中触发保存。

3. **并发修改**：组数据在内存中维护一份副本，Engine 方法修改后标记 `_dirty`。多个命令同时修改时需要锁。

4. **Undo/Redo**：MVP 暂不实现 Undo/Redo。后续可以在 GraphicGroupManager 上加 Command pattern 支持。

5. **卸载插件后数据安全**：XRecord 存储在 DWG 中，卸载插件后 XRecord 不会丢失，但不会被读取。重新加载插件后恢复访问。这是安全的设计。

---

## 9. 后续版本规划

| 版本 | 内容 |
|------|------|
| V1 (MVP) | 图层组：新建/删除/重命名/展开折叠/过滤/持久化 |
| V2 | 组可见性/冻结/锁定一键开关 |
| V3 | 组条件过滤（规则引擎："名称前缀=xxx 的图层自动加入组"） |
| V4 | 对象组（Entity Group） |
| V5 | 跨图纸复制组 + 组模板 |

---

*文档版本：v1.0*
*更新日期：2026-07-01*
