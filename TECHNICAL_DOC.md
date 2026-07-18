# 喵喵AI任务管理器 WinUI3 项目技术文档

## 项目概述

将 `meow-ai-tskmgr-winui` 项目重构为标准的 WinUI Gallery 风格架构，采用 MVVM 模式、XAML 数据绑定、页面导航等现代 WinUI3 开发实践。

## 架构设计

```
meow-ai-tskmgr-ui3/
├── App.xaml(.cs)                 # 应用入口，服务初始化
├── MainWindow.xaml(.cs)          # 主窗口，NavigationView + Frame 导航
├── Pages/
│   ├── DashboardPage.xaml(.cs)   # 仪表盘页面
│   ├── SettingsPage.xaml(.cs)    # 设置页面
│   └── SystemInfoPage.xaml(.cs)  # 系统信息页面
├── Controls/
│   ├── StatusCard.xaml(.cs)      # 状态卡片控件
│   └── AIAnalysisCard.xaml(.cs)  # AI 分析卡片控件
├── ViewModels/
│   ├── DashboardViewModel.cs     # 仪表盘 ViewModel
│   ├── SettingsViewModel.cs      # 设置 ViewModel
│   └── SystemInfoViewModel.cs    # 系统信息 ViewModel
├── Models/
│   └── SystemStatus.cs           # 数据模型
├── Services/
│   ├── AIService.cs              # AI 服务
│   ├── ConfigService.cs          # 配置服务
│   └── SystemMonitorService.cs   # 系统监控服务
├── Helpers/
│   ├── ThemeHelper.cs            # 主题管理
│   ├── WindowHelper.cs           # 窗口管理
│   └── SettingsHelper.cs         # 设置持久化
├── Converters.cs                 # 值转换器
└── Styles/
    └── Cards.xaml                # 样式资源
```

---

## 遇到的问题及解决方案

### 问题 1：CommunityToolkit.Mvvm `[ObservableProperty]` 在 WinUI 3 中无法工作

**症状：**
```
未在类型"DashboardViewModel"中找到属性"CpuUsage"
未在类型"DashboardViewModel"中找到属性"GpuUsage"
...
```

**原因：**
CommunityToolkit.Mvvm 的 `[ObservableProperty]` 特性使用源生成器在编译时生成属性，但 WinUI 3 的 XAML 编译器在处理 `x:Bind` 绑定时无法看到这些生成的属性。

**解决方案：**
将所有 ViewModel 中的 `[ObservableProperty]` 改为手动实现的属性，使用 `INotifyPropertyChanged` 接口：

```csharp
// 错误方式（CommunityToolkit.Mvvm）
[ObservableProperty]
private float _cpuUsage;

// 正确方式（手动实现）
private float _cpuUsage;
public float CpuUsage
{
    get => _cpuUsage;
    set { _cpuUsage = value; OnPropertyChanged(); }
}
```

---

### 问题 2：XAML 转换器无法解析

**症状：**
```
XamlCompiler error WMC0001: Unknown type 'FloatToStringConverter' in XML namespace 'using:meow_ai_tskmgr_ui3.Converters'
```

**原因：**
XAML 编译器在编译时无法解析本地命名空间中的转换器类型。

**解决方案：**
在 `App.xaml.cs` 的 `OnLaunched` 方法中动态添加转换器到资源字典（而不是在 XAML 中声明）：

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    InitializeConverters();
    MainWindow = new MainWindow();
    MainWindow.Activate();
}

