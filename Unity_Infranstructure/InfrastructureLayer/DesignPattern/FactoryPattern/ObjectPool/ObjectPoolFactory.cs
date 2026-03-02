using System;
using System.Collections.Generic;
using System.Linq;

// 泛型对象池配置
[System.Serializable]
public class PoolConfig<T>
{
    public string poolName;
    public Func<T> createFunc; // 对象创建函数
    public Action<T> onGet;    // 获取对象时的回调
    public Action<T> onReturn; // 返回对象时的回调
    public Action<T> onDestroy; // 销毁对象时的回调

    public int initialSize = 10;
    public int maxSize = 50;
    public bool expandable = true;
    public ExpandMode expandMode = ExpandMode.ReuseOldest;

    // 自动清理配置
    public bool enableAutoCleanup = false;
    public float cleanupInterval = 60f; // 清理间隔（秒）
    public int minKeepCount = 5; // 最少保留数量
}

// 扩展模式枚举
public enum ExpandMode
{
    ReuseOldest,    // 复用最老的对象
    CreateNew,      // 创建新对象（无视最大限制）
    ReturnNull,     // 返回null
    DestroyOldest   // 销毁最老的对象，创建新的
}

// 池化对象接口（可选）
public interface IPoolable
{
    void OnGet();
    void OnReturn();
    DateTime LastUseTime { get; set; }
}

// 池状态信息
public class PoolStatus
{
    public string poolName;
    public int activeCount;
    public int inactiveCount;
    public int totalCount;
    public Type objectType;
    public DateTime createTime;
}

public class GenericObjectPool<T> : IDisposable
{
    private Queue<T> pool = new Queue<T>();
    private List<T> activeObjects = new List<T>();
    private PoolConfig<T> config;
    private DateTime lastCleanupTime;

    public string PoolName => config.poolName;
    public int TotalCount => activeObjects.Count + pool.Count;
    public int ActiveCount => activeObjects.Count;
    public int InactiveCount => pool.Count;
    public bool IsDisposed { get; private set; }

    public event Action<T> OnObjectCreated;
    public event Action<T> OnObjectDestroyed;
    public event Action<PoolStatus> OnPoolStatusChanged;

    public GenericObjectPool(PoolConfig<T> poolConfig)
    {
        this.config = poolConfig ?? throw new ArgumentNullException(nameof(poolConfig));
        this.lastCleanupTime = DateTime.Now;

        if (config.createFunc == null)
        {
            throw new ArgumentException("必须提供对象创建函数");
        }

        PreWarm();
    }

    public T Get()
    {
        if (IsDisposed)
            throw new ObjectDisposedException($"对象池 '{config.poolName}' 已被销毁");

        T item = default(T);
        bool isNewObject = false;

        // 1. 尝试从池中获取对象
        if (pool.Count > 0)
        {
            item = pool.Dequeue();
        }
        // 2. 池为空但可以创建新对象
        else if (TotalCount < config.maxSize)
        {
            item = CreateNewObject();
            isNewObject = true;
        }
        // 3. 超过最大数量时的处理
        else
        {
            item = HandleOverflow();
            isNewObject = item != null && !activeObjects.Contains(item);
        }

        if (item != null)
        {
            if (!isNewObject)
            {
                activeObjects.Add(item);
            }

            // 调用获取回调
            config.onGet?.Invoke(item);

            // 如果对象实现了 IPoolable 接口
            if (item is IPoolable poolable)
            {
                poolable.LastUseTime = DateTime.Now;
                poolable.OnGet();
            }

            // 触发状态变化事件
            NotifyStatusChanged();

            // 执行自动清理检查
            CheckAutoCleanup();
        }

        return item;
    }

    public void Return(T item)
    {
        if (IsDisposed || item == null) return;

        // 从活跃列表中移除
        bool wasActive = activeObjects.Remove(item);

        if (!wasActive)
        {
            // 对象可能已经被回收过了
            return;
        }

        // 处理池满情况
        if (pool.Count >= config.maxSize && !config.expandable)
        {
            DestroyObject(item);
            return;
        }

        // 调用返回回调
        config.onReturn?.Invoke(item);

        // 如果对象实现了 IPoolable 接口
        if (item is IPoolable poolable)
        {
            poolable.OnReturn();
        }

        // 放回池中
        pool.Enqueue(item);

        // 触发状态变化事件
        NotifyStatusChanged();
    }

