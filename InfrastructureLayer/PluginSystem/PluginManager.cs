using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

// 插件加载结果
public struct PluginLoadResult
{
    public bool Success;
    public IPluginContract Plugin;
    public string ErrorMessage;
    public Exception Exception;
    public List<string> Warnings;
}

// 插件卸载结果
public struct UnloadResult
{
    public bool Success;
    public string ErrorMessage;

    public static UnloadResult SuccessResult => new UnloadResult { Success = true };
    public static UnloadResult NotFound => new UnloadResult { Success = false, ErrorMessage = "Plugin not found" };
}

// 插件管理器完整实现
public class PluginManager : IPluginManager, IDisposable
{
    private readonly Dictionary<string, PluginWrapper> _plugins = new Dictionary<string, PluginWrapper>();
    private readonly IEventBus _eventBus;
    private readonly PluginContext _context;
    private bool _disposed = false;

    public PluginManager()
    {
        _eventBus = new EventBus();
        _context = new PluginContext(this, _eventBus);
    }

    public IPluginContext Context => _context;

    public async Task<PluginLoadResult> LoadPluginAsync(string assemblyPath)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PluginManager));

        try
        {
            // 检查文件是否存在
            if (!File.Exists(assemblyPath))
            {
                return new PluginLoadResult
                {
                    Success = false,
                    ErrorMessage = $"Assembly file not found: {assemblyPath}"
                };
            }

            // 加载程序集
            Assembly assembly = Assembly.LoadFrom(assemblyPath);

            // 查找实现IPluginContract的类型
            Type pluginType = null;
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IPluginContract).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                {
                    pluginType = type;
                    break;
                }
            }

            if (pluginType == null)
            {
                return new PluginLoadResult
                {
                    Success = false,
                    ErrorMessage = $"No plugin type found in assembly: {assemblyPath}"
                };
            }

            // 创建插件实例
            var plugin = Activator.CreateInstance(pluginType) as IPluginContract;
            if (plugin == null)
            {
                return new PluginLoadResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to create plugin instance: {pluginType.Name}"
                };
            }

            // 包装插件
            var wrapper = new PluginWrapper(plugin, assemblyPath);
            _plugins[plugin.PluginId] = wrapper;

            // 初始化插件
            var initResult = await wrapper.InitializeAsync(_context);
            if (!initResult.Success)
            {
                _plugins.Remove(plugin.PluginId);
                return new PluginLoadResult
                {
                    Success = false,
                    ErrorMessage = $"Plugin initialization failed: {string.Join(", ", initResult.Errors)}"
                };
            }

            Debug.Log($"Plugin loaded successfully: {plugin.Name} v{plugin.Version}");

            return new PluginLoadResult
            {
                Success = true,
                Plugin = plugin,
                Warnings = initResult.Warnings
            };
        }
        catch (Exception ex)
        {
            return new PluginLoadResult
            {
                Success = false,
                ErrorMessage = $"Failed to load plugin: {ex.Message}",
                Exception = ex
            };
        }
    }

    public async Task<UnloadResult> UnloadPluginAsync(string pluginId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PluginManager));

        if (_plugins.TryGetValue(pluginId, out var wrapper))
        {
            try
            {
                await wrapper.StopAsync();
                wrapper.Dispose();
                _plugins.Remove(pluginId);

                Debug.Log($"Plugin unloaded successfully: {pluginId}");
                return UnloadResult.SuccessResult;
            }
            catch (Exception ex)
            {
                return new UnloadResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        return UnloadResult.NotFound;
    }

    public IPluginContract GetPlugin(string pluginId)
    {
        return _plugins.TryGetValue(pluginId, out var wrapper) ? wrapper.Plugin : null;
    }

    public T GetPlugin<T>() where T : IPluginContract
    {
        foreach (var wrapper in _plugins.Values)
        {
            if (wrapper.Plugin is T plugin)
                return plugin;
        }
        return default(T);
    }

    public IEnumerable<IPluginContract> GetAllPlugins()
    {
        foreach (var wrapper in _plugins.Values)
        {
            yield return wrapper.Plugin;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var wrapper in _plugins.Values)
            {
                wrapper.Dispose();
            }
            _plugins.Clear();
            _disposed = true;
        }
    }
}

// 插件上下文实现
public class PluginContext : IPluginContext
{
    public IServiceProvider ServiceProvider { get; }
    public IPluginManager PluginManager { get; }
    public IEventBus EventBus { get; }

    public PluginContext(IPluginManager pluginManager, IEventBus eventBus)
    {
        PluginManager = pluginManager;
        EventBus = eventBus;
        ServiceProvider = new ServiceProvider();
    }

    public T GetService<T>() where T : class
    {
        return ServiceProvider.GetService(typeof(T)) as T;
    }

    public T GetPlugin<T>() where T : IPluginContract
    {
        return PluginManager.GetPlugin<T>();
    }
}

// 简单服务提供者
public class ServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public ServiceProvider()
    {
        // 注册默认服务
        _services[typeof(ILogger)] = new UnityLogger();
    }

    public object GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    public T GetService<T>()
    {
        return (T)GetService(typeof(T));
    }

    public void RegisterService<T>(T service)
    {
        _services[typeof(T)] = service;
    }
}

public interface ILogger
{
    void Log(string message);
    void LogError(string message);
}

public class UnityLogger : ILogger
{
    public void Log(string message) => Debug.Log(message);
    public void LogError(string message) => Debug.LogError(message);
}