private void InitializeConverters()
{
    Resources["FloatToStringConverter"] = new FloatToStringConverter();
    Resources["UInt64ToStringConverter"] = new UInt64ToStringConverter();
    Resources["StringToVisibilityConverter"] = new StringToVisibilityConverter();
    Resources["PercentageToWidthConverter"] = new PercentageToWidthConverter { MaxWidth = 120 };
}
```

---

### 问题 3：COMException 应用启动崩溃

**症状：**
```
System.Runtime.InteropServices.COMException (位于 WinRT.Runtime.dll 中)
程序已退出，返回值为 4294967295 (0xffffffff)
```

**原因：**
`InitializeConverters()` 在 `InitializeComponent()` 之后立即调用，但 XAML 资源还没有完全准备好。

**解决方案：**
将 `InitializeConverters()` 移到 `OnLaunched` 方法中，确保在 XAML 完全加载后再初始化转换器：

```csharp
// 错误方式
public App()
{
    this.InitializeComponent();
    InitializeConverters();  // 太早了！
}

// 正确方式
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    InitializeConverters();  // XAML 已加载
    MainWindow = new MainWindow();
    MainWindow.Activate();
}
```

---

### 问题 4：`WinRT.Interop.WindowFromWindow` 不存在

**症状：**
```
CS0234: 命名空间"WinRT.Interop"中不存在类型或命名空间名"WindowFromWindow"
```

**原因：**
WinUI 3 的 API 变化，`WindowFromWindow` 方法不可用。

**解决方案：**
使用 `Microsoft.UI.Win32Interop.GetWindowIdFromWindow()` 或简化窗口管理逻辑，移除对窗口句柄的直接操作。

---

### 问题 5：PerformanceCounter 类型转发问题

**症状：**
```
未能在命名空间"System.Diagnostics"中找到类型名"PerformanceCounter"
此类型已转发到程序集"System.Diagnostics.PerformanceCounter"
```

**原因：**
.NET 8 中 `PerformanceCounter` 类型被转发到了独立的程序集。

**解决方案：**
确保 NuGet 包 `System.Diagnostics.PerformanceCounter` 已安装，并清理 bin/obj 目录后重新构建。

---

### 问题 6：ContentDialog 的 `ShowAsync()` 异步问题

**症状：**
```
CS4036: "IAsyncOperation<ContentDialogResult>"不包含"GetAwaiter"的定义
```

**原因：**
WinUI 3 的 `ContentDialog.ShowAsync()` 返回的是 `IAsyncOperation`，需要特殊处理。

**解决方案：**
使用 `try-catch` 包裹 `ShowAsync()` 调用，或使用 `.AsTask()` 转换：

```csharp
try
{
    await dialog.ShowAsync();
}
catch (Exception)
{
    // Ignore dialog errors
}
```

---

### 问题 7：后台线程更新 UI 属性不生效

**症状：**
- 倒计时卡在2分59秒不动
- CPU/GPU/RAM 信息不更新
- 打开后不会自动触发 AI 分析

**原因：**
WinUI 3 的 UI 绑定需要在 UI 线程上才能生效。`OnPropertyChanged` 在后台线程中被调用时，UI 无法接收到属性变更通知。

**解决方案：**
使用 `DispatcherQueue` 将 UI 更新调度到 UI 线程：

```csharp
private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcher;

public DashboardViewModel(...)
{
    _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
}

private void UpdateUI(Action action)
{
    if (_dispatcher != null)
    {
        _dispatcher.TryEnqueue(() => action());
    }
    else
    {
        action();
    }
}

