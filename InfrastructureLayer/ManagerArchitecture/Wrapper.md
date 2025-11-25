## 为什么需要包装器：具体原因

### 1. 生命周期管理的标准化

```csharp
// 不同的插件可能有不同的初始化接口
public interface IPluginA 
{
    void Initialize();  // 有的叫 Initialize
}

public interface IPluginB  
{
    void Setup();       // 有的叫 Setup
    void Cleanup();     // 有的叫 Cleanup
}

public interface IPluginC
{
    void Init();        // 有的叫 Init
    void Destroy();     // 有的叫 Destroy
}

// 包装器提供统一接口
public class PluginWrapper<T> : IDisposable where T : class
{
    private T _plugin;
    
    public PluginWrapper(T plugin)
    {
        _plugin = plugin;
        
        // 统一处理各种初始化方法
        if (plugin is IPluginA a) a.Initialize();
        else if (plugin is IPluginB b) b.Setup(); 
        else if (plugin is IPluginC c) c.Init();
        // 还可以通过反射来查找初始化方法...
    }
    
    public void Dispose()
    {
        // 统一处理各种销毁方法
        if (_plugin is IPluginB b) b.Cleanup();
        else if (_plugin is IPluginC c) c.Destroy();
        // 统一的资源释放逻辑...
    }
}
```

### 2. 资源管理和异常保护

```csharp
public class SafePluginWrapper<T> : IDisposable where T : class
{
    private T _plugin;
    private bool _isInitialized = false;
    
    public SafePluginWrapper(T plugin)
    {
        try
        {
            _plugin = plugin;
            
            // 包装器添加了异常处理
            if (plugin is IPluginA a) 
            {
                a.Initialize();
                _isInitialized = true;
            }
            // 其他插件类型...
        }
        catch (Exception ex)
        {
            Debug.LogError($"插件初始化失败: {ex.Message}");
            _isInitialized = false;
        }
    }
    
    public void Execute(Action<T> action)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("插件未正确初始化，跳过执行");
            return;
        }
        
        try
        {
            action(_plugin);
        }
        catch (Exception ex)
        {
            Debug.LogError($"插件执行异常: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        // 确保资源释放，即使插件自己的销毁方法有问题
        if (_plugin is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"插件销毁异常: {ex.Message}");
            }
        }
        _plugin = null;
    }
}
```

### 3. 跨插件依赖管理

```csharp
public class PluginManager
{
    private Dictionary<Type, object> _wrappers = new Dictionary<Type, object>();
    
    public void RegisterPlugin<T>(T plugin, Type[] dependencies = null) where T : class
    {
        var wrapper = new PluginWrapper<T>(plugin);
        _wrappers[typeof(T)] = wrapper;
        
        // 包装器负责解决依赖关系
        ResolveDependencies(plugin, dependencies);
    }
    
    private void ResolveDependencies(object plugin, Type[] dependencies)
    {
        if (dependencies == null) return;
        
        foreach (var depType in dependencies)
        {
            if (_wrappers.TryGetValue(depType, out var depWrapper))
            {
                // 通过反射或其他机制注入依赖
                InjectDependency(plugin, depType, GetPluginFromWrapper(depWrapper));
            }
        }
    }
}
```

### 4. 性能监控和统计

```csharp
public class MonitoredPluginWrapper<T> : IDisposable where T : class
{
    private T _plugin;
    private Stopwatch _initTimer = new Stopwatch();
    private Stopwatch _executionTimer = new Stopwatch();
    private int _callCount = 0;
    
    public MonitoredPluginWrapper(T plugin)
    {
        _initTimer.Start();
        // 初始化插件...
        _initTimer.Stop();
        
        Debug.Log($"插件 {typeof(T).Name} 初始化耗时: {_initTimer.ElapsedMilliseconds}ms");
    }
    
    public TResult Execute<TResult>(Func<T, TResult> func)
    {
        _callCount++;
        _executionTimer.Start();
        
        try
        {
            var result = func(_plugin);
            return result;
        }
        finally
        {
            _executionTimer.Stop();
            
            // 每100次调用输出一次性能统计
            if (_callCount % 100 == 0)
            {
                Debug.Log($"插件 {typeof(T).Name} 平均执行时间: {_executionTimer.ElapsedMilliseconds / _callCount}ms");
            }
        }
    }
}
```

### 5. 实际应用场景

```csharp
// 第三方插件 - 你无法修改它的代码
public class ThirdPartyAudioPlugin
{
    public void StartEngine() { /* 第三方初始化 */ }
    public void StopEngine() { /* 第三方清理 */ }
    public void PlaySound(string sound) { /* 播放音效 */ }
}

// 包装器适配你的系统
public class AudioPluginWrapper : IAudioService, IDisposable
{
    private ThirdPartyAudioPlugin _plugin;
    
    public AudioPluginWrapper()
    {
        _plugin = new ThirdPartyAudioPlugin();
        _plugin.StartEngine(); // 转换为你的生命周期
    }
    
    public void PlaySound(string sound)
    {
        _plugin.PlaySound(sound);
    }
    
    public void Dispose()
    {
        _plugin.StopEngine(); // 统一销毁逻辑
    }
}

// 使用
var audioWrapper = new AudioPluginWrapper();
ServiceLocator.Register<IAudioService>(audioWrapper);
```

## 总结：包装器的价值

| 场景 | 没有包装器 | 有包装器 |
|------|------------|----------|
| **第三方插件** | 无法统一生命周期接口 | ✅ 可以适配到标准接口 |
| **异常处理** | 插件崩溃影响整个系统 | ✅ 异常被隔离在包装器内 |
| **性能监控** | 难以统计插件性能 | ✅ 包装器自动收集指标 |
| **依赖管理** | 手动处理依赖关系 | ✅ 自动依赖注入 |
| **热插拔** | 直接替换可能有问题 | ✅ 安全的热替换机制 |
| **测试** | 难以模拟插件行为 | ✅ 可以创建测试包装器 |

**核心思想**：包装器不是在重复插件的工作，而是在**管理插件的使用**。就像项目经理不直接写代码，但他确保团队协作顺畅一样。

包装器提供了**控制层**，而插件提供**功能层**。这种分离让系统更加健壮和灵活。