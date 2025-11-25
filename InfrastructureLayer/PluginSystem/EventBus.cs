using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

// 事件总线完整实现
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new ConcurrentDictionary<Type, List<Delegate>>();
    private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _asyncHandlers = new ConcurrentDictionary<Type, List<Func<object, Task>>>();

    public void Publish<TEvent>(TEvent eventData)
    {
        if (eventData == null) return;

        var eventType = typeof(TEvent);

        // 同步处理器
        if (_handlers.TryGetValue(eventType, out var syncHandlers))
        {
            foreach (var handler in syncHandlers.ToArray())
            {
                try
                {
                    ((Action<TEvent>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in event handler for {eventType.Name}: {ex.Message}");
                }
            }
        }

        // 异步处理器
        if (_asyncHandlers.TryGetValue(eventType, out var asyncHandlers))
        {
            Task.Run(async () =>
            {
                foreach (var handler in asyncHandlers.ToArray())
                {
                    try
                    {
                        await handler(eventData);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in async event handler for {eventType.Name}: {ex.Message}");
                    }
                }
            });
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        var eventType = typeof(TEvent);
        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return new EventSubscription<TEvent>(this, handler);
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        var asyncHandlers = _asyncHandlers.GetOrAdd(eventType, _ => new List<Func<object, Task>>());
        lock (asyncHandlers)
        {
            asyncHandlers.Add(evt => handler((TEvent)evt));
        }

        return new EventSubscription<TEvent>(this, handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
    {
        if (_asyncHandlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(evt => handler((TEvent)evt));
            }
        }
    }

    // 事件订阅
    private class EventSubscription<TEvent> : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly Action<TEvent> _syncHandler;
        private readonly Func<TEvent, Task> _asyncHandler;

        public EventSubscription(EventBus eventBus, Action<TEvent> handler)
        {
            _eventBus = eventBus;
            _syncHandler = handler;
        }

        public EventSubscription(EventBus eventBus, Func<TEvent, Task> handler)
        {
            _eventBus = eventBus;
            _asyncHandler = handler;
        }

        public void Dispose()
        {
            if (_syncHandler != null)
                _eventBus.Unsubscribe<TEvent>(_syncHandler);
            if (_asyncHandler != null)
                _eventBus.Unsubscribe<TEvent>(_asyncHandler);
        }
    }
}