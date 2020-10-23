using System.Collections.Generic;
using JetBrains.Annotations;

namespace Extensions
{
    public static class DictionaryExtensions
    {
        [CanBeNull]
        public static TValue TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TValue: class
        {
            if (dictionary.TryGetValue(key, out var value)) return value;
            return null;
        }
    }
}