using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace GameRules.Scripts.ECS.Jobs
{
    [BurstCompile]
    public struct MultiHashMapSelectUniqueKeysJob<TKey, TValue> : IJobParallelForBatch
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [ReadOnly] 
        public NativeMultiHashMap<TKey, TValue> HashMap;
        [WriteOnly]
        public NativeList<TKey>.ParallelWriter UniqueKeys;
        
        public unsafe void Execute(int startIndex, int count)
        {
            var bucketData = HashMap.GetUnsafeBucketData();
            var buckets = (int*)bucketData.buckets;
            var next = (int*)bucketData.next;
            var keys = bucketData.keys;
            
            for (int i = startIndex, iMax = startIndex + count; i < iMax; i++)
            {
                int entryIndex = buckets[i];
                
                while (entryIndex != -1)
                {
                    var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                    HashMap.TryGetFirstValue(key, out _, out var it);
                
                    if (entryIndex == it.GetEntryIndex())
                        UniqueKeys.AddNoResize(key);

                    entryIndex = next[entryIndex];
                }
            }
        }
    }
    
    [BurstCompile]
    public struct MultiHashMapSelectUniqueKeysDefferJob<TKey, TValue> : IJobParallelForBatchDefer
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [ReadOnly] 
        public NativeMultiHashMap<TKey, TValue> HashMap;
        [WriteOnly]
        public NativeList<TKey>.ParallelWriter UniqueKeys;
        
        public unsafe void Execute([AssumeRange(0, Int32.MaxValue)] int i, [AssumeRange(0, Int32.MaxValue)] int end)
        {
            var bucketData = HashMap.GetUnsafeBucketData();
            var buckets = (int*)bucketData.buckets;
            var next = (int*)bucketData.next;
            var keys = bucketData.keys;
            
            for (; i < end; i++)
            {
                int entryIndex = buckets[i];
                
                while (entryIndex != -1)
                {
                    var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                    HashMap.TryGetFirstValue(key, out _, out var it);
                
                    if (entryIndex == it.GetEntryIndex())
                        UniqueKeys.AddNoResize(key);

                    entryIndex = next[entryIndex];
                }
            }
        }
    }
}