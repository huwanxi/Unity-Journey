using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 步骤1: 定义观察者接口
public interface IAchievementObserver
{
    void OnPlayerScoreChanged(int newScore);
}

// 步骤2: 定义主题（被观察者）
public class PlayerScoreSubject
{
    // 私有列表，存储所有观察者
    private List<IAchievementObserver> _observers = new List<IAchievementObserver>();

    private int _score;
    public int Score
    {
        get => _score;
        set
        {
            if (_score != value)
            {
                _score = value;
                // 分数改变时，通知所有观察者
                NotifyObservers();
            }
        }
    }

    // 注册观察者
    public void RegisterObserver(IAchievementObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    // 移除观察者
    public void UnregisterObserver(IAchievementObserver observer)
    {
        _observers.Remove(observer);
    }

    // 通知所有观察者
    private void NotifyObservers()
    {
        // 为了安全，在遍历前创建一个副本，防止在通知过程中列表被修改
        foreach (var observer in _observers.ToArray())
        {
            observer.OnPlayerScoreChanged(_score);
        }
    }
}

// 步骤3: 实现具体观察者 - 成就系统
public class AchievementSystem : IAchievementObserver
{
    private bool _hundredPointsUnlocked = false;

    public void OnPlayerScoreChanged(int newScore)
    {
        // 当分数达到100时，解锁成就
        if (newScore >= 100 && !_hundredPointsUnlocked)
        {
            UnlockAchievement("百步穿杨");
            _hundredPointsUnlocked = true;
        }

        // 可以检查更多成就条件...
        if (newScore >= 500)
        {
            UnlockAchievement("分数大师");
        }
    }

    private void UnlockAchievement(string achievementName)
    {
        Debug.Log($"成就已解锁: {achievementName}");
        // 这里可以触发UI显示、播放音效等
    }
}

// 步骤4: 实现具体观察者 - UI 分数显示
public class UIScoreDisplay : IAchievementObserver
{
    public void OnPlayerScoreChanged(int newScore)
    {
        // 更新UI显示
        Debug.Log($"UI更新: 当前分数 {newScore}");
    }
}

// 步骤5: 在Unity中使用
public class GameManager : MonoBehaviour
{
    private PlayerScoreSubject _scoreSubject;
    private AchievementSystem _achievementSystem;
    private UIScoreDisplay _uiDisplay;

    void Start()
    {
        // 创建主题
        _scoreSubject = new PlayerScoreSubject();

        // 创建观察者
        _achievementSystem = new AchievementSystem();
        _uiDisplay = new UIScoreDisplay();

        // 注册观察者
        _scoreSubject.RegisterObserver(_achievementSystem);
        _scoreSubject.RegisterObserver(_uiDisplay);

        // 模拟分数变化
        StartCoroutine(SimulateScoreChanges());
    }

    private IEnumerator SimulateScoreChanges()
    {
        _scoreSubject.Score = 10;
        yield return new WaitForSeconds(1);

        _scoreSubject.Score = 50;
        yield return new WaitForSeconds(1);

        _scoreSubject.Score = 100; // 这里会触发成就
        yield return new WaitForSeconds(1);

        _scoreSubject.Score = 500; // 这里会触发另一个成就
    }
}
