using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Kattbot.Services.Cache;
public abstract class CacheQueue<T>
{
    private readonly int _maxSize;
    private readonly TimeSpan _maxAge;

    private readonly ConcurrentQueue<(T Item, DateTime Expiration)> _queue;

    public CacheQueue(int maxSize, TimeSpan maxAge)
    {
        _maxSize = maxSize;
        _maxAge = maxAge;

        _queue = new ConcurrentQueue<(T, DateTime)>();
    }

    public void Enqueue(T item)
    {
        RemoveExpiredItems();

        if (_queue.Count >= _maxSize)
        {
            _ = _queue.TryDequeue(out _);
        }

        _queue.Enqueue((item, DateTime.UtcNow.Add(_maxAge)));
    }

    public IEnumerable<T> GetAll()
    {
        RemoveExpiredItems();

        return _queue.ToList().Select(l => l.Item);
    }

    private void RemoveExpiredItems()
    {
        if (_queue.IsEmpty) return;

        while (_queue.TryPeek(out var peakItem) && peakItem.Expiration < DateTime.UtcNow)
        {
            _ = _queue.TryDequeue(out _);
        }
    }
}
