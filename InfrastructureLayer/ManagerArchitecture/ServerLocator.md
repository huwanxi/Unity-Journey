## 什么是服务定位器模式？

服务定位器模式是一种**中心化的注册表**，用于提供对应用程序中各种服务的全局访问。它本质上是一个"服务的电话簿" - 你向它请求一个服务，它返回该服务的实例。

## 基础实现

### 1. 最简单的服务定位器

```csharp
using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    // 注册服务
    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service;
    }

    // 获取服务
    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return service as T;
        }
        return null;
    }

    // 检查服务是否存在
    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    // 注销服务
    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    // 清空所有服务（用于场景切换或重置）
    public static void Clear()
    {
        _services.Clear();
    }
}
```

### 2. 在 Unity 中的使用示例

**定义服务接口：**
```csharp
// 音频服务接口
public interface IAudioService
{
    void PlaySound(string soundId);
    void PlayMusic(string musicId);
    void SetVolume(float volume);
}

// 存档服务接口  
public interface ISaveService
{
    void SaveGame(string saveId);
    void LoadGame(string saveId);
    bool SaveExists(string saveId);
}
```

**实现具体服务：**
```csharp
public class AudioManager : MonoBehaviour, IAudioService
{
    public void PlaySound(string soundId)
    {
        Debug.Log($"Playing sound: {soundId}");
        // 实际的音频播放逻辑
    }

    public void PlayMusic(string musicId)
    {
        Debug.Log($"Playing music: {musicId}");
    }

    public void SetVolume(float volume)
    {
        Debug.Log($"Setting volume to: {volume}");
    }
}

public class SaveManager : MonoBehaviour, ISaveService
{
    public void SaveGame(string saveId)
    {
        Debug.Log($"Saving game: {saveId}");
    }

    public void LoadGame(string saveId)
    {
        Debug.Log($"Loading game: {saveId}");
    }

    public bool SaveExists(string saveId)
    {
        return true;
    }
}
```

**注册和使用服务：**
```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private SaveManager saveManager;

    private void Awake()
    {
        // 注册服务
        ServiceLocator.Register<IAudioService>(audioManager);
        ServiceLocator.Register<ISaveService>(saveManager);

        // 确保引导对象不被销毁
        DontDestroyOnLoad(gameObject);
    }
}

// 在任何其他脚本中使用
public class PlayerController : MonoBehaviour
{
    private void Start()
    {
        // 获取音频服务
        var audioService = ServiceLocator.Get<IAudioService>();
        audioService?.PlaySound("Jump");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            // 获取存档服务
            var saveService = ServiceLocator.Get<ISaveService>();
            saveService?.SaveGame("AutoSave");
        }
    }
}
```

## 高级特性实现

### 3. 增强的服务定位器（支持默认服务和验证）

```csharp
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
    private static readonly Dictionary<Type, Func<object>> _providers = new Dictionary<Type, Func<object>>();

    // 基础注册
    public static void Register<T>(T service) where T : class
    {
        _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
    }

    // 使用工厂方法注册（延迟初始化）
    public static void Register<T>(Func<T> serviceFactory) where T : class
    {
        _providers[typeof(T)] = serviceFactory;
    }

    // 安全的获取服务（带验证）
    public static T Get<T>() where T : class
    {
        var type = typeof(T);

        // 首先检查已有实例
        if (_services.TryGetValue(type, out var service))
        {
            return service as T;
        }

        // 检查是否有工厂方法
        if (_providers.TryGetValue(type, out var factory))
        {
            service = factory();
            _services[type] = service; // 缓存结果
            return service as T;
        }

        // 在编辑器中给出警告
#if UNITY_EDITOR
        Debug.LogWarning($"Service of type {type.Name} is not registered!");
#endif
        
        return null;
    }

    // 强制获取服务（如果不存在会报错）
    public static T GetRequired<T>() where T : class
    {
        var service = Get<T>();
        if (service == null)
        {
            throw new InvalidOperationException($"Required service of type {typeof(T).Name} is not registered!");
        }
        return service;
    }

    // 注册服务并返回自身，支持链式调用
    public static T RegisterAs<T>(this T service) where T : class
    {
        Register(service);
        return service;
    }
}
```

## 实际应用场景

