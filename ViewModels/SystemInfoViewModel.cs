using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using meow_ai_tskmgr_ui3.Models;
using meow_ai_tskmgr_ui3.Services;

namespace meow_ai_tskmgr_ui3.ViewModels;

public partial class SystemInfoViewModel : INotifyPropertyChanged
{
    private readonly SystemMonitorService _monitorService;
    private readonly AIService _aiService;

    private string _cpuName = "";
    private string _gpuName = "";
    private string _ramInfo = "";
    private string _osInfo = "";
    private string _machineName = "";
    private string _motherboardInfo = "";
    private string _diskInfo = "";
    private string _configCritique = "";
    private bool _isCritiquing;
    private int _processCount;
    private string _searchQuery = "";
    private string _selectedProcessInfo = "";
    private bool _isLoading;

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

    public string RamInfo
    {
        get => _ramInfo;
        set { _ramInfo = value; OnPropertyChanged(); }
    }

    public string OsInfo
    {
        get => _osInfo;
        set { _osInfo = value; OnPropertyChanged(); }
    }

    public string MachineName
    {
        get => _machineName;
        set { _machineName = value; OnPropertyChanged(); }
    }

    public string MotherboardInfo
    {
        get => _motherboardInfo;
        set { _motherboardInfo = value; OnPropertyChanged(); }
    }

    public string DiskInfo
    {
        get => _diskInfo;
        set { _diskInfo = value; OnPropertyChanged(); }
    }

    public string ConfigCritique
    {
        get => _configCritique;
        set { _configCritique = value; OnPropertyChanged(); }
    }

    public bool IsCritiquing
    {
        get => _isCritiquing;
        set { _isCritiquing = value; OnPropertyChanged(); }
    }

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

    private IRelayCommand? _critiqueConfigCommand;
    public IRelayCommand CritiqueConfigCommand => _critiqueConfigCommand ??= new AsyncRelayCommand(CritiqueConfigAsync);

    public SystemInfoViewModel(SystemMonitorService monitorService, AIService aiService)
    {
        _monitorService = monitorService;
        _aiService = aiService;
        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        MachineName = Environment.MachineName;
        OsInfo = RuntimeInformation.OSDescription;

        // 获取详细硬件信息
        CpuName = GetCpuNameStatic();
        GpuName = GetGpuNameAllStatic();  // 显示所有GPU
        RamInfo = GetRamInfoStatic();
        MotherboardInfo = GetMotherboardInfoStatic();
        DiskInfo = GetDiskInfoStatic();

        LoadProcesses();
    }

    private async Task CritiqueConfigAsync()
    {
        if (IsCritiquing) return;

        IsCritiquing = true;
        ConfigCritique = "猫娘正在审视你的配置...";

        try
        {
            var prompt = $@"你是一只可爱的猫娘AI助手。请用活泼可爱的语气评价以下电脑配置，可以适当吐槽或夸奖，150字以内。

计算机名: {MachineName}
操作系统: {OsInfo}
CPU: {CpuName}
GPU: {GpuName}
内存: {RamInfo}
主板: {MotherboardInfo}
硬盘: {DiskInfo}";

            var result = await _aiService.CritiqueConfigAsync(prompt);
            ConfigCritique = result;
        }
        catch (Exception ex)
        {
            ConfigCritique = $"锐评失败: {ex.Message}";
        }
        finally
        {
            IsCritiquing = false;
        }
    }

    // 静态方法，供 DashboardViewModel 调用
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

        // 备用方案
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

    private string GetCpuName() => GetCpuNameStatic();
    private string GetGpuName() => GetGpuNameStatic();
    private string GetRamInfo() => GetRamInfoStatic();
    private string GetMotherboardInfo() => GetMotherboardInfoStatic();

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

    private string GetDiskInfo() => GetDiskInfoStatic();

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
