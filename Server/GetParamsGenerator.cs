using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace GameRules.Scripts.Server
{
    public class GetParamsGenerator
    {
        private static readonly ConcurrentBag<GetParamsGenerator> _bag = new ConcurrentBag<GetParamsGenerator>();
        private readonly StringBuilder _stringBuilder = new StringBuilder(256);

        private int _count;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GetParamsGenerator Begin()
        {
            if (_bag.TryTake(out var result))
                result.Clear();
            else
                result = new GetParamsGenerator();
                
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GetParamsGenerator Add(string key, string value)
        {
            var preChar = _count == 0 ? '?' : '&';

            _stringBuilder.Append(preChar);
            _stringBuilder.Append(key);
            _stringBuilder.Append('=');
            _stringBuilder.Append(value);

            _count++;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GetParamsGenerator Add(string key, int value)
        {
            var preChar = _count == 0 ? '?' : '&';

            _stringBuilder.Append(preChar);
            _stringBuilder.Append(key);
            _stringBuilder.Append('=');
            _stringBuilder.Append(value);

            _count++;
            return this;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Uri Release(string uri)
        {
            _stringBuilder.Insert(0, uri);
            var result = new Uri(_stringBuilder.ToString());
            
            Free();
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear()
        {
            _count = 0;
            _stringBuilder.Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Free()
        {
            _bag.Add(this);
        }
    }
}