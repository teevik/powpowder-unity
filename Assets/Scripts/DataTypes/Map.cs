using System.Collections;
using System.Collections.Generic;

namespace DataTypes
{
    public class Map<T1, T2> : IReadOnlyDictionary<T1, T2>
    {
        private readonly Dictionary<T1, T2> forward = new Dictionary<T1, T2>();
        private readonly Dictionary<T2, T1> reverse = new Dictionary<T2, T1>();

        // public Dictionary<T1, T2>.KeyCollection Keys => _forward.Keys;
        // public Dictionary<T1, T2>.ValueCollection Values => _forward.Values;

        public void Add(T1 t1, T2 t2)
        {
            forward.Add(t1, t2);
            reverse.Add(t2, t1);
        }

        public void Remove(T1 t1)
        {
            var revKey = forward[t1];
            forward.Remove(t1);
            reverse.Remove(revKey);
        }
    
        public void Remove(T2 t2)
        {
            var forwardKey = reverse[t2];
            reverse.Remove(t2);
            forward.Remove(forwardKey);
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            return forward.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) forward).GetEnumerator();
        }

        public int Count => forward.Count;

        public bool ContainsKey(T1 key)
        {
            return forward.ContainsKey(key);
        }

        public bool TryGetValue(T1 key, out T2 value)
        {
            return forward.TryGetValue(key, out value);
        }

        public T2 this[T1 key] => forward[key];

        public IEnumerable<T1> Keys => forward.Keys;

        public IEnumerable<T2> Values => forward.Values;
    }
}