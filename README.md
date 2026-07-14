# 喵喵AI任务管理器

> 一只可爱的猫娘AI助手，实时监控你的电脑状态，用活泼可爱的语气告诉你系统发生了什么~

## 这是什么？

一个 WinUI 3 桌面应用，结合系统监控和 AI 分析，用猫娘的口吻为你提供**情绪价值**：

- 🐱 实时显示 CPU/GPU/RAM 使用率
- 🤖 每隔几分钟自动调用 AI，分析你在干什么（办公、游戏、摸鱼...）
- 💬 用可爱的语气回复，让你的电脑使用体验不再无聊
- 📊 统计 Token 消耗和费用，让你知道养这只猫娘花了多少钱

## 开发环境要求

### 必需软件

| 软件 | 版本要求 | 说明 |
|------|----------|------|
| **Visual Studio 2026** | 最新版 | 需要安装「.NET 桌面开发」工作负载 |
| **.NET 8 SDK** | 8.0+ | 项目目标框架 |
| **Windows 10/11** | 1903+ | WinUI 3 最低系统要求 |

### VS 工作负载和组件

安装 Visual Studio 2026 时，请确保勾选：

- ✅ **.NET 桌面开发**（C#）
- ✅ **Windows App SDK**（通常包含在 .NET 桌面开发中）

### 必需 NuGet 包

项目已配置好，`dotnet restore` 会自动安装：

| 包名 | 用途 |
|------|------|
| `Microsoft.WindowsAppSDK` 2.2.0 | WinUI 3 框架 |
| `System.Diagnostics.PerformanceCounter` 8.0.0 | CPU/GPU/RAM 监控 |
| `System.Management` 8.0.0 | WMI 硬件信息查询 |
| `CommunityToolkit.Mvvm` 8.4.0 | MVVM 命令支持 |
| `PInvoke.User32` 0.7.124 | 窗口句柄获取 |

### API 密钥（可选）

AI 分析功能需要 DeepSeek API 密钥：

1. 前往 [DeepSeek 开放平台](https://platform.deepseek.com/) 注册
2. 创建 API 密钥
3. 在应用「设置」页面填入密钥

> 💡 没有 API 密钥也能运行，只是 AI 分析功能不可用，系统监控功能正常。

## 快速开始

```bash
# 克隆项目
git clone <repo-url>
cd meow-ai-tskmgr-ui3

# 还原依赖
dotnet restore

# 运行（必须指定 x64 平台）
dotnet run -p:Platform=x64
```

## 构建命令

```bash
# 调试运行
dotnet run -p:Platform=x64

# 构建
dotnet build -p:Platform=x64

# 清理
dotnet clean -p:Platform=x64
```

> ⚠️ **必须指定 `-p:Platform=x64`**，因为 `PerformanceCounter` 仅支持 x64 平台。

> ⚠️ **不要使用 `PublishSingleFile=true`**，会导致 WinUI 3 应用崩溃（`0xc000027b`）。

## 项目结构

```
├── App.xaml.cs              # 应用入口，初始化服务和转换器
├── MainWindow.xaml           # NavigationView 导航壳
├── Pages/
│   ├── DashboardPage         # 仪表盘：CPU/GPU/RAM、AI分析、统计
│   ├── HardwareInfoPage      # 硬件信息：CPU/GPU/主板/硬盘型号
│   ├── ProcessListPage       # 进程列表：搜索、AI查询
│   └── SettingsPage          # 设置：API配置、分析间隔
├── ViewModels/               # 每个页面一个 ViewModel
├── Services/                 # AIService、ConfigService、SystemMonitorService
├── Models/                   # 数据模型
├── Controls/                 # StatusCard、AIAnalysisCard 自定义控件
├── Converters.cs             # 值转换器（根命名空间）
├── Styles/Cards.xaml         # 卡片样式
└── Assets/appsettings.json   # AI API 配置文件
```

## 功能说明

### 仪表盘
- 实时显示 CPU/GPU/RAM 使用率（每 2 秒刷新）
- 启动时自动触发一次 AI 分析
- 之后每 N 分钟自动分析（可在设置中调整间隔）
- 手动点击「开始分析」按钮触发

### 硬件信息
- 通过 WMI 查询 CPU、GPU、主板、硬盘型号
- 显示操作系统版本、内存大小

### 进程列表
- 显示所有运行中的进程
- 支持按名称搜索
- AI 查询功能：选中进程，让猫娘告诉你它是干什么的

### 设置
- 配置 DeepSeek API 端点、密钥、模型
- 调整 AI 分析间隔（1-60 分钟）

## 已知问题

- GPU 监控在某些系统上返回 0（驱动或硬件不支持）
- AI 分析需要有效的 API 密钥，否则会返回错误
- 没有自动化测试，全靠手动验证

## 技术文档

详细的开发过程中遇到的问题和解决方案，请参阅 [TECHNICAL_DOC.md](TECHNICAL_DOC.md)。

## 许可证

MIT
