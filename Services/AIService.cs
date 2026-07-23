using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using meow_ai_tskmgr_ui3.Models;

namespace meow_ai_tskmgr_ui3.Services;

public class AIService : IDisposable
{
    private readonly ConfigService _configService;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    public AIService(ConfigService configService)
    {
        _configService = configService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    public async Task<AnalysisResult> AnalyzeSystemAsync(SystemStatus status)
    {
        var sw = Stopwatch.StartNew();

        var prompt = BuildSystemPrompt(status);
        var result = await CallAIAsync(prompt);

        sw.Stop();

        return new AnalysisResult
        {
            Content = result.Content,
            PromptTokens = result.PromptTokens,
            CompletionTokens = result.CompletionTokens,
            DurationSeconds = sw.Elapsed.TotalSeconds,
            Timestamp = DateTime.Now,
            ModelName = _configService.Config.Api.Model ?? "deepseek-v4-flash"
        };
    }

    public async Task<string> QueryProcessAsync(string processName, List<ProcessInfo> processes)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"用户想了解进程: {processName}");
        sb.AppendLine("当前运行的进程:");
        foreach (var p in processes.Take(20))
        {
            sb.AppendLine($"- {p.Name} (PID: {p.Pid}, RAM: {p.RamMB}MB)");
        }
        sb.AppendLine("\n请用100字以内解释这个进程的作用，用可爱的猫咪语气回答。");

        var result = await CallAIAsync(sb.ToString());
        return result.Content;
    }

    public async Task<string> CritiqueConfigAsync(string configInfo)
    {
        var result = await CallAIAsync(configInfo);
        return result.Content;
    }

    private string BuildSystemPrompt(SystemStatus status)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是一只可爱的猫娘AI助手，请用活泼可爱的语气回复，适当使用喵~等语气词。");
        sb.AppendLine("\n当前系统状态:");
        sb.AppendLine($"- CPU 使用率: {status.CpuUsage:F1}%");
        sb.AppendLine($"- GPU 使用率: {status.GpuUsage:F1}%");
        sb.AppendLine($"- 内存使用: {status.UsedRamMB}MB / {status.TotalRamMB}MB ({status.RamUsage:F1}%)");

        if (status.TopCpuProcesses.Count > 0)
        {
            sb.AppendLine("\nCPU 占用最高的进程:");
            foreach (var p in status.TopCpuProcesses)
            {
                sb.AppendLine($"- {p.Name}: {p.CpuPercent:F1}%");
            }
        }

        if (status.TopRamProcesses.Count > 0)
        {
            sb.AppendLine("\n内存占用最高的进程:");
            foreach (var p in status.TopRamProcesses)
            {
                sb.AppendLine($"- {p.Name}: {p.RamMB}MB");
            }
        }

        if (status.Warnings.Count > 0)
        {
            sb.AppendLine("\n警告:");
            foreach (var w in status.Warnings)
            {
                sb.AppendLine($"- {w}");
            }
        }

        sb.AppendLine("\n请分析用户可能在做什么（如游戏、看视频、办公、摸鱼等），100-150字以内回复。");

        return sb.ToString();
    }

    private async Task<AIResponse> CallAIAsync(string userMessage)
    {
        var config = _configService.Config?.Api;
        if (config == null || string.IsNullOrEmpty(config.ApiKey))
        {
            return new AIResponse
            {
                Content = "请先在设置页面配置 API 密钥",
                PromptTokens = 0,
                CompletionTokens = 0
            };
        }

        var requestBody = new
        {
            model = config.Model ?? "deepseek-v4-flash",
            messages = new[]
            {
                new { role = "user", content = userMessage }
            },
            max_tokens = 500,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.Endpoint ?? "https://api.deepseek.com"}/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey) }
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new AIResponse
                {
                    Content = $"API 错误: {response.StatusCode} - {responseJson}",
                    PromptTokens = 0,
                    CompletionTokens = 0
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    return new AIResponse { Content = "未收到有效回复", PromptTokens = 0, CompletionTokens = 0 };
                }

                var message = choices[0].GetProperty("message");
                var responseContent = message.GetProperty("content").GetString() ?? "";

                // 如果 content 为空，尝试获取 reasoning_content
                if (string.IsNullOrWhiteSpace(responseContent) && message.TryGetProperty("reasoning_content", out var reasoning))
                {
                    var reasoningText = reasoning.GetString() ?? "";
                    responseContent = reasoningText.Length > 200
                        ? reasoningText.Substring(0, 200)
                        : reasoningText;
                }

                // 获取 token 使用量
                int promptTokens = 0;
                int completionTokens = 0;
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pt))
                        promptTokens = pt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var ct))
                        completionTokens = ct.GetInt32();
                }

                return new AIResponse
                {
                    Content = CleanMarkdown(responseContent),
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens
                };
            }
            catch (JsonException ex)
            {
                return new AIResponse
                {
                    Content = $"JSON 解析错误: {ex.Message}",
                    PromptTokens = 0,
                    CompletionTokens = 0
                };
            }
        }
        catch (HttpRequestException ex)
        {
            return new AIResponse
            {
                Content = $"网络请求失败: {ex.Message}",
                PromptTokens = 0,
                CompletionTokens = 0
            };
        }
        catch (TaskCanceledException)
        {
            return new AIResponse
            {
                Content = "请求超时，请检查网络连接",
                PromptTokens = 0,
                CompletionTokens = 0
            };
        }
        catch (Exception ex)
        {
            return new AIResponse
            {
                Content = $"请求失败: {ex.Message}",
                PromptTokens = 0,
                CompletionTokens = 0
            };
        }
    }

    private string CleanMarkdown(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"(\*\*|__)(.*?)\1", "$2")
            .Replace("`", "")
            .Trim();
    }

    private class AIResponse
    {
        public string Content { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
    }
}
