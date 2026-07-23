# AGENTS.md

## Project Overview

WinUI 3 desktop app: AI-powered system monitor with cat-girl personality.
- Framework: .NET 8, WindowsAppSDK 2.2, WinUI 3
- Architecture: MVVM with `NavigationView` + `Frame` page navigation
- Namespace: `meow_ai_tskmgr_ui3`

## Build & Run

```bash
# Always use x64 platform (PerformanceCounter requires it)
dotnet build -p:Platform=x64
dotnet run -p:Platform=x64

# Clean
dotnet clean -p:Platform=x64
```

**Critical**: Never use `PublishSingleFile=true` — causes `0xc000027b` crash with WinUI 3.

## Architecture

```
App.xaml.cs → Initializes services (static singletons) + converters
MainWindow.xaml → NavigationView shell with Frame
Pages/
  DashboardPage     → CPU/GPU/RAM cards, AI analysis, stats
  HardwareInfoPage  → WMI hardware info (CPU/GPU/mobo/disk names)
  ProcessListPage   → Process list with search + AI query
  SettingsPage      → API config + monitor interval + theme toggle
ViewModels/
  DashboardViewModel   → DashboardPage
  HardwareInfoViewModel → HardwareInfoPage (hardware info + config critique)
  ProcessListViewModel → ProcessListPage (process list + search + AI query)
  SettingsViewModel    → SettingsPage
Services/          → SystemMonitorService, AIService, ConfigService
Helpers/           → ThemeHelper, WindowHelper, SettingsHelper
Models/            → SystemStatus, ProcessInfo, AnalysisResult, AppConfig
Converters.cs      → Root namespace converters (not in subfolder)
Styles/Cards.xaml   → Shared card styles + FloatToScaleConverter
```

## WinUI 3 Quirks (Hard-Won Lessons)

### 1. Never use `[ObservableProperty]` from CommunityToolkit.Mvvm

WinUI 3 XAML compiler cannot see source-generated properties via `x:Bind`.
Always implement `INotifyPropertyChanged` manually with `[CallerMemberName]`.

### 2. Converters must be in root namespace, initialized in OnLaunched

XAML compiler cannot resolve converters in sub-namespaces (`using:meow_ai_tskmgr_ui3.Converters` fails).
Converters live in `Converters.cs` at root namespace. They are added to `Resources` in `App.OnLaunched()` — never in the constructor.

### 3. UI updates from background threads require DispatcherQueue

`OnPropertyChanged` called from background threads does not trigger UI updates.
ViewModels capture `DispatcherQueue.GetForCurrentThread()` in constructor and dispatch via `_dispatcher.TryEnqueue()`.

### 4. WinUI 3 Button has no Icon property

Use `<StackPanel Orientation="Horizontal"><FontIcon/><TextBlock/></StackPanel>` inside `Button.Content`.

### 5. Window handle via WinRT.Interop (not GetActiveWindow)

`WinRT.Interop.WindowFromWindow` does not exist. Use `WindowNative.GetWindowHandle(this)`
instead of `GetActiveWindow()` (which has a race condition if another window has focus):
```csharp
var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
```

### 6. ContentDialog.ShowAsync() needs try-catch

Returns `IAsyncOperation`, not `Task`. Wrap in try-catch to avoid unhandled exceptions.

### 7. Progress bar width must not use fixed-width converter

`Border.Width` with `PercentageToWidthConverter(MaxWidth=120)` breaks on window resize.
Use `ScaleTransform(CenterX=0, ScaleX=Progress/100)` on a `HorizontalAlignment="Stretch"` Border instead.

### 8. NavigationView must have a Header to prevent hamburger overlap

Without `NavigationView.Header`, the pane toggle button in minimal mode overlaps the Frame content.
Always set `<NavigationView.Header><Grid Height="48" /></NavigationView.Header>` to reserve layout space.

### 9. Window title bar does not follow app RequestedTheme

`RequestedTheme` only affects client area. Update `AppWindow.TitleBar` button colors manually
via `ThemeHelper.UpdateTitleBarTheme()`. The `ThemeHelper` handles this automatically.

**Critical**: Only set `Button*` properties (`ButtonForegroundColor`, `ButtonBackgroundColor` etc.).
Never set `ForegroundColor` or `BackgroundColor` — those activate full custom title bar mode
and break system theme tracking. They are only valid with `ExtendsContentIntoTitleBar = true`.

### 10. MicaBackdrop follows system theme, not app RequestedTheme

`MicaBackdrop` uses the Windows system light/dark mode regardless of the app's `RequestedTheme`.
If the system is in dark mode but the app switches to light mode via `RequestedTheme`,
the Mica backdrop remains dark. Set theme to "跟随系统" in Settings for consistent appearance.

### 11. Process.GetProcesses() throws Win32Exception on system processes

System processes (PID 0/4 etc.) throw on `ProcessName`/`WorkingSet64`/`TotalProcessorTime`.
Always guard individual property access with try-catch and skip inaccessible PIDs early.

### 12. Unpackaged apps must set `<WindowsPackageType>None</WindowsPackageType>`

Without this property, the Windows App SDK Foundation auto-initializer calls
`DeploymentManager.Initialize()` on startup, which crashes with
`COMException (0x80040154)` — the `DeploymentInitializeOptions` COM class is
not registered for unpackaged apps. Add to `csproj`:

```xml
<WindowsPackageType>None</WindowsPackageType>
```

Also set `<EnableMsixTooling>true</EnableMsixTooling>` if you want MSIX
publishing to still work — the two properties are independent.

## Service Lifecycle

```
App(ConfigService) → AIService(ConfigService) → SystemMonitorService
All accessed via App.ConfigService, App.AI, App.Monitor statics.
ViewModels created in App.InitializeServices(), accessed via App.DashboardViewModel etc.
ThemeHelper initialized in App.OnLaunched() after converter setup.
WindowHelper.TrackWindow() called for each Window to support theme propagation.
```

## Key Dependencies

- `Microsoft.WindowsAppSDK` 2.2.0
- `System.Diagnostics.PerformanceCounter` 8.0.0 (CPU/GPU/RAM monitoring)
- `System.Management` 8.0.0 (WMI hardware info)
- `CommunityToolkit.Mvvm` 8.4.0 (IRelayCommand, AsyncRelayCommand only)

## AI Configuration

Config file: `Assets/appsettings.json`
```json
{
  "Api": { "Endpoint": "https://api.deepseek.com", "ApiKey": "", "Model": "deepseek-chat" },
  "Monitor": { "AnalysisIntervalMinutes": 3 }
}
```

Uses OpenAI-compatible `/v1/chat/completions` format. System prompt requests cat-girl persona.

## Testing

No automated tests. Manual verification:
1. `dotnet run -p:Platform=x64` — window appears at 630x980
2. Dashboard shows CPU/GPU/RAM percentages updating every 2s
3. AI analysis triggers on startup + every N minutes
4. Hardware Info page shows correct CPU/GPU/motherboard/disk names
5. Process List page shows processes, search filters work
6. Settings page saves config to appsettings.json
