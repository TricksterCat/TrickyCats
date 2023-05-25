using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace GameRules.Scripts.ECS.Jobs
{
    [JobProducerType(typeof(IJobNativeMultiHashMapUniqueKeysVisitWithValuesExtensions.JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<,,>))]
    public interface IJobNativeMultiHashMapUniqueKeysVisitWithValues<TKey, TValue> 
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        void Execute(TKey key, NativeArray<TValue> values);
    }

    public static class IJobNativeMultiHashMapUniqueKeysVisitWithValuesExtensions
    {
        internal struct JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<T, TKey, TValue>
            where T : struct, IJobNativeMultiHashMapUniqueKeysVisitWithValues<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            static IntPtr s_JobReflectionData;

            [ReadOnly] public NativeMultiHashMap<TKey, TValue> HashMap;
            internal T JobData;

            public static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(
                        typeof(JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<T, TKey, TValue>), typeof(T),
                        JobType.ParallelFor, (ExecuteJobFunction) Execute);
                }

                return s_JobReflectionData;
            }

            public delegate void ExecuteJobFunction(
                ref JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<T, TKey, TValue> producer, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public unsafe static void Execute(
                ref JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<T, TKey, TValue> producer, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                var hashMap = producer.HashMap;
                var hashMapData = hashMap.GetUnsafeBucketData();

                var keys = hashMapData.keys;
                
                var buckets = (int*) hashMapData.buckets;
                var next = (int*) hashMapData.next;
                
                var list = new NativeList<TValue>(256, Allocator.Temp);
                bool found;

                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                        return;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);

                            hashMap.TryGetFirstValue(key, out var value, out var it);
                            if (it.GetEntryIndex() == entryIndex)
                            {
                                list.Clear();
                                list.Add(value);
                                found = hashMap.TryGetNextValue(out value, ref it);
                                
                                while (found)
                                {
                                    list.Add(value);
                                    found = hashMap.TryGetNextValue(out value, ref it);
                                }
                                
                                producer.JobData.Execute(key, list.AsArray());
                            }

                            entryIndex = next[entryIndex];
                        }
                    }
                }
            }
        }


        public static unsafe JobHandle Schedule<T, TKey, TValue>(this T jobData, NativeMultiHashMap<TKey, TValue> hashMap, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobNativeMultiHashMapUniqueKeysVisitWithValues<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var jobProducer = new JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<T, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };
            
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer),
                JobNativeMultiHashMapUniqueKeysVisitWithValuesProducer<T, TKey, TValue>.Initialize(), 
                dependsOn, 
                ScheduleMode.Batched);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.GetUnsafeBucketData().bucketCapacityMask + 1, innerloopBatchCount);
        }
    }
}