// 使用方式
Task.Run(async () =>
{
    var status = await Task.Run(() => _monitorService.GetStatus());
    UpdateUI(() =>
    {
        CpuUsage = status.CpuUsage;
        GpuUsage = status.GpuUsage;
        RamUsage = status.RamUsage;
    });
});
```

---

### 问题 8：应用启动时不触发 AI 分析

**症状：**
打开应用后需要等待 3 分钟才会进行第一次 AI 分析。

**原因：**
`StartAutoAnalysis()` 只是等待 N 分钟后才触发第一次分析，没有在启动时立即触发。

**解决方案：**
在 `StartMonitoring()` 中添加启动时立即触发的逻辑：

```csharp
public void StartMonitoring()
{
    StartStatusRefresh();
    StartCountdown();
    StartAutoAnalysis();

    // 启动时立即触发一次分析
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000); // 等待1秒让UI加载完成
        await TriggerAnalysisAsync();
    });
}
```

---

## 关键设计决策

### 1. MVVM 模式
- 使用 `INotifyPropertyChanged` 接口手动实现属性绑定
- 不使用 CommunityToolkit.Mvvm 的 `[ObservableProperty]`（与 WinUI 3 不兼容）
- 使用 `IRelayCommand` 和 `AsyncRelayCommand` 实现命令绑定

### 2. 数据绑定
- 使用 `x:Bind`（编译时绑定）提高性能
- `Mode=OneWay` 用于动态数据
- `Mode=TwoWay` 用于输入控件

### 3. 导航架构
- 使用 `NavigationView` + `Frame` 实现页面导航
- 页面类型通过 `Tag` 属性映射
- 支持后退导航

### 4. 服务层
- 使用静态单例模式管理服务
- 服务在 `App.xaml.cs` 中初始化
- 通过 `App.XXX` 访问服务实例

---

## 构建和运行

```bash
# 构建
dotnet build -p:Platform=x64

# 运行
dotnet run -p:Platform=x64

# 清理
dotnet clean -p:Platform=x64
```

**注意：**
- 必须指定 `Platform=x64`（因为使用了 PerformanceCounter）
- 不支持单文件发布（`PublishSingleFile=true` 会导致崩溃）

---

---

### 问题 9：`WinRT.Interop.WindowFromWindow` 不存在

**症状：**
```
CS0234: 命名空间"WinRT.Interop"中不存在类型或命名空间名"WindowFromWindow"
```

**原因：**
WinUI 3 的 API 变化，`WindowFromWindow` 方法在某些版本中不可用。

**解决方案：**
使用 P/Invoke 调用 `GetActiveWindow` 获取窗口句柄：

```csharp
[DllImport("user32.dll")]
private static extern IntPtr GetActiveWindow();

var hwnd = GetActiveWindow();
var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
```

---

## 功能说明

### 系统信息页面

系统信息页面显示以下硬件信息：
- **计算机名**：当前计算机的名称
- **操作系统**：Windows 版本信息
- **CPU**：处理器型号（如 "Intel Core i7-12700K"）
- **GPU**：显卡型号（如 "NVIDIA GeForce RTX 3080"）
- **内存**：总内存大小
- **主板**：主板型号
- **硬盘**：硬盘型号和容量

这些信息通过 WMI（Windows Management Instrumentation）获取，需要安装 `System.Management` NuGet 包。

---

### 问题 10：WinUI 3 Button 不支持 Icon 属性

**症状：**
```
XamlCompiler error WMC0011: Unknown member 'Icon' on element 'Button'
```

**原因：**
WinUI 3 的 `Button` 控件不支持 `Icon` 属性（与 WPF 不同）。

**解决方案：**
使用 `Button.Content` 包含图标和文本：

```xml
<Button>
    <StackPanel Orientation="Horizontal" Spacing="8">
        <FontIcon Glyph="&#xE9F5;" FontSize="14" />
        <TextBlock Text="开始分析" />
    </StackPanel>
</Button>
```

---

## 页面结构

应用采用多页面结构，通过 `NavigationView` + `Frame` 实现导航：

```
Pages/
├── DashboardPage.xaml(.cs)      # 仪表盘（CPU/GPU/RAM、AI分析、统计信息）
├── HardwareInfoPage.xaml(.cs)   # 硬件信息（CPU名称、GPU名称、主板、硬盘等）
├── ProcessListPage.xaml(.cs)    # 进程列表（搜索、刷新、AI查询）
└── SettingsPage.xaml(.cs)       # 设置（API配置、监控间隔）
```

### 导航菜单图标

| 菜单项 | 图标 | Glyph |
|--------|------|-------|
| 仪表盘 | 主页 | `&#xE80F;` |
| 硬件信息 | 电脑 | `&#xE9F5;` |
| 进程列表 | 列表 | `&#xE7F4;` |
| 设置 | 设置 | `&#xE713;` |

