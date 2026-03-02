using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 初始化结果
public struct InitializationResult
{
    public bool Success;
    public List<string> Errors;
    public List<string> Warnings;

    public static InitializationResult Create()
    {
        return new InitializationResult
        {
            Errors = new List<string>(),
            Warnings = new List<string>()
        };
    }
}

// 插件包装器实现
public class PluginWrapper : IDisposable
{
    public IPluginContract Plugin { get; }
    public string AssemblyPath { get; }
    public PluginStatus Status => Plugin?.Status ?? PluginStatus.Disposed;

    private bool _disposed = false;
    private readonly List<IDisposable> _eventSubscriptions = new List<IDisposable>();

    public PluginWrapper(IPluginContract plugin, string assemblyPath)
    {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        AssemblyPath = assemblyPath;

        // 监听插件状态变化
        plugin.OnStatusChanged += OnPluginStatusChanged;
    }

    public async Task<InitializationResult> InitializeAsync(IPluginContext context)
    {
        var result = InitializationResult.Create();

        try
        {
            // 验证依赖
            var dependencyResult = await ValidateDependenciesAsync(Plugin.Dependencies, context);
            if (!dependencyResult.Success)
            {
                if (dependencyResult.MissingDependencies != null && dependencyResult.MissingDependencies.Count > 0)
                {
                    result.Errors.Add($"Missing dependencies: {string.Join(", ", dependencyResult.MissingDependencies)}");
                }
                return result;
            }

            // 执行初始化
            bool initSuccess = await Plugin.InitializeAsync(context);
            if (!initSuccess)
            {
                result.Errors.Add("Plugin initialization returned false");
                return result;
            }

            result.Success = true;
            result.Warnings.AddRange(dependencyResult.Warnings);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Initialization exception: {ex.Message}");
        }

        return result;
    }

    public async Task StartAsync()
    {
        if (!_disposed && (Status == PluginStatus.Initialized || Status == PluginStatus.Stopped))
        {
            await Plugin.StartAsync();
        }
    }

    public async Task StopAsync()
    {
        if (!_disposed && (Status == PluginStatus.Running || Status == PluginStatus.Starting))
        {
            await Plugin.StopAsync();
        }
    }

    private void OnPluginStatusChanged(IPluginContract plugin, PluginStatus status)
    {
        Debug.Log($"Plugin {plugin.Name} status changed to {status}");

        // 发布状态变化事件
        var eventBus = (plugin as IPluginContract)?.GetConfiguration() as IEventBus;
        eventBus?.Publish(new PluginStatusChangedEvent
        {
            PluginId = plugin.PluginId,
            PluginName = plugin.Name,
            OldStatus = Status,
            NewStatus = status
        });
    }

    private async Task<DependencyValidationResult> ValidateDependenciesAsync(string[] dependencies, IPluginContext context)
    {
        var result = new DependencyValidationResult();

        if (dependencies == null)
        {
            result.Success = true;
            return result;
        }

        foreach (var dependency in dependencies)
        {
            var plugin = context.PluginManager.GetPlugin(dependency);
            if (plugin == null)
            {
                result.MissingDependencies.Add(dependency);
                continue;
            }

            if (plugin.Status != PluginStatus.Running && plugin.Status != PluginStatus.Initialized)
            {
                result.NotReadyDependencies.Add(dependency);
            }
        }

        result.Success = result.MissingDependencies.Count == 0 && result.NotReadyDependencies.Count == 0;

        if (result.NotReadyDependencies.Count > 0)
        {
            result.Warnings.Add($"Some dependencies are not ready: {string.Join(", ", result.NotReadyDependencies)}");
        }

        return result;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Plugin.OnStatusChanged -= OnPluginStatusChanged;

            foreach (var subscription in _eventSubscriptions)
            {
                subscription.Dispose();
            }
            _eventSubscriptions.Clear();

            Plugin.Dispose();
            _disposed = true;
        }
    }
}

// 依赖验证结果
public struct DependencyValidationResult
{
    public bool Success;
    public List<string> MissingDependencies;
    public List<string> NotReadyDependencies;
    public List<string> Warnings;

}

// 插件状态变更事件
public struct PluginStatusChangedEvent
{
    public string PluginId;
    public string PluginName;
    public PluginStatus OldStatus;
    public PluginStatus NewStatus;
}