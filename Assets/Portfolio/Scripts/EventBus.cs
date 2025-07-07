using System;
using System.Collections.Generic;

using UnityEngine;

namespace LudoEvolution.Events
{
    // ===== BASE EVENT SYSTEM =====

    public abstract class BaseEvent : IEvent
    {
        public DateTime Timestamp { get; private set; }

        protected BaseEvent()
        {
            Timestamp = DateTime.Now;
        }
    }

    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : BaseEvent;

        void Unsubscribe<T>(Action<T> handler) where T : BaseEvent;

        void Publish<T>(T eventData) where T : BaseEvent;

        void Clear();
    }

    // ===== EVENT BUS IMPLEMENTATION =====

    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();
        private readonly object _lock = new object();

        public void Subscribe<T>(Action<T> handler) where T : BaseEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                var eventType = typeof(T);

                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<object>();
                }

                _handlers[eventType].Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : BaseEvent
        {
            if (handler == null) return;

            lock (_lock)
            {
                var eventType = typeof(T);

                if (_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType].Remove(handler);

                    if (_handlers[eventType].Count == 0)
                    {
                        _handlers.Remove(eventType);
                    }
                }
            }
        }

        public void Publish<T>(T eventData) where T : BaseEvent
        {
            if (eventData == null) return;

            List<object> handlers;
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    return;

                // Create a copy to avoid concurrent modification issues
                handlers = new List<object>(_handlers[eventType]);
            }

            // Execute handlers outside the lock to prevent deadlocks
            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler).Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Event handler failed for {typeof(T).Name}: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
        }
    }

    public interface IEvent
    {
    }
}