### 按钮图标

| 按钮 | 图标 | Glyph |
|------|------|-------|
| 开始分析 | 搜索 | `&#xE9F5;` |
| 刷新 | 刷新 | `&#xE72C;` |
| AI 查询 | 机器人 | `&#xE99A;` |
| 保存设置 | 保存 | `&#xE74E;` |

---

## 新增功能说明

### 硬件信息显示

仪表盘页面的 CPU/GPU/RAM 卡片下方显示当前读取的硬件型号：
- **CPU 卡片**：显示处理器型号（如 "Intel Core i7-12700K"）
- **GPU 卡片**：显示显卡型号（如 "NVIDIA GeForce RTX 3080"）
- **RAM 卡片**：显示内存容量（如 "32.0 GB"）

### 硬件信息首选项

用户可以在设置页面配置显示哪些硬件信息：
- **显示 CPU 型号**：开关控制
- **显示 GPU 型号**：开关控制
- **显示内存详情**：开关控制
- **显示主板型号**：开关控制（默认关闭）
- **GPU 选择**：如果有多个 GPU（集成显卡 + 独立显卡），可以选择显示哪个

配置保存在 `Assets/appsettings.json` 的 `Hardware` 节：
```json
{
  "Hardware": {
    "ShowCpuName": true,
    "ShowGpuName": true,
    "ShowRamDetail": true,
    "ShowMotherboard": false,
    "GpuIndex": 0
  }
}
```

### 倒计时重置

手动触发 AI 分析后，倒计时会自动重置为配置的间隔时间，确保下次自动分析时间准确。

### 页面导航优化

从其他页面返回仪表盘时，不会自动触发 AI 分析，只恢复定时器。只有首次进入仪表盘时才会触发分析。

### GPU 选择下拉框

设置页面的 GPU 选择改为下拉框，自动检测并显示所有可用的 GPU 名称。用户可以直接选择，无需手动输入索引。

### 锐评配置

硬件信息页面新增"锐评配置"按钮，点击后调用 AI 对用户的电脑配置进行猫娘风格的评价。评价内容包括对 CPU、GPU、内存等硬件的吐槽或夸奖。

硬件信息页面的 GPU 现在会显示所有检测到的 GPU，每个 GPU 占一行。使用 `GetGpuNameAllStatic()` 方法返回所有 GPU 名称。

### 问题 11：AIService NullReferenceException

**症状：**
```
System.NullReferenceException: "Object reference not set to an instance of an object."
```

**原因：**
1. `reasoning.GetString()` 被调用了两次，第一次返回 null 时，第二次调用 `.Length` 会抛出异常
2. `config.ApiKey` 可能为 null 或空字符串
3. JSON 解析时未处理缺失的字段

**解决方案：**
1. 修复 `reasoning_content` 处理逻辑，只调用一次 `GetString()`
2. 在方法开头添加 config 和 ApiKey 的 null 检查
3. 使用 `TryGetProperty` 安全获取 JSON 字段
4. 添加 `JsonException`、`HttpRequestException`、`TaskCanceledException` 等异常处理

---

---

### 问题 12：Indexed x:Bind 导致 XAML 编译错误 WMC9999

**症状：**
```
Xaml Internal Error error WMC9999: 未能找到任何适合于指定的区域性或非特定区域性的资源
```

**原因：**
WinUI 3 的 `x:Bind` 不支持索引器语法（如 `AvailableModels[ViewModel.ModelIndex].Description`）。

**解决方案：**
改用中间属性传递数据：
```csharp
// ViewModel 中定义
private string _modelDescription;
public string ModelDescription { get; set; }
// 在 ModelIndex setter 中同步更新
ModelDescription = AvailableModels[value].Description;
```

