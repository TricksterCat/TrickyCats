using System;
using System.Collections.Concurrent;

namespace GameRules.Scripts.Pool
{
    public sealed class RefEnum<T> where T : Enum
    {
        private static readonly ConcurrentBag<RefEnum<T>> _pool = new ConcurrentBag<RefEnum<T>>();

        private T _value;
        private bool _isRelease;
        
        public T Value => _value;
        public bool IsRelease => _isRelease;
        
        private RefEnum(T value)
        {
            Set(value);
        }
        
        public static RefEnum<T> Create(T value)
        {
            if (!_pool.TryTake(out var result))
                result = new RefEnum<T>(value);
            else
                result.Set(value);

            result._isRelease = false;
            return result;
        }

        public void Set(T value)
        {
            if(_isRelease)
                return;
            _value = value;
        }

        public void Release()
        {
            _isRelease = true;
            _pool.Add(this);
        }
    }
}