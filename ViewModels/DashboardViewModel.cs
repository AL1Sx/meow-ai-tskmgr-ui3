using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using meow_ai_tskmgr_ui3.Models;
using meow_ai_tskmgr_ui3.Services;

namespace meow_ai_tskmgr_ui3.ViewModels;

public partial class DashboardViewModel : INotifyPropertyChanged
{
    private readonly SystemMonitorService _monitorService;
    private readonly AIService _aiService;
    private readonly ConfigService _configService;
    private readonly Microsoft.UI.Dispatching.DispatcherQueue? _dispatcher;

    private CancellationTokenSource? _statusCts;
    private CancellationTokenSource? _countdownCts;
    private CancellationTokenSource? _autoAnalysisCts;

    private float _cpuUsage;
    private float _gpuUsage;
    private float _ramUsage;
    private string _ramInfo = "";
    private string _cpuName = "";
    private string _gpuName = "";
    private string _ramDetail = "";
    private string _aiContent = "点击「开始分析」或等待自动分析~";
    private bool _isAnalyzing;
    private string _analysisStatus = "";
    private string _lastAnalysisTime = "--:--:--";
    private string _analysisDuration = "--";
    private string _nextAnalysisTime = "--";
    private string _tokenInfo = "--";
    private string _costInfo = "--";
    private string _estimate24h = "--";
    private int _countdownSeconds;
    private AnalysisResult? _lastResult;

    public float CpuUsage
    {
        get => _cpuUsage;
        set { _cpuUsage = value; OnPropertyChanged(); }
    }

    public float GpuUsage
    {
        get => _gpuUsage;
        set { _gpuUsage = value; OnPropertyChanged(); }
    }

    public float RamUsage
    {
        get => _ramUsage;
        set { _ramUsage = value; OnPropertyChanged(); }
    }

    public string RamInfo
    {
        get => _ramInfo;
        set { _ramInfo = value; OnPropertyChanged(); }
    }

    public string CpuName
    {
        get => _cpuName;
        set { _cpuName = value; OnPropertyChanged(); }
    }

    public string GpuName
    {
        get => _gpuName;
        set { _gpuName = value; OnPropertyChanged(); }
    }

    public string RamDetail
    {
        get => _ramDetail;
        set { _ramDetail = value; OnPropertyChanged(); }
    }

    public string AiContent
    {
        get => _aiContent;
        set { _aiContent = value; OnPropertyChanged(); }
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set { _isAnalyzing = value; OnPropertyChanged(); }
    }

    public string AnalysisStatus
    {
        get => _analysisStatus;
        set { _analysisStatus = value; OnPropertyChanged(); }
    }

    public string LastAnalysisTime
    {
        get => _lastAnalysisTime;
        set { _lastAnalysisTime = value; OnPropertyChanged(); }
    }

    public string AnalysisDuration
    {
        get => _analysisDuration;
        set { _analysisDuration = value; OnPropertyChanged(); }
    }

    public string NextAnalysisTime
    {
        get => _nextAnalysisTime;
        set { _nextAnalysisTime = value; OnPropertyChanged(); }
    }

    public string TokenInfo
    {
        get => _tokenInfo;
        set { _tokenInfo = value; OnPropertyChanged(); }
    }

    public string CostInfo
    {
        get => _costInfo;
        set { _costInfo = value; OnPropertyChanged(); }
    }

    public string Estimate24h
    {
        get => _estimate24h;
        set { _estimate24h = value; OnPropertyChanged(); }
    }

    public int CountdownSeconds
    {
        get => _countdownSeconds;
        set { _countdownSeconds = value; OnPropertyChanged(); }
    }

    public AnalysisResult? LastResult
    {
        get => _lastResult;
        set { _lastResult = value; OnPropertyChanged(); }
    }

    private IRelayCommand? _triggerAnalysisCommand;
    public IRelayCommand TriggerAnalysisCommand => _triggerAnalysisCommand ??= new AsyncRelayCommand(TriggerAnalysisAsync);

    public DashboardViewModel(SystemMonitorService monitorService, AIService aiService, ConfigService configService)
    {
        _monitorService = monitorService;
        _aiService = aiService;
        _configService = configService;
        _dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        
        // 加载硬件信息
        LoadHardwareInfo();
    }

