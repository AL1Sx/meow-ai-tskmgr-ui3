using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using meow_ai_tskmgr_ui3.Models;
using meow_ai_tskmgr_ui3.Services;

namespace meow_ai_tskmgr_ui3.ViewModels;

public partial class ProcessListViewModel : INotifyPropertyChanged
{
    private readonly SystemMonitorService _monitorService;
    private readonly AIService _aiService;

    private int _processCount;
    private string _searchQuery = "";
    private string _selectedProcessInfo = "";
    private bool _isLoading;

    public int ProcessCount
    {
        get => _processCount;
        set { _processCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProcessCountText)); }
    }

    public string ProcessCountText => $"共 {ProcessCount} 个进程";

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value;
            OnPropertyChanged();
            OnSearchQueryChanged(value);
        }
    }

    public string SelectedProcessInfo
    {
        get => _selectedProcessInfo;
        set { _selectedProcessInfo = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ProcessInfo> Processes { get; } = new();

    private IRelayCommand? _loadProcessesCommand;
    public IRelayCommand LoadProcessesCommand => _loadProcessesCommand ??= new RelayCommand(LoadProcesses);

    private IRelayCommand? _queryProcessCommand;
    public IRelayCommand QueryProcessCommand => _queryProcessCommand ??= new AsyncRelayCommand(QueryProcessAsync);

    public ProcessListViewModel(SystemMonitorService monitorService, AIService aiService)
    {
        _monitorService = monitorService;
        _aiService = aiService;
        LoadProcesses();
    }

    public static string GetCpuNameStatic()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Name"]?.ToString()?.Trim() ?? $"{Environment.ProcessorCount} 核心";
            }
        }
        catch { }
        return $"{Environment.ProcessorCount} 核心";
    }

    public static string GetGpuNameStatic(int gpuIndex = 0)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            var gpus = new List<string>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    gpus.Add(name);
                }
            }
            if (gpus.Count > 0)
            {
                gpuIndex = Math.Min(gpuIndex, gpus.Count - 1);
                return gpus[gpuIndex];
            }
        }
        catch { }
        return "未检测到 GPU";
    }

    public static string GetGpuNameAllStatic()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            var gpus = new List<string>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    gpus.Add(name);
                }
            }
            if (gpus.Count > 0)
            {
                return string.Join("\n", gpus);
            }
        }
        catch { }
        return "未检测到 GPU";
    }

    public static string GetRamInfoStatic()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["TotalPhysicalMemory"] != null)
                {
                    var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    var totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                    return $"{totalGB:F1} GB";
                }
            }
        }
        catch { }

        var gcInfo = GC.GetGCMemoryInfo();
        return $"{gcInfo.TotalAvailableMemoryBytes / (1024 * 1024 * 1024)} GB";
    }

    public static string GetMotherboardInfoStatic()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                var manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                var product = obj["Product"]?.ToString()?.Trim() ?? "";
                return $"{manufacturer} {product}".Trim();
            }
        }
        catch { }
        return "未知主板";
    }

    public static string GetRamDetailStatic()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["TotalPhysicalMemory"] != null)
                {
                    var totalBytes = Convert.ToUInt64(obj["TotalPhysicalMemory"]);
                    var totalGB = totalBytes / (1024.0 * 1024.0 * 1024.0);
                    return $"{totalGB:F1} GB";
                }
            }
        }
        catch { }

        var gcInfo = GC.GetGCMemoryInfo();
        return $"{gcInfo.TotalAvailableMemoryBytes / (1024 * 1024 * 1024)} GB";
    }

    public static string GetDiskInfoStatic()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");
            var disks = new List<string>();
            foreach (ManagementObject obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString()?.Trim() ?? "";
                var size = obj["Size"];
                if (size != null)
                {
                    var sizeGB = Convert.ToUInt64(size) / (1024.0 * 1024.0 * 1024.0);
                    disks.Add($"{model} ({sizeGB:F0} GB)");
                }
            }
            return disks.Count > 0 ? string.Join("\n", disks) : "未检测到硬盘";
        }
        catch { }
        return "未检测到硬盘";
    }

    private void LoadProcesses()
    {
        Processes.Clear();
        var processes = _monitorService.GetAllProcesses();
        foreach (var p in processes)
        {
            Processes.Add(p);
        }
        ProcessCount = Processes.Count;
    }

    private async Task QueryProcessAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsLoading = true;
        try
        {
            var processes = _monitorService.GetAllProcesses();
            var result = await _aiService.QueryProcessAsync(SearchQuery, processes);
            SelectedProcessInfo = result;
        }
        catch (Exception ex)
        {
            SelectedProcessInfo = $"查询失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnSearchQueryChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            LoadProcesses();
            return;
        }

        Processes.Clear();
        var allProcesses = _monitorService.GetAllProcesses();
        foreach (var p in allProcesses)
        {
            if (p.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                Processes.Add(p);
            }
        }
        ProcessCount = Processes.Count;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
