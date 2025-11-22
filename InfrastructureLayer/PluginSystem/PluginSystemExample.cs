using System.Threading.Tasks;
using UnityEngine;

public class PluginSystemExample : MonoBehaviour
{
    private PluginManager _pluginManager;

    async void Start()
    {
        _pluginManager = new PluginManager();

        // 加载音频插件
        string pluginPath = Application.dataPath + "/Plugins/AudioPlugin.dll";
        var result = await _pluginManager.LoadPluginAsync(pluginPath);

        if (result.Success)
        {
            Debug.Log("Plugin loaded successfully!");

            // 获取插件并启动
            var audioPlugin = _pluginManager.GetPlugin<AudioPlugin>();
            if (audioPlugin != null)
            {
                await audioPlugin.StartAsync();

                // 使用插件功能
                // audioPlugin.PlaySound(someClip, 0.8f);
            }
        }
        else
        {
            Debug.LogError($"Failed to load plugin: {result.ErrorMessage}");
        }
    }

    async void OnDestroy()
    {
        if (_pluginManager != null)
        {
            // 卸载所有插件
            foreach (var plugin in _pluginManager.GetAllPlugins())
            {
                await _pluginManager.UnloadPluginAsync(plugin.PluginId);
            }
            _pluginManager.Dispose();
        }
    }
}