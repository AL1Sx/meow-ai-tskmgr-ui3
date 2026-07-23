using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using meow_ai_tskmgr_ui3.Helpers;
using meow_ai_tskmgr_ui3.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace meow_ai_tskmgr_ui3.ViewModels;

public class ModelOption
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
}

public partial class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;

    private const int CustomModelIndex = 2;

    private string _apiEndpoint;
    private string _apiKey;
    private string _model;
    private int _modelIndex;
    private string _modelDescription;
    private string _customModelName;
    private bool _isCustomModel;
    private int _analysisInterval;
    private bool _showCpuName;
    private bool _showGpuName;
    private bool _showRamDetail;
    private bool _showMotherboard;
    private int _gpuIndex;
    private int _selectedThemeIndex;

    public string[] ThemeOptions { get; } = { "跟随系统", "浅色", "深色" };

    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            if (_selectedThemeIndex != value)
            {
                _selectedThemeIndex = value;
                OnPropertyChanged();
                var theme = value switch
                {
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
                ThemeHelper.CurrentTheme = theme;
            }
        }
    }

    public ObservableCollection<ModelOption> AvailableModels { get; } = new()
    {
        new ModelOption
        {
            Name = "deepseek-v4-flash",
            DisplayName = "DeepSeek V4 Flash（推荐）",
            Description = ""
        },
        new ModelOption
        {
            Name = "deepseek-v4-pro",
            DisplayName = "DeepSeek V4 Pro",
            Description = ""
        },
        new ModelOption
        {
            Name = "__custom__",
            DisplayName = "其他兼容模型（自定义）",
            Description = ""
        }
    };

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

    public string ModelDescription
    {
        get => _modelDescription;
        set { _modelDescription = value; OnPropertyChanged(); }
    }

    public string CustomModelName
    {
        get => _customModelName;
        set
        {
            _customModelName = value;
            if (_isCustomModel) Model = value;
            OnPropertyChanged();
        }
    }

    public bool IsCustomModel
    {
        get => _isCustomModel;
        set { _isCustomModel = value; OnPropertyChanged(); }
    }

    public int ModelIndex
    {
        get => _modelIndex;
        set
        {
            if (_modelIndex != value && value >= 0 && value < AvailableModels.Count)
            {
                _modelIndex = value;
                IsCustomModel = value == CustomModelIndex;
                Model = IsCustomModel ? _customModelName : AvailableModels[value].Name;
                ModelDescription = AvailableModels[value].Description;
                OnPropertyChanged();
            }
        }
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
        _customModelName = _model;
        _modelIndex = FindModelIndex(_model);
        _isCustomModel = _modelIndex == CustomModelIndex;
        _modelDescription = AvailableModels[_modelIndex].Description;
        _analysisInterval = _configService.Config.Monitor.AnalysisIntervalMinutes;
        _showCpuName = _configService.Config.Hardware.ShowCpuName;
        _showGpuName = _configService.Config.Hardware.ShowGpuName;
        _showRamDetail = _configService.Config.Hardware.ShowRamDetail;
        _showMotherboard = _configService.Config.Hardware.ShowMotherboard;
        _gpuIndex = _configService.Config.Hardware.GpuIndex;
        _selectedThemeIndex = ThemeHelper.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };

        // 异步加载 GPU 列表
        _ = LoadGpuListAsync();
    }

    private int FindModelIndex(string modelName)
    {
        for (int i = 0; i < AvailableModels.Count; i++)
        {
            if (string.Equals(AvailableModels[i].Name, modelName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        // 不匹配任何预设模型 → 视为自定义
        return CustomModelIndex;
    }

    private async Task LoadGpuListAsync()
    {
        await Task.Run(() =>
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GPU list load error: {ex.Message}");
            }

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
        });
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