```xml
<!-- XAML 中使用简单属性绑定 -->
<TextBlock Text="{x:Bind ViewModel.ModelDescription, Mode=OneWay}" />
```

---

### 功能：AI 模型选择与计费说明

#### 支持模型

用户在设置页面可通过 ComboBox 选择模型：

| 选项 | 模型名 | 定价 |
|------|--------|------|
| DeepSeek V4 Flash（推荐） | `deepseek-v4-flash` | 输入 ¥1，输出 ¥2，缓存 ¥0.02（/百万tokens） |
| DeepSeek V4 Pro | `deepseek-v4-pro` | 输入 ¥3，输出 ¥6，缓存 ¥0.025（/百万tokens） |
| 其他兼容模型（自定义） | 用户输入 | 计费估算不准确 |

#### 动态费用计算

`AnalysisResult.Cost` 根据 `ModelName` 自动切换单价：
```csharp
var (inputPrice, outputPrice) = ModelName switch
{
    "deepseek-v4-pro" => (3.00, 6.00),
    _ => (1.00, 2.00)
};
return (PromptTokens * inputPrice + CompletionTokens * outputPrice) / 1_000_000.0;
```

#### 峰谷定价提示

DeepSeek API 预计 2026 年 7 月中旬起采用峰谷定价：
- **高峰时段**：北京时间每日 9:00～12:00 和 14:00～18:00
- **价格**：平日价格的 **2 倍**
- 仪表盘费用显示为平日基础价格估算

---

## 待优化项

1. **主题切换**：ThemeHelper 已实现，但需要在 UI 中添加主题切换控件
2. **错误处理**：需要更完善的全局异常处理
3. **性能监控**：GPU 监控在某些系统上可能返回 0
4. **AI 分析**：需要配置有效的 API 密钥才能使用
5. **进程列表**：可以添加排序和筛选功能
6. **进程列表自动刷新**：可以添加定时刷新功能

---

## 参考资源

- [WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Windows App SDK](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/)

---

## 打包分发

### 概述

项目支持两种打包方式：

1. **独立可执行文件**（推荐用于快速分发）- 用户无需安装 .NET 运行时或证书
2. **MSIX 旁加载包**（推荐用于企业部署）- 支持自动更新，但需要安装证书

### 方式一：独立可执行文件

#### 特点

- ✅ 用户无需安装 .NET 运行时
- ✅ 无需安装证书或启用开发者模式
- ✅ 双击即可运行
- ❌ 文件体积较大（约 315 MB）
- ❌ 无自动更新机制

#### 发布配置文件

项目包含以下独立发布配置文件：

| 文件 | 用途 |
|------|------|
| `Properties/PublishProfiles/win-x64-standalone.pubxml` | x64 架构（推荐） |
| `Properties/PublishProfiles/win-x86-standalone.pubxml` | x86 架构（32位系统） |
| `Properties/PublishProfiles/win-arm64-standalone.pubxml` | ARM64 架构（ARM设备） |

#### 发布步骤

**方法一：使用发布脚本（推荐）**

```cmd
# 双击运行发布脚本
publish-standalone.bat
```

脚本提供图形化选择界面，支持单架构或全架构批量发布。

**方法二：使用命令行**

```powershell
# 发布 x64 版本（推荐）
dotnet publish -p:Platform=x64 -p:Configuration=Release -p:PublishProfile=win-x64-standalone.pubxml

# 发布 x86 版本（32位系统）
dotnet publish -p:Platform=x86 -p:Configuration=Release -p:PublishProfile=win-x86-standalone.pubxml

# 发布 ARM64 版本（ARM 设备）
dotnet publish -p:Platform=ARM64 -p:Configuration=Release -p:PublishProfile=win-arm64-standalone.pubxml
```

**方法三：使用 Visual Studio 2026**

