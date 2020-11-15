using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QueueDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly LinkedList<Tuple<TKey, TValue>> queue = new LinkedList<Tuple<TKey, TValue>>();

    private readonly Dictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>> dictionary = new Dictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>>();

    public TValue Dequeue()
    {
        var (key, value) = queue.First();
        queue.RemoveFirst();
        dictionary.Remove(key);
        return value;
    }

    public TValue Dequeue(TKey key)
    {
        var node = dictionary[key];
        dictionary.Remove(key);
        queue.Remove(node);
        return node.Value.Item2;
    }

    public void Enqueue(TKey key, TValue value)
    {
        var node = queue.AddLast(new Tuple<TKey, TValue>(key, value));
        dictionary.Add(key, node);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return dictionary
            .Select(keyValuePair => new KeyValuePair<TKey, TValue>(keyValuePair.Key, keyValuePair.Value.Value.Item2))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => ((IReadOnlyDictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>>) dictionary).Count;

    public bool ContainsKey(TKey key)
    {
        return ((IReadOnlyDictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>>) dictionary).ContainsKey(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (dictionary.TryGetValue(key, out var linkedListNode))
        {
            value = linkedListNode.Value.Item2;
            return true;
        }

        value = default!;
        return false;
    }

    public TValue this[TKey key] => dictionary[key].Value.Item2;

    public IEnumerable<TKey> Keys => ((IReadOnlyDictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>>) dictionary).Keys;

    public IEnumerable<TValue> Values => dictionary.Values.Select(linkedListNode => linkedListNode.Value.Item2);
}