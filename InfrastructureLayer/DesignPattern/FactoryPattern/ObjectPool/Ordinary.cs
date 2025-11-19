using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class, new()
{
    private Queue<T> pool = new Queue<T>();
    private System.Func<T> createFunc;

    public ObjectPool(System.Func<T> createFunction, int initialSize = 10)
    {
        this.createFunc = createFunction;
        PreWarm(initialSize);
    }

    private void PreWarm(int size)
    {
        for (int i = 0; i < size; i++)
        {
            T obj = createFunc();
            pool.Enqueue(obj);
        }
    }

    public T Get()
    {
        
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        return createFunc();
    }

    public void Return(T obj)
    {
        pool.Enqueue(obj);
    }

    public int GetPoolSize()
    {
        return pool.Count;
    }
}