using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 事件总线接口
public interface IEventBus
{
    void Publish<TEvent>(TEvent eventData);
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler);
    void Unsubscribe<TEvent>(Action<TEvent> handler);
}

// 插件管理器接口
public interface IPluginManager
{
    IPluginContext Context { get; }
    Task<PluginLoadResult> LoadPluginAsync(string assemblyPath);
    Task<UnloadResult> UnloadPluginAsync(string pluginId);
    IPluginContract GetPlugin(string pluginId);
    T GetPlugin<T>() where T : IPluginContract;
    IEnumerable<IPluginContract> GetAllPlugins();
}


// 插件上下文接口
public interface IPluginContext
{
    IServiceProvider ServiceProvider { get; }
    IPluginManager PluginManager { get; }
    IEventBus EventBus { get; }
    T GetService<T>() where T : class;
    T GetPlugin<T>() where T : IPluginContract;
}

// 严格插件契约接口
public interface IPluginContract : IDisposable
{
    string PluginId { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }

    string[] Dependencies { get; }
    string[] OptionalDependencies { get; }

    PluginStatus Status { get; }
    event Action<IPluginContract, PluginStatus> OnStatusChanged;

    Task<bool> InitializeAsync(IPluginContext context);
    Task StartAsync();
    Task StopAsync();

    object GetConfiguration();
    bool ValidateConfiguration(object config);
}

public enum PluginStatus
{
    NotInitialized,
    Initializing,
    Initialized,
    Starting,
    Running,
    Stopping,
    Stopped,
    Faulted,
    Disposed
}