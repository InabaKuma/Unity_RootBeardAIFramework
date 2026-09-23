using System.Collections.Generic;

namespace RootBeard.Framework
{
    /// <summary>
    /// 行为树共享数据容器，节点可以通过它访问 AI 的各种信息。
    /// </summary>
    public class BlackBoard
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();

        public void Set<T>(string key, T value) => data[key] = value;

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