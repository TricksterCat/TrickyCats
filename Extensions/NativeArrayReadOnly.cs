using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace GameRules.Scripts.Extensions
{
    public struct NativeArrayReadOnly<T> where T : unmanaged
    {
        private NativeArray<T> _array;

        public NativeArrayReadOnly(NativeArray<T> array)
        {
            _array = array;
        }

        public T this[int index] => _array[index];

        public unsafe T* GetUnsafeReadOnlyPtr()
        {
            return (T*)_array.GetUnsafeReadOnlyPtr();
        }
    }
}