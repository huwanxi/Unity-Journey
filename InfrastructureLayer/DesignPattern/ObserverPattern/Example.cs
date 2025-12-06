// 使用 C# 事件实现
using System;
using UnityEngine;

public class Player
{
    // 定义事件（相当于主题的通知机制）
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDied;

    private int _health = 100;

    public int Health
    {
        get => _health;
        set
        {
            _health = value;
            OnHealthChanged?.Invoke(_health); // 通知所有订阅者

            if (_health <= 0)
            {
                OnPlayerDied?.Invoke(); // 通知玩家死亡
            }
        }
    }
}

// 观察者1：音效系统
public class SoundSystem
{
    public SoundSystem(Player player)
    {
        // 订阅事件
        player.OnHealthChanged += OnHealthChanged;
        player.OnPlayerDied += OnPlayerDied;
    }

    private void OnHealthChanged(int newHealth)
    {
        if (newHealth < 30)
        {
            Debug.Log("播放低血量警告音效");
        }
    }

    private void OnPlayerDied()
    {
        Debug.Log("播放玩家死亡音效");
    }
}

// 观察者2：UI 系统
public class UISystem
{
    public UISystem(Player player)
    {
        player.OnHealthChanged += UpdateHealthBar;
    }

    private void UpdateHealthBar(int health)
    {
        Debug.Log($"更新血条UI: {health}/100");
    }
}