    private void LoadHardwareInfo()
    {
        var hardwareConfig = _configService.Config.Hardware;
        
        if (hardwareConfig.ShowCpuName)
            CpuName = SystemInfoViewModel.GetCpuNameStatic();
        
        if (hardwareConfig.ShowGpuName)
            GpuName = SystemInfoViewModel.GetGpuNameStatic(hardwareConfig.GpuIndex);
        
        if (hardwareConfig.ShowRamDetail)
            RamDetail = SystemInfoViewModel.GetRamDetailStatic();
    }

    public void RefreshHardwareInfo()
    {
        LoadHardwareInfo();
    }

    public void StartMonitoring()
    {
        StartStatusRefresh();
        StartCountdown();
        StartAutoAnalysis();

        // 启动时立即触发一次分析
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            await TriggerAnalysisAsync();
        });
    }

    public void ResumeMonitoring()
    {
        // 只恢复定时器，不触发分析
        StartStatusRefresh();
        StartCountdown();
        StartAutoAnalysis();
    }

    public void StopMonitoring()
    {
        _statusCts?.Cancel();
        _countdownCts?.Cancel();
        _autoAnalysisCts?.Cancel();
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

    private void StartStatusRefresh()
    {
        _statusCts?.Cancel();
        _statusCts = new CancellationTokenSource();
        var token = _statusCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var status = await Task.Run(() => _monitorService.GetStatus(), token);
                    UpdateUI(() =>
                    {
                        CpuUsage = status.CpuUsage;
                        GpuUsage = status.GpuUsage;
                        RamUsage = status.RamUsage;
                        RamInfo = $"{status.UsedRamMB} / {status.TotalRamMB} MB";
                    });
                }
                catch { }
                await Task.Delay(2000, token);
            }
        }, token);
    }

    private void StartCountdown()
    {
        _countdownCts?.Cancel();
        _countdownCts = new CancellationTokenSource();
        var token = _countdownCts.Token;

        var intervalMinutes = _configService.Config.Monitor.AnalysisIntervalMinutes;
        CountdownSeconds = intervalMinutes * 60;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UpdateUI(() =>
                    {
                        CountdownSeconds = Math.Max(0, CountdownSeconds - 1);
                        NextAnalysisTime = CountdownSeconds > 0
                            ? $"{CountdownSeconds / 60:D2}:{CountdownSeconds % 60:D2}"
                            : "分析中...";
                    });

                    if (CountdownSeconds <= 0)
                    {
                        CountdownSeconds = intervalMinutes * 60;
                    }
                }
                catch { }
                await Task.Delay(1000, token);
            }
        }, token);
    }

    private void StartAutoAnalysis()
    {
        _autoAnalysisCts?.Cancel();
        _autoAnalysisCts = new CancellationTokenSource();
        var token = _autoAnalysisCts.Token;

        var intervalMinutes = _configService.Config.Monitor.AnalysisIntervalMinutes;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), token);
                    if (!token.IsCancellationRequested)
                    {
                        await TriggerAnalysisAsync();
                    }
                }
                catch { }
            }
        }, token);
    }

    private void ResetCountdown()
    {
        var intervalMinutes = _configService.Config.Monitor.AnalysisIntervalMinutes;
        CountdownSeconds = intervalMinutes * 60;
    }

    public async Task TriggerAnalysisAsync()
    {
        if (IsAnalyzing) return;

        UpdateUI(() =>
        {
            IsAnalyzing = true;
            AnalysisStatus = "获取系统状态中...";
        });

        try
        {
            var status = await Task.Run(() => _monitorService.GetStatus());

            UpdateUI(() =>
            {
                AnalysisStatus = "调用 AI 分析中...";
            });

            var result = await _aiService.AnalyzeSystemAsync(status);

            UpdateUI(() =>
            {
                LastResult = result;
                AiContent = result.Content;
                LastAnalysisTime = result.Timestamp.ToString("HH:mm:ss");
                AnalysisDuration = $"{result.DurationSeconds:F1}s";
                TokenInfo = $"{result.TotalTokens}";
                CostInfo = $"¥{result.Cost:F4}";
                Estimate24h = $"¥{result.Cost * 480:F2}";
            });
        }
        catch (Exception ex)
        {
            UpdateUI(() =>
            {
                AiContent = $"分析失败: {ex.Message}";
            });
        }
        finally
        {
            UpdateUI(() =>
            {
                IsAnalyzing = false;
                AnalysisStatus = "";
                ResetCountdown();  // 分析完成后重置倒计时
            });
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
