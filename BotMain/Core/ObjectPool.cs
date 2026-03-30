
namespace BotMain.Core;

/// <summary>
/// 简单的线程安全对象池，容量固定，超出容量时直接丢弃归还的对象
/// </summary>
internal sealed class ObjectPool<T> where T : class
{
    private const int c_DefaultCapacity = 4;

    private readonly Func<T> _factory;
    private readonly T?[] _pool;
    private int _count;
    private readonly Lock _lock = new();

    internal ObjectPool(Func<T> factory, int capacity = c_DefaultCapacity)
    {
        _factory = factory;
        _pool = new T?[capacity];
    }

    internal T Rent()
    {
        lock (_lock)
        {
            if (_count > 0)
            {
                var obj = _pool[--_count]!;
                _pool[_count] = null;
                return obj;
            }
        }
        return _factory();
    }

    internal void Return(T obj)
    {
        lock (_lock)
        {
            if (_count < _pool.Length)
                _pool[_count++] = obj;
        }
    }
}
