using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : SingletonMonoBehaviour<EventBus>
{
    private readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
            return;

        Type eventType = typeof(TEvent);
        _handlers.TryGetValue(eventType, out Delegate existingHandlers);
        _handlers[eventType] = Delegate.Combine(existingHandlers, handler);
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null)
            return;

        Type eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out Delegate existingHandlers))
            return;

        Delegate remainingHandlers = Delegate.Remove(existingHandlers, handler);
        if (remainingHandlers == null)
            _handlers.Remove(eventType);
        else
            _handlers[eventType] = remainingHandlers;
    }

    public void Publish<TEvent>(TEvent gameEvent)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out Delegate handlers))
            return;

        foreach (Delegate handler in handlers.GetInvocationList())
            ((Action<TEvent>)handler).Invoke(gameEvent);
    }
}
