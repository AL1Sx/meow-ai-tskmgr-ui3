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
  SettingsPage      → API config + monitor interval
ViewModels/        → One VM per page, INotifyPropertyChanged (manual)
Services/          → SystemMonitorService, AIService, ConfigService
Models/            → SystemStatus, ProcessInfo, AnalysisResult, AppConfig
Converters.cs      → Root namespace converters (not in subfolder)
Styles/Cards.xaml   → Shared card styles + PercentageToWidthConverter
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

### 5. Window handle via P/Invoke

`WinRT.Interop.WindowFromWindow` does not exist. Use:
```csharp
[DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
var hwnd = GetActiveWindow();
var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
```

### 6. ContentDialog.ShowAsync() needs try-catch

Returns `IAsyncOperation`, not `Task`. Wrap in try-catch to avoid unhandled exceptions.

## Service Lifecycle

```
App(ConfigService) → AIService(ConfigService) → SystemMonitorService
All accessed via App.ConfigService, App.AI, App.Monitor statics.
ViewModels created in App.InitializeServices(), accessed via App.DashboardViewModel etc.
```

## Key Dependencies

- `Microsoft.WindowsAppSDK` 2.2.0
- `System.Diagnostics.PerformanceCounter` 8.0.0 (CPU/GPU/RAM monitoring)
- `System.Management` 8.0.0 (WMI hardware info)
- `CommunityToolkit.Mvvm` 8.4.0 (IRelayCommand, AsyncRelayCommand only)
- `PInvoke.User32` 0.7.124 (window handle)

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