    public void ReturnAll()
    {
        // 复制列表以避免修改集合时的枚举异常
        var objectsToReturn = activeObjects.ToList();
        foreach (var obj in objectsToReturn)
        {
            Return(obj);
        }
    }

    public void PreWarm()
    {
        int objectsToCreate = Math.Min(config.initialSize, config.maxSize) - TotalCount;

        for (int i = 0; i < objectsToCreate; i++)
        {
            T obj = CreateNewObject();
            pool.Enqueue(obj);
        }

        NotifyStatusChanged();
    }

    public void Clear()
    {
        // 销毁所有池中的对象
        foreach (var item in pool)
        {
            DestroyObject(item);
        }
        pool.Clear();

        // 销毁所有活跃对象
        foreach (var item in activeObjects)
        {
            DestroyObject(item);
        }
        activeObjects.Clear();

        NotifyStatusChanged();
    }

    public void Resize(int newMaxSize)
    {
        if (newMaxSize < 0)
            throw new ArgumentException("最大数量不能为负数");

        config.maxSize = newMaxSize;

        // 如果新的大小小于当前总数，需要销毁多余的对象
        if (TotalCount > newMaxSize)
        {
            int objectsToDestroy = TotalCount - newMaxSize;
            DestroyExcessObjects(objectsToDestroy);
        }

        NotifyStatusChanged();
    }

    public PoolStatus GetStatus()
    {
        return new PoolStatus
        {
            poolName = config.poolName,
            activeCount = ActiveCount,
            inactiveCount = InactiveCount,
            totalCount = TotalCount,
            objectType = typeof(T),
            createTime = DateTime.Now
        };
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            Clear();
            IsDisposed = true;
        }
    }

    private T CreateNewObject()
    {
        T obj = config.createFunc();

        // 触发对象创建事件
        OnObjectCreated?.Invoke(obj);

        return obj;
    }

    private void DestroyObject(T item)
    {
        config.onDestroy?.Invoke(item);
        OnObjectDestroyed?.Invoke(item);

        // 如果对象实现了 IDisposable 接口，调用 Dispose
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private T HandleOverflow()
    {
        switch (config.expandMode)
        {
            case ExpandMode.ReuseOldest:
                return GetOldestActiveObject();

            case ExpandMode.CreateNew:
                return CreateNewObject();

            case ExpandMode.ReturnNull:
                return default(T);

            case ExpandMode.DestroyOldest:
                T oldest = GetOldestActiveObject();
                if (oldest != null)
                {
                    DestroyObject(oldest);
                    activeObjects.Remove(oldest);
                }
                return CreateNewObject();

            default:
                return default(T);
        }
    }

    private T GetOldestActiveObject()
    {
        if (activeObjects.Count == 0)
            return default(T);

        // 如果对象实现了 IPoolable 接口，按使用时间排序
        if (activeObjects[0] is IPoolable)
        {
            return activeObjects
                .OrderBy(obj => ((IPoolable)obj).LastUseTime)
                .First();
        }

        // 否则返回第一个对象
        return activeObjects[0];
    }

    private void DestroyExcessObjects(int count)
    {
        int destroyCount = Math.Min(count, pool.Count);
        for (int i = 0; i < destroyCount; i++)
        {
            T obj = pool.Dequeue();
            DestroyObject(obj);
        }
    }

    private void CheckAutoCleanup()
    {
        if (!config.enableAutoCleanup) return;

        TimeSpan timeSinceCleanup = DateTime.Now - lastCleanupTime;
        if (timeSinceCleanup.TotalSeconds >= config.cleanupInterval)
        {
            PerformCleanup();
            lastCleanupTime = DateTime.Now;
        }
    }

    private void PerformCleanup()
    {
        int excessCount = pool.Count - config.minKeepCount;
        if (excessCount > 0)
        {
            DestroyExcessObjects(excessCount);
            NotifyStatusChanged();
        }
    }

    private void NotifyStatusChanged()
    {
        OnPoolStatusChanged?.Invoke(GetStatus());
    }
}