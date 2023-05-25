using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace GameRules.Scripts.Extensions
{
    public static class NativeKeyValueArraysExtensions
    {
        private struct DefaultComparer<T> : IComparer<T> where T : IComparable<T>
        {
            public int Compare(T x, T y) => x.CompareTo(y);
        }
        
        public static unsafe void Sort<TKey, TValue>(this NativeKeyValueArrays<TKey, TValue> array)
            where TKey : struct, IComparable<TKey>
            where TValue : struct
        {
            var keysPtr = array.Keys.GetUnsafePtr();
            var valuesPtr = array.Values.GetUnsafePtr();

            var count = array.Keys.Length;
            var comparer = new DefaultComparer<TKey>();
            
            IntroSort<TKey, TValue, DefaultComparer<TKey>>(keysPtr, valuesPtr, 0, count-1, 2 * CollectionHelper.Log2Floor(count), comparer);
        }
        
        const int k_IntrosortSizeThreshold = 16;
        private static unsafe void IntroSort<TKey, TValue, U>(void* keys, void* values, int lo, int hi, int depth, U comp) 
            where TKey : struct
            where TValue : struct 
            where U : IComparer<TKey>
        {
            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= k_IntrosortSizeThreshold)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, lo, hi, comp);
                        return;
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, lo, hi - 1, comp);
                        SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, lo, hi, comp);
                        SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, hi - 1, hi, comp);
                        return;
                    }

                    InsertionSort<TKey, TValue, U>(keys, values, lo, hi, comp);
                    return;
                }

                if (depth == 0)
                {
                    HeapSort<TKey, TValue, U>(keys, values, lo, hi, comp);
                    return;
                }
                depth--;

                int p = Partition<TKey, TValue, U>(keys, values, lo, hi, comp);
                IntroSort<TKey, TValue, U>(keys, values, p + 1, hi, depth, comp);
                hi = p - 1;
            }
        }
        
        private static unsafe int Partition<TKey, TValue, U>(void* keys, void* values, int lo, int hi, U comp) 
            where TKey : struct
            where TValue : struct 
            where U : IComparer<TKey>
        {
            int mid = lo + ((hi - lo) / 2);
            SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, lo, mid, comp);
            SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, lo, hi, comp);
            SwapIfGreaterWithItems<TKey, TValue, U>(keys, values, mid, hi, comp);

            TKey pivot = UnsafeUtility.ReadArrayElement<TKey>(keys, mid);
            Swap<TKey>(keys, mid, hi - 1);
            Swap<TValue>(values, mid, hi - 1);
            int left = lo, right = hi - 1;

            while (left < right)
            {
                while (comp.Compare(pivot, UnsafeUtility.ReadArrayElement<TKey>(keys, ++left)) > 0) ;
                while (comp.Compare(pivot, UnsafeUtility.ReadArrayElement<TKey>(keys, --right)) < 0) ;

                if (left >= right)
                    break;

                Swap<TKey>(keys, left, right);
                Swap<TValue>(values, left, right);
            }

            Swap<TKey>(keys, left, (hi - 1));
            Swap<TValue>(values, left, (hi - 1));
            return left;
        }
        
        private static unsafe void HeapSort<TKey, TValue, U>(void* keys, void* values, int lo, int hi, U comp) 
            where TKey : struct
            where TValue : struct 
            where U : IComparer<TKey>
        {
            int n = hi - lo + 1;

            for (int i = n / 2; i >= 1; i--)
            {
                Heapify<TKey, TValue, U>(keys, values, i, n, lo, comp);
            }

            for (int i = n; i > 1; i--)
            {
                Swap<TKey>(keys, lo, lo + i - 1);
                Swap<TValue>(values, lo, lo + i - 1);
                Heapify<TKey, TValue, U>(keys, values, 1, i - 1, lo, comp);
            }
        }

        
        private static unsafe void Heapify<TKey, TValue, U>(void* keys, void* values, int i, int n, int lo, U comp) 
            where TKey : struct
            where TValue : struct 
            where U : IComparer<TKey>
        {
            TKey val = UnsafeUtility.ReadArrayElement<TKey>(keys, lo + i - 1);
            TValue valV = UnsafeUtility.ReadArrayElement<TValue>(values, lo + i - 1);
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && (comp.Compare(UnsafeUtility.ReadArrayElement<TKey>(keys, lo + child - 1), UnsafeUtility.ReadArrayElement<TKey>(keys, (lo + child))) < 0))
                {
                    child++;
                }
                if (comp.Compare(UnsafeUtility.ReadArrayElement<TKey>(keys, (lo + child - 1)), val) < 0)
                    break;

                UnsafeUtility.WriteArrayElement(keys, lo + i - 1, UnsafeUtility.ReadArrayElement<TKey>(keys, lo + child - 1));
                UnsafeUtility.WriteArrayElement(values, lo + i - 1, UnsafeUtility.ReadArrayElement<TValue>(values, lo + child - 1));
                i = child;
            }
            UnsafeUtility.WriteArrayElement(keys, lo + i - 1, val);
            UnsafeUtility.WriteArrayElement(values, lo + i - 1, valV);
        }

        
        private static unsafe void InsertionSort<TKey, TValue, U>(void* keys, void* values, int lo, int hi, U comp) 
            where TKey : struct 
            where TValue : struct 
            where U : IComparer<TKey>
        {
            int i, j;
            TKey t;
            TValue v;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = UnsafeUtility.ReadArrayElement<TKey>(keys, i + 1);
                v = UnsafeUtility.ReadArrayElement<TValue>(values, i + 1);
                while (j >= lo && comp.Compare(t, UnsafeUtility.ReadArrayElement<TKey>(keys, j)) < 0)
                {
                    UnsafeUtility.WriteArrayElement(keys, j + 1, UnsafeUtility.ReadArrayElement<TKey>(keys, j));
                    UnsafeUtility.WriteArrayElement(values, j + 1, UnsafeUtility.ReadArrayElement<TValue>(values, j));
                    j--;
                }
                UnsafeUtility.WriteArrayElement(keys, j + 1, t);
                UnsafeUtility.WriteArrayElement(values, j + 1, v);
            }
        }
        
        
        private static unsafe void SwapIfGreaterWithItems<TKey, TValue, U>(void* keys, void* values, int lhs, int rhs, U comp) 
            where TKey : struct 
            where TValue : struct 
            where U : IComparer<TKey>
        {
            if (lhs != rhs)
            {
                if (comp.Compare(UnsafeUtility.ReadArrayElement<TKey>(keys, lhs), UnsafeUtility.ReadArrayElement<TKey>(keys, rhs)) > 0)
                {
                    Swap<TKey>(keys, lhs, rhs);
                    Swap<TValue>(values, lhs, rhs);
                }
            }
        }
        
        
        private static unsafe void Swap<T>(void* array, int lhs, int rhs) where T : struct
        {
            T val = UnsafeUtility.ReadArrayElement<T>(array, lhs);
            UnsafeUtility.WriteArrayElement(array, lhs, UnsafeUtility.ReadArrayElement<T>(array, rhs));
            UnsafeUtility.WriteArrayElement(array, rhs, val);
        }
    }
}