1. 在解决方案资源管理器中右键项目
2. 选择 **发布**
3. 选择目标：**文件夹**
4. 位置：`bin\publish-standalone\`
5. 配置：**Release - x64**
6. 点击 **发布**

#### 输出文件

发布完成后，在 `bin\publish-standalone\` 目录会生成：

```
meow-ai-tskmgr-ui3.exe  (约 315 MB，含完整 .NET 运行时)
meow-ai-tskmgr-ui3.pdb  (调试符号，可删除)
```

**测试结果**（2026-07-05）：
- x64 Release 发布成功
- 文件大小：330,276,963 字节（约 315 MB）
- 包含完整的 .NET 8 运行时和 Windows App SDK

#### 用户使用说明

**系统要求**
- Windows 10 版本 1809 (build 17763) 或更高版本
- 建议 4GB 以上内存

**安装步骤**
1. 将 `meow-ai-tskmgr-ui3.exe` 复制到任意目录
2. 双击运行
3. 首次运行时，Windows 可能显示安全警告：
   - 点击"更多信息"
   - 点击"仍要运行"

**配置文件位置**
应用配置保存在：`%APPDATA%\meow-ai-tskmgr-ui3\`

---

### 方式二：MSIX 旁加载包

#### 特点

- ✅ 文件体积较小
- ✅ 支持自动更新
- ✅ 标准的 Windows 安装体验
- ❌ 需要安装证书
- ❌ 需要启用开发者模式或安装证书

#### 打包步骤

**1. 创建自签名证书（首次）**

```powershell
# 创建证书
$cert = New-SelfSignedCertificate `
  -Type CodeSigningCert `
  -Subject "CN=Meow AI" `
  -CertStoreLocation Cert:\CurrentUser\My `
  -NotAfter (Get-Date).AddYears(5)

# 导出 .pfx 文件（用于签名）
$password = ConvertTo-SecureString -String "YourPassword" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "MeowAI.pfx" -Password $password

# 导出 .cer 文件（用于目标机器安装）
Export-Certificate -Cert $cert -FilePath "MeowAI.cer"
```

**2. 使用 Visual Studio 2026 打包**

1. 在解决方案资源管理器中右键项目
2. 选择 **发布** → **创建应用程序包**
3. 选择 **旁加载**
4. 选择证书文件 `MeowAI.pfx` 并输入密码
5. 选择目标架构：**x64**
6. 点击 **创建**

**3. 输出文件**

打包完成后，在 `AppPackages` 目录会生成：

```
meow-ai-tskmgr-ui3_1.0.0.0_x64_Debug.msix
meow-ai-tskmgr-ui3_1.0.0.0_x64_Debug_Test/
  ├── Install.ps1
  ├── Add-AppDevPackage.ps1
  ├── Dependencies/
  └── meow-ai-tskmgr-ui3_1.0.0.0_x64_Debug.appxsym
```

#### 用户使用说明

**方法一：使用 PowerShell 脚本（推荐）**

1. 将整个 `meow-ai-tskmgr-ui3_1.0.0.0_x64_Debug_Test` 文件夹复制到目标电脑
2. 右键 `Install.ps1` → **使用 PowerShell 运行**
3. 按照提示完成安装

**方法二：手动安装证书**

1. 将 `MeowAI.cer` 复制到目标电脑
2. 双击证书文件 → **安装证书**
3. 选择 **本地计算机** → **下一步**
4. 选择 **将所有的证书都放入下列存储** → **浏览**
5. 选择 **受信任的根证书颁发机构** → **确定** → **下一步** → **完成**
6. 双击 `.msix` 文件安装应用

---

### 常见问题

#### Q1: 独立发布的 .exe 文件太大怎么办？

可以启用更激进的裁剪：

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>full</TrimMode>
</PropertyGroup>
```

注意：这可能导致某些反射功能失效，需要充分测试。

#### Q2: 首次运行被 Windows Defender 阻止？

