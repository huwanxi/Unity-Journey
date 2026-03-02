using System;
using System.Threading.Tasks;
using UnityEngine;

// 音频插件完整实现
public class AudioPlugin : IPluginContract
{
    public string PluginId => "audio.manager";
    public string Name => "Audio Manager";
    public string Version => "1.0.0";
    public string Description => "Manages audio playback and settings";
    public string Author => "Game Studio";

    public string[] Dependencies => new string[0];
    public string[] OptionalDependencies => new string[0];

    public PluginStatus Status { get; private set; } = PluginStatus.NotInitialized;
    public event Action<IPluginContract, PluginStatus> OnStatusChanged;

    private AudioSource _audioSource;
    private IPluginContext _context;
    private bool _disposed = false;

    private void ChangeStatus(PluginStatus newStatus)
    {
        var oldStatus = Status;
        Status = newStatus;
        OnStatusChanged?.Invoke(this, oldStatus);
    }

    public async Task<bool> InitializeAsync(IPluginContext context)
    {
        if (_disposed) throw new ObjectDisposedException(Name);

        try
        {
            ChangeStatus(PluginStatus.Initializing);
            _context = context;

            // 创建音频对象
            var audioObject = new GameObject("AudioManager");
            _audioSource = audioObject.AddComponent<AudioSource>();
            UnityEngine.Object.DontDestroyOnLoad(audioObject);

            // 模拟异步初始化
            await Task.Delay(100);

            ChangeStatus(PluginStatus.Initialized);
            Debug.Log($"{Name} initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize {Name}: {ex.Message}");
            ChangeStatus(PluginStatus.Faulted);
            return false;
        }
    }

    public async Task StartAsync()
    {
        if (_disposed) throw new ObjectDisposedException(Name);

        try
        {
            ChangeStatus(PluginStatus.Starting);

            // 模拟异步启动
            await Task.Delay(50);

            ChangeStatus(PluginStatus.Running);
            Debug.Log($"{Name} started successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to start {Name}: {ex.Message}");
            ChangeStatus(PluginStatus.Faulted);
        }
    }

    public async Task StopAsync()
    {
        if (_disposed) throw new ObjectDisposedException(Name);

        try
        {
            ChangeStatus(PluginStatus.Stopping);

            // 停止所有音频
            if (_audioSource != null)
            {
                _audioSource.Stop();
            }

            // 模拟异步停止
            await Task.Delay(50);

            ChangeStatus(PluginStatus.Stopped);
            Debug.Log($"{Name} stopped successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to stop {Name}: {ex.Message}");
            ChangeStatus(PluginStatus.Faulted);
        }
    }

    public object GetConfiguration()
    {
        return new AudioConfig
        {
            Volume = _audioSource?.volume ?? 1.0f,
            Muted = _audioSource?.mute ?? false
        };
    }

    public bool ValidateConfiguration(object config)
    {
        return config is AudioConfig;
    }

    // 插件特定功能
    public void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        if (Status == PluginStatus.Running && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip, volume);
        }
    }

    public void SetVolume(float volume)
    {
        if (_audioSource != null)
        {
            _audioSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_audioSource != null && _audioSource.gameObject != null)
            {
                UnityEngine.Object.Destroy(_audioSource.gameObject);
            }
            _disposed = true;
            ChangeStatus(PluginStatus.Disposed);
        }
    }
}

// 音频配置
public struct AudioConfig
{
    public float Volume;
    public bool Muted;
}