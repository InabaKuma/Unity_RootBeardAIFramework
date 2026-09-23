using System;
using System.Collections.Generic;

namespace RootBeard.Framework
{
    public class BlackBoard
    {
        private Dictionary<string, object> data = new Dictionary<string, object>();

        public event Action<string> OnValueChanged;

        public void Set<T>(string key, T value)
        {
            data[key] = value;
            OnValueChanged?.Invoke(key);
        }

        public T Get<T>(string key)
        {
            if (data.TryGetValue(key, out object value) && value is T typedValue)
                return typedValue;
            return default;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (data.TryGetValue(key, out object obj) && obj is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public bool ContainsKey(string key) => data.ContainsKey(key);
        public void Remove(string key) => data.Remove(key);
        public void Clear() => data.Clear();
    }
}