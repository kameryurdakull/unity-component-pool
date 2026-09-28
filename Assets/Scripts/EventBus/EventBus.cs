using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using VContainer;

namespace EventSystem
{
    public class EventBus : IEventBus
    {
        private sealed class Subscribers<T>
        {
            public Action<T>[] Snapshot { get; private set; } = Array.Empty<Action<T>>();

            public void Add(Action<T> callback)
            {
                var current = Snapshot;
                var updated = new Action<T>[current.Length + 1];
                Array.Copy(current, updated, current.Length);
                updated[current.Length] = callback;
                Snapshot = updated;
            }

            public void Remove(Action<T> callback)
            {
                var current = Snapshot;
                var index = Array.IndexOf(current, callback);
                if (index < 0) return;

                if (current.Length == 1)
                {
                    Snapshot = Array.Empty<Action<T>>();
                    return;
                }

                var updated = new Action<T>[current.Length - 1];
                Array.Copy(current, 0, updated, 0, index);
                Array.Copy(current, index + 1, updated, index, current.Length - index - 1);
                Snapshot = updated;
            }
        }

        private readonly Dictionary<Type, object> _subscribers = new();

        [Inject]
        public EventBus()
        {
        }

        public void Subscribe<T>(Action<T> callback)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var subscribers))
            {
                subscribers = new Subscribers<T>();
                _subscribers.Add(typeof(T), subscribers);
            }

            ((Subscribers<T>)subscribers).Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback)
        {
            if (_subscribers.TryGetValue(typeof(T), out var subscribers))
                ((Subscribers<T>)subscribers).Remove(callback);
        }

        public async UniTask PublishAsync<T>(T eventData)
        {
            Publish(eventData);
            await UniTask.Yield();
        }

        public void Publish<T>(T eventMessage)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var subscribers)) return;

            // Keep the current array stable if a callback changes subscriptions during dispatch.
            var snapshot = ((Subscribers<T>)subscribers).Snapshot;
            for (var index = 0; index < snapshot.Length; index++)
            {
                snapshot[index]?.Invoke(eventMessage);
            }
        }
    }
}