1. 打开 Windows 安全中心
2. 病毒和威胁防护 → 管理设置
3. 排除项 → 添加排除项 → 选择 .exe 文件

#### Q3: 如何支持自动更新？

独立发布不支持自动更新。如需自动更新功能，请使用 MSIX 旁加载方式，并配置更新服务器。

#### Q4: 杀毒软件误报怎么办？

独立发布的 .exe 文件可能被杀毒软件误报，建议：
1. 添加到杀毒软件白名单
2. 使用正式的代码签名证书签名
3. 向杀毒软件厂商提交误报报告

---

### 技术要点

#### WinUI 3 独立发布要求

1. **必须启用 `EnableMsixTooling=true`**
   - WinUI 3 资源编译依赖 MSIX 工具
   - 即使发布为独立可执行文件，也需要此配置

2. **必须启用 `WindowsAppSDKSelfContained=true`**
   - 包含完整的 Windows App SDK 运行时
   - 确保应用在无开发环境的机器上运行

3. **推荐启用 `PublishTrimmed=true`**
   - 裁剪未使用的程序集
   - 显著减小文件体积

4. **推荐启用 `PublishReadyToRun=true`**
   - 预编译为本机代码
   - 提高应用启动速度

#### 文件体积说明

独立发布包含以下组件：
- .NET 8 运行时（约 80-100 MB）
- Windows App SDK 运行时（约 150-200 MB）
- 应用程序代码和资源（约 10-20 MB）

总计约 300-330 MB，这是 WinUI 3 独立发布的正常大小。

#### 配置文件说明

项目包含以下发布配置文件：

| 文件名 | 用途 | 说明 |
|--------|------|------|
| `win-x64.pubxml` | MSIX 打包 | 默认配置，用于 Visual Studio 打包 |
| `win-x64-standalone.pubxml` | 独立发布 | 生成单个 .exe 文件 |
| `win-x86.pubxml` | MSIX 打包 (x86) | 32 位系统 |
| `win-arm64.pubxml` | MSIX 打包 (ARM64) | ARM 设备 |

---

### 最佳实践

#### 开发阶段
- 使用 `dotnet run -p:Platform=x64` 运行和调试
- 保持 MSIX 工具启用，方便测试打包功能

#### 发布阶段
1. 更新版本号：修改 `Package.appxmanifest` 中的 `Version`
2. 更新发布说明：在 `Properties/PublishProfiles/` 中添加注释
3. 测试发布：在干净的 Windows 虚拟机中测试安装
4. 签名：使用正式的代码签名证书（非自签名）

#### 分发建议
- **个人使用**：独立可执行文件
- **小团队**：MSIX 旁加载 + 自签名证书
- **企业部署**：MSIX + 企业证书 + Intune/SCCM
- **公开发布**：Microsoft Store 或正式代码签名证书

---

### 相关文档

- `docs/packaging-guide.md` - 完整打包指南
- `docs/standalone-publish-summary.md` - 独立发布配置说明

---

## 签名与信任

### 为什么需要签名？

Windows SmartScreen 会检查应用的签名状态：
- **无签名**：显示"Windows 已保护你的电脑"警告
- **自签名证书**：显示"未知发布者"警告
- **EV 代码签名证书**：几乎无警告

### 为什么别人的 msixbundle 可以"无视风险"安装？

| 类型 | 来源 | 信任度 | 用户体验 |
|------|------|--------|----------|
| **自签名证书** | 你自己创建 | ❌ 最低 | 显示"未知发布者"警告 |
| **普通代码签名证书** | CA 机构签发 | ⚠️ 中等 | 显示警告，但可点击运行 |
| **EV 代码签名证书** | CA 机构严格审核 | ✅ 最高 | 几乎无警告 |
| **Microsoft Store** | Microsoft 签名 | ✅ 最高 | 完全信任 |

**常见情况分析：**

