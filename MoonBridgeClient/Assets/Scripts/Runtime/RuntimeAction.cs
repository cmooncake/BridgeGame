using System;

namespace MoonBridge.Runtime
{
    public interface IRuntimeAction
    {
    }

    public sealed class RuntimeAction<T> : IRuntimeAction
    {
        private Action<T> callbacks;

        public void Add(Action<T> callback)
        {
            callbacks += callback;
        }

        public void Remove(Action<T> callback)
        {
            callbacks -= callback;
        }

        public void Emit(T payload)
        {
            var snapshot = callbacks;
            if (snapshot != null)
            {
                snapshot.Invoke(payload);
            }
        }

        public static RuntimeAction<T> operator +(RuntimeAction<T> action, Action<T> callback)
        {
            if (action == null)
            {
                action = new RuntimeAction<T>();
            }

            action.Add(callback);
            return action;
        }

        public static RuntimeAction<T> operator -(RuntimeAction<T> action, Action<T> callback)
        {
            if (action == null)
            {
                return null;
            }

            action.Remove(callback);
            return action;
        }
    }
}
