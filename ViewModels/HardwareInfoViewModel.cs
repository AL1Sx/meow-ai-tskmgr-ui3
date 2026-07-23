using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using meow_ai_tskmgr_ui3.Services;

namespace meow_ai_tskmgr_ui3.ViewModels;

public partial class HardwareInfoViewModel : INotifyPropertyChanged
{
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

    private IRelayCommand? _critiqueConfigCommand;
    public IRelayCommand CritiqueConfigCommand => _critiqueConfigCommand ??= new AsyncRelayCommand(CritiqueConfigAsync);

    public HardwareInfoViewModel(AIService aiService)
    {
        _aiService = aiService;
        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        MachineName = Environment.MachineName;
        OsInfo = RuntimeInformation.OSDescription;

        CpuName = ProcessListViewModel.GetCpuNameStatic();
        GpuName = ProcessListViewModel.GetGpuNameAllStatic();
        RamInfo = ProcessListViewModel.GetRamInfoStatic();
        MotherboardInfo = ProcessListViewModel.GetMotherboardInfoStatic();
        DiskInfo = ProcessListViewModel.GetDiskInfoStatic();
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
