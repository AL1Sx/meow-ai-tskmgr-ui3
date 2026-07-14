using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using meow_ai_tskmgr_ui3.Services;
using Microsoft.UI.Xaml.Controls;

namespace meow_ai_tskmgr_ui3.ViewModels;

public partial class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;

    private string _apiEndpoint;
    private string _apiKey;
    private string _model;
    private int _analysisInterval;
    private bool _showCpuName;
    private bool _showGpuName;
    private bool _showRamDetail;
    private bool _showMotherboard;
    private int _gpuIndex;

    public string ApiEndpoint
    {
        get => _apiEndpoint;
        set { _apiEndpoint = value; OnPropertyChanged(); }
    }

    public string ApiKey
    {
        get => _apiKey;
        set { _apiKey = value; OnPropertyChanged(); }
    }

    public string Model
    {
        get => _model;
        set { _model = value; OnPropertyChanged(); }
    }

    public int AnalysisInterval
    {
        get => _analysisInterval;
        set { _analysisInterval = value; OnPropertyChanged(); }
    }

    public bool ShowCpuName
    {
        get => _showCpuName;
        set { _showCpuName = value; OnPropertyChanged(); }
    }

    public bool ShowGpuName
    {
        get => _showGpuName;
        set { _showGpuName = value; OnPropertyChanged(); }
    }

    public bool ShowRamDetail
    {
        get => _showRamDetail;
        set { _showRamDetail = value; OnPropertyChanged(); }
    }

    public bool ShowMotherboard
    {
        get => _showMotherboard;
        set { _showMotherboard = value; OnPropertyChanged(); }
    }

    public int GpuIndex
    {
        get => _gpuIndex;
        set { _gpuIndex = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> AvailableGpus { get; } = new();

    private IRelayCommand? _saveSettingsCommand;
    public IRelayCommand SaveSettingsCommand => _saveSettingsCommand ??= new AsyncRelayCommand(SaveSettingsAsync);

    public SettingsViewModel(ConfigService configService)
    {
        _configService = configService;
        _apiEndpoint = _configService.Config.Api.Endpoint;
        _apiKey = _configService.Config.Api.ApiKey;
        _model = _configService.Config.Api.Model;
        _analysisInterval = _configService.Config.Monitor.AnalysisIntervalMinutes;
        _showCpuName = _configService.Config.Hardware.ShowCpuName;
        _showGpuName = _configService.Config.Hardware.ShowGpuName;
        _showRamDetail = _configService.Config.Hardware.ShowRamDetail;
        _showMotherboard = _configService.Config.Hardware.ShowMotherboard;
        _gpuIndex = _configService.Config.Hardware.GpuIndex;

        // 加载 GPU 列表
        LoadGpuList();
    }

    private void LoadGpuList()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    AvailableGpus.Add(name);
                }
            }
        }
        catch { }

        // 如果没有检测到 GPU，添加默认项
        if (AvailableGpus.Count == 0)
        {
            AvailableGpus.Add("未检测到 GPU");
        }

        // 确保 GpuIndex 在有效范围内
        if (_gpuIndex >= AvailableGpus.Count)
        {
            _gpuIndex = 0;
        }
    }

    private async Task SaveSettingsAsync()
    {
        _configService.UpdateConfig(config =>
        {
            config.Api.Endpoint = ApiEndpoint;
            config.Api.ApiKey = ApiKey;
            config.Api.Model = Model;
            config.Monitor.AnalysisIntervalMinutes = AnalysisInterval;
            config.Hardware.ShowCpuName = ShowCpuName;
            config.Hardware.ShowGpuName = ShowGpuName;
            config.Hardware.ShowRamDetail = ShowRamDetail;
            config.Hardware.ShowMotherboard = ShowMotherboard;
            config.Hardware.GpuIndex = GpuIndex;
        });

        // 刷新仪表盘的硬件信息
        App.DashboardViewModel?.RefreshHardwareInfo();

        var dialog = new ContentDialog
        {
            Title = "保存成功",
            Content = "设置已保存，新的配置将在下次分析时生效。",
            CloseButtonText = "确定",
            XamlRoot = App.MainWindow?.Content.XamlRoot
        };

        try
        {
            await dialog.ShowAsync();
        }
        catch (Exception)
        {
            // Ignore dialog errors
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