### 4. 在复杂项目中的使用

```csharp
// 引导程序 - 负责初始化所有服务
public class AppBootstrap : MonoBehaviour
{
    [Header("Service References")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private NetworkManager networkManager;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitializeServices();
    }

    private void InitializeServices()
    {
        // 注册核心服务
        ServiceLocator.Register<IAudioService>(audioManager);
        ServiceLocator.Register<IUIService>(uiManager);
        ServiceLocator.Register<INetworkService>(networkManager);

        // 使用工厂模式注册（延迟初始化）
        ServiceLocator.Register<IAnalyticsService>(() => new AnalyticsManager());
        ServiceLocator.Register<IAdService>(() => new AdManager());

        // 链式注册
        new LocalizationManager().RegisterAs<ILocalizationService>();

        Debug.Log("All services initialized");
    }
}

// 游戏管理器使用各种服务
public class GameManager : MonoBehaviour
{
    private IAudioService _audio;
    private IUIService _ui;
    private IAnalyticsService _analytics;

    private void Start()
    {
        // 获取所有需要的服务
        _audio = ServiceLocator.GetRequired<IAudioService>();
        _ui = ServiceLocator.GetRequired<IUIService>();
        _analytics = ServiceLocator.GetRequired<IAnalyticsService>();

        StartGame();
    }

    private void StartGame()
    {
        _audio.PlayMusic("Background");
        _ui.ShowScreen("HUD");
        _analytics.TrackEvent("GameStarted");
    }
}
```

### 5. 测试和模拟的威力

```csharp
// 在测试时，可以注册模拟服务
public class GameTestSetup : MonoBehaviour
{
    private void Awake()
    {
        // 注册模拟的音频服务（不会真正播放声音）
        ServiceLocator.Register<IAudioService>(new MockAudioService());
        
        // 注册模拟的存档服务（使用内存存储）
        ServiceLocator.Register<ISaveService>(new MockSaveService());
    }
}

// 模拟音频服务实现
public class MockAudioService : IAudioService
{
    public void PlaySound(string soundId)
    {
        Debug.Log($"[MOCK] Would play sound: {soundId}");
        // 不实际播放音频，适合在测试模式使用
    }

    public void PlayMusic(string musicId)
    {
        Debug.Log($"[MOCK] Would play music: {musicId}");
    }

    public void SetVolume(float volume)
    {
        Debug.Log($"[MOCK] Volume set to: {volume}");
    }
}
```

## 服务定位器 vs 单例模式

| 方面 | 服务定位器 | 单例模式 |
|------|------------|----------|
| **耦合度** | 低 - 依赖接口而非具体类 | 高 - 直接依赖具体类 |
| **可测试性** | 高 - 容易注入模拟服务 | 低 - 难以替换实现 |
| **灵活性** | 高 - 运行时可以替换服务 | 低 - 实现是固定的 |
| **初始化控制** | 灵活 - 可以控制初始化时机 | 固定 - 通常在使用时初始化 |
| **复杂度** | 较高 - 需要设置注册机制 | 较低 - 实现简单 |

## 最佳实践

1. **面向接口编程**：服务应该通过接口暴露，而不是具体实现类
2. **明确的初始化**：在游戏启动时明确注册所有服务
3. **依赖验证**：在 `GetRequired` 失败时提供清晰的错误信息
4. **生命周期管理**：注意服务的创建和销毁时机
5. **测试友好**：总是提供模拟服务实现用于测试

## 在 Unity 中的完整示例

```csharp
// 主场景引导器
public class MainSceneBootstrap : MonoBehaviour
{
    private void Start()
    {
        // 确保核心服务已注册
        if (!ServiceLocator.IsRegistered<IAudioService>())
        {
            // 回退到查找场景中的组件
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
            {
                ServiceLocator.Register<IAudioService>(audioManager);
            }
        }

        // 初始化场景特定的逻辑
        InitializeScene();
    }

    private void InitializeScene()
    {
        var uiService = ServiceLocator.Get<IUIService>();
        uiService?.ShowScreen("MainMenu");
    }
}
```

服务定位器模式在大型 Unity 项目中特别有价值，它提供了单例模式的便利性，同时解决了单例模式的许多缺点，特别是**可测试性**和**灵活性**方面。