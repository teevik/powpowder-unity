using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QueueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly LinkedList<Tuple<TKey, TValue>> queue = new LinkedList<Tuple<TKey, TValue>>();

    private readonly Dictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>> dictionary = new Dictionary<TKey, LinkedListNode<Tuple<TKey, TValue>>>();
    
    public TValue Dequeue()
    {
        Tuple<TKey, TValue> item = queue.First();
        queue.RemoveFirst();
        dictionary.Remove(item.Item1);
        return item.Item2;
    }

    public TValue Dequeue(TKey key)
    {
        LinkedListNode<Tuple<TKey, TValue>> node = dictionary[key];
        dictionary.Remove(key);
        queue.Remove(node);
        return node.Value.Item2;
    }

    public void Enqueue(TKey key, TValue value)
    {
        LinkedListNode<Tuple<TKey, TValue>> node = 
            queue.AddLast(new Tuple<TKey, TValue>(key, value));
        dictionary.Add(key, node);
    }

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<TKey, TValue>>) dictionary).GetEnumerator();
    }

    public IEnumerator GetEnumerator()
    {
        return ((IEnumerable) dictionary).GetEnumerator();
    }
}