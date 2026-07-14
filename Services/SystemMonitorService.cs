using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using meow_ai_tskmgr_ui3.Models;

namespace meow_ai_tskmgr_ui3.Services;

public class SystemMonitorService : IDisposable
{
    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicallyInstalledSystemMemory(out ulong totalMemoryInKilobytes);

    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramCounter;
    private List<PerformanceCounter> _gpuCounters = new();
    private bool _disposed;
    private bool _initialized;

    private Dictionary<int, (TimeSpan CpuTime, DateTime Time)> _prevCpuTimes = new();

    public SystemMonitorService()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            InitializeGpuCounters();
            _initialized = true;

            // PerformanceCounter 需要第一次调用后等待才能获取有效值
            _cpuCounter.NextValue();
            System.Threading.Thread.Sleep(100);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SystemMonitorService initialization error: {ex.Message}");
            _initialized = false;
        }
    }

    private void InitializeGpuCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instanceNames = category.GetInstanceNames();
            foreach (var instance in instanceNames)
            {
                if (instance.Contains("engtype_3D") || instance.Contains("engtype_Copy"))
                {
                    var counters = category.GetCounters(instance);
                    foreach (var counter in counters)
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            _gpuCounters.Add(counter);
                        }
                    }
                }
            }
            Debug.WriteLine($"GPU counters found: {_gpuCounters.Count}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GPU counter initialization error: {ex.Message}");
        }
    }

    public SystemStatus GetStatus()
    {
        var status = new SystemStatus();

        if (!_initialized)
        {
            Debug.WriteLine("SystemMonitorService not initialized");
            return status;
        }

        try
        {
            if (_cpuCounter != null)
            {
                status.CpuUsage = _cpuCounter.NextValue();
                Debug.WriteLine($"CPU Usage: {status.CpuUsage}%");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CPU counter error: {ex.Message}");
        }

        try
        {
            if (_gpuCounters.Count > 0)
            {
                float gpuTotal = 0;
                foreach (var counter in _gpuCounters)
                {
                    gpuTotal += counter.NextValue();
                }
                status.GpuUsage = Math.Min(gpuTotal, 100);
                Debug.WriteLine($"GPU Usage: {status.GpuUsage}%");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GPU counter error: {ex.Message}");
        }

        try
        {
            if (_ramCounter != null)
            {
                var availableMB = (ulong)_ramCounter.NextValue();
                GetPhysicallyInstalledSystemMemory(out ulong totalKB);
                status.TotalRamMB = totalKB / 1024;
                status.UsedRamMB = status.TotalRamMB - availableMB;
                status.RamUsage = status.TotalRamMB > 0 ? (float)status.UsedRamMB / status.TotalRamMB * 100 : 0;
                Debug.WriteLine($"RAM Usage: {status.RamUsage}% ({status.UsedRamMB}/{status.TotalRamMB} MB)");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"RAM counter error: {ex.Message}");
        }

        try
        {
            var processes = Process.GetProcesses();
            var processInfos = new List<ProcessInfo>();

            foreach (var proc in processes)
            {
                try
                {
                    if (proc.Id == 0) continue;

                    var info = new ProcessInfo
                    {
                        Pid = proc.Id,
                        Name = proc.ProcessName,
                        RamMB = (ulong)(proc.WorkingSet64 / (1024 * 1024))
                    };

                    var cpuTime = proc.TotalProcessorTime;
                    var now = DateTime.Now;

                    if (_prevCpuTimes.TryGetValue(proc.Id, out var prev))
                    {
                        var cpuDelta = cpuTime - prev.CpuTime;
                        var timeDelta = now - prev.Time;
                        if (timeDelta.TotalMilliseconds > 0)
                        {
                            info.CpuPercent = (float)(cpuDelta.TotalMilliseconds / (timeDelta.TotalMilliseconds * Environment.ProcessorCount) * 100);
                        }
                    }
                    _prevCpuTimes[proc.Id] = (cpuTime, now);

                    processInfos.Add(info);
                }
                catch { }
            }

            status.TopCpuProcesses = processInfos
                .Where(p => p.CpuPercent > 0 && p.Name != "Memory Compression")
                .OrderByDescending(p => p.CpuPercent)
                .Take(5)
                .ToList();

            status.TopRamProcesses = processInfos
                .Where(p => p.RamMB > 0 && p.Name != "Memory Compression")
                .OrderByDescending(p => p.RamMB)
                .Take(5)
                .ToList();

            foreach (var p in status.TopRamProcesses)
            {
                if (p.RamMB > 2000)
                    status.Warnings.Add($"{p.Name} 占用内存 {p.RamMB}MB");
            }

            foreach (var p in status.TopCpuProcesses)
            {
                if (p.CpuPercent > 80)
                    status.Warnings.Add($"{p.Name} CPU 占用 {p.CpuPercent:F1}% (严重)");
                else if (p.CpuPercent > 50)
                    status.Warnings.Add($"{p.Name} CPU 占用 {p.CpuPercent:F1}%");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Process enumeration error: {ex.Message}");
        }

        return status;
    }

    public List<ProcessInfo> GetAllProcesses()
    {
        var processes = new List<ProcessInfo>();
        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (proc.Id == 0) continue;
                    processes.Add(new ProcessInfo
                    {
                        Pid = proc.Id,
                        Name = proc.ProcessName,
                        RamMB = (ulong)(proc.WorkingSet64 / (1024 * 1024))
                    });
                }
                catch { }
                finally
                {
                    proc.Dispose();
                }
            }
        }
        catch { }
        return processes.OrderByDescending(p => p.RamMB).ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            foreach (var counter in _gpuCounters)
            {
                counter.Dispose();
            }
            _gpuCounters.Clear();
            _disposed = true;
        }
    }
}