1. **使用 EV 代码签名证书**
   - EV（Extended Validation）证书需要提交公司注册证明、电话验证等
   - 价格较贵（约 $200-500/年）
   - Windows SmartScreen 对 EV 签名的应用**立即信任**

2. **积累了足够的"声誉"**
   - Windows 会记录应用的下载和安装次数
   - 当足够多的用户安装且没有报告问题时，SmartScreen 会逐渐信任该应用
   - 需要大量用户安装（通常数千次以上）

3. **企业内部部署**
   - 企业使用内部 CA 签发的证书
   - 通过组策略分发证书到所有域内机器
   - 域内机器自动信任该证书

4. **用户已安装证书**
   - 如果用户之前已经安装了你的自签名证书
   - 后续安装同一证书签名的应用就不会显示警告

### 如何消除警告？

#### 方案1：购买 EV 代码签名证书（推荐用于公开发布）

**优点：**
- SmartScreen 立即信任
- 用户体验最好
- 专业形象

**缺点：**
- 价格较贵（$200-500/年）
- 需要公司资质

**提供商：**
- DigiCert
- Sectigo (Comodo)
- GlobalSign
- GoDaddy

#### 方案2：自签名证书 + 手动安装（推荐用于内部测试）

**步骤：**
1. 创建自签名证书
2. 安装证书到目标机器的"受信任的根证书颁发机构"
3. 使用证书签名应用
4. 分发 .msix + .cer 文件

**优点：**
- 免费
- 适合小规模分发

**缺点：**
- 需要用户手动安装证书
- 不适合公开发布

#### 方案3：积累声誉（不推荐，需要大量用户）

**原理：**
- Windows 会记录应用的下载和安装次数
- 当足够多的用户安装且没有报告问题时，SmartScreen 会逐渐信任该应用

**要求：**
- 需要数千次安装且无安全报告
- 耗时较长

### 证书文件管理

**⚠️ 重要：.pfx 文件不要上传到 git**

- .pfx 包含私钥，泄露会导致他人伪造你的签名
- 只上传 .cer 文件（公钥证书）
- 本地保存 .pfx，用于签名

**.gitignore 配置：**

```gitignore
# 证书文件（包含私钥，不要上传）
*.pfx
*.p12

# 发布输出
bin/publish-*/
AppPackages/
```

### 分发给用户时

1. 提供 .msix 安装包（已签名）
2. 提供 .cer 证书文件
3. 提供安装说明：
   - 双击 .cer → 安装证书 → 选择"受信任的根证书颁发机构"
   - 双击 .msix 安装

---

## 裁剪配置说明

### 为什么禁用裁剪？

**问题：** 启用 `PublishTrimmed=true` 会导致以下程序集被移除：
- `System.Management`（用于 WMI 获取硬件信息）
- `System.Diagnostics.PerformanceCounter`（用于 CPU/GPU/RAM 监控）

**症状：** 独立发布的 .exe 文件无法识别 CPU、GPU、RAM 等信息。

**解决方案：** 禁用裁剪（`PublishTrimmed=false`），确保所有程序集保留。

### 文件体积影响

| 配置 | 文件大小 | 说明 |
|------|----------|------|
| `PublishTrimmed=true` | ~250 MB | 裁剪未使用程序集，但可能移除关键功能 |
| `PublishTrimmed=false` | ~330 MB | 保留所有程序集，确保功能正常 |

**结论：** 对于本项目，建议禁用裁剪，因为 WMI 和 PerformanceCounter 是核心功能。

### 最佳实践

1. **开发阶段**：使用 `dotnet run` 运行，无需裁剪
2. **发布阶段**：禁用裁剪，确保功能正常
3. **优化体积**：使用 ReadyToRun 编译提高启动速度

---

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0.0.0 | 2026-07-05 | 初始版本 |
| 1.0.0.1 | 2026-07-05 | 禁用裁剪，修复 CPU/GPU/RAM 识别问题 |
