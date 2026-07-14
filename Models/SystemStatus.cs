using System;
using System.Collections.Generic;

namespace meow_ai_tskmgr_ui3.Models;

public class SystemStatus
{
    public float CpuUsage { get; set; }
    public float GpuUsage { get; set; }
    public float RamUsage { get; set; }
    public ulong UsedRamMB { get; set; }
    public ulong TotalRamMB { get; set; }
    public List<ProcessInfo> TopCpuProcesses { get; set; } = new();
    public List<ProcessInfo> TopRamProcesses { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ProcessInfo
{
    public int Pid { get; set; }
    public string Name { get; set; } = string.Empty;
    public float CpuPercent { get; set; }
    public ulong RamMB { get; set; }
}

public class AnalysisResult
{
    public string Content { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public double DurationSeconds { get; set; }
    public DateTime Timestamp { get; set; }
    public double Cost => (PromptTokens * 1.00 + CompletionTokens * 2.00) / 1_000_000.0;
}

public class AppConfig
{
    public ApiSettings Api { get; set; } = new();
    public MonitorSettings Monitor { get; set; } = new();
    public HardwareSettings Hardware { get; set; } = new();
}

public class ApiSettings
{
    public string Endpoint { get; set; } = "https://api.deepseek.com";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "deepseek-chat";
}

public class MonitorSettings
{
    public int AnalysisIntervalMinutes { get; set; } = 3;
}

public class HardwareSettings
{
    public bool ShowCpuName { get; set; } = true;
    public bool ShowGpuName { get; set; } = true;
    public bool ShowRamDetail { get; set; } = true;
    public bool ShowMotherboard { get; set; } = false;
    public int GpuIndex { get; set; } = 0;
}
