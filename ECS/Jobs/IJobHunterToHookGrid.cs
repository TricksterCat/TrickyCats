using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.Jobs
{
    public ref struct HookAndHunters
    {
        public unsafe Entity* Hooks;
        public int HooksCount;
        
        public unsafe Entity* Hunters;
        public int HuntersCount;
    }
    
    [JobProducerType(typeof(IJobHunterToHookGridExtensions.JobHunterToHookGridProducer<>))]
    public interface IJobHunterToHookGrid
    {
        void ExecuteCenter(ref HookAndHunters data);
        void ExecuteBorder(ref HookAndHunters center, ref HookAndHunters border);
    }

    public static class IJobHunterToHookGridExtensions
    {
        internal struct JobHunterToHookGridProducer<T>
            where T : struct, IJobHunterToHookGrid
        {
            static IntPtr s_JobReflectionData;

            [ReadOnly] public NativeMultiHashMap<int2, Entity> HooksMap;
            [ReadOnly] public NativeMultiHashMap<int2, Entity> HuntersMap;
            
            internal T JobData;

            public static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(
                        typeof(JobHunterToHookGridProducer<T>), typeof(T),
                        JobType.ParallelFor, (ExecuteJobFunction) Execute);
                }

                return s_JobReflectionData;
            }

            public delegate void ExecuteJobFunction(
                ref JobHunterToHookGridProducer<T> producer, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public unsafe static void Execute(
                ref JobHunterToHookGridProducer<T> producer, IntPtr additionalPtr,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                var hashMap = producer.HooksMap;
                var hashMapData = hashMap.GetUnsafeBucketData();

                var hunters = producer.HuntersMap;
                
                var keys = (int2*)hashMapData.keys;
                
                var buckets = (int*) hashMapData.buckets;
                var next = (int*) hashMapData.next;
                
                var hooks = (Entity*)UnsafeUtility.Malloc(128 * UnsafeUtility.SizeOf<Entity>(), UnsafeUtility.AlignOf<Entity>(), Allocator.Temp);
                var hunter = (Entity*)UnsafeUtility.Malloc(128 * UnsafeUtility.SizeOf<Entity>(), UnsafeUtility.AlignOf<Entity>(), Allocator.Temp);

                var center = new HookAndHunters
                {
                    Hooks = hooks,
                    Hunters = hunter
                };
                
                var border = new HookAndHunters
                {
                    Hooks =  (Entity*)UnsafeUtility.Malloc(128 * UnsafeUtility.SizeOf<Entity>(), UnsafeUtility.AlignOf<Entity>(), Allocator.Temp),
                    Hunters = (Entity*)UnsafeUtility.Malloc(128 * UnsafeUtility.SizeOf<Entity>(), UnsafeUtility.AlignOf<Entity>(), Allocator.Temp)
                };

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
                            var key = keys[entryIndex];

                            hashMap.TryGetFirstValue(key, out var value, out var it);
                            if (it.GetEntryIndex() == entryIndex)
                            {
                                var count = 1;
                                hooks[0] = value;
                                var found = hashMap.TryGetNextValue(out value, ref it);
                                
                                while (found)
                                {
                                    hooks[count++] = value;
                                    found = hashMap.TryGetNextValue(out value, ref it);
                                }

                                var hunterCount = 0;
                                found = hunters.TryGetFirstValue(key,out value, out it);
                                while (found)
                                {
                                    hunter[hunterCount++] = value;
                                    found = hunters.TryGetNextValue(out value, ref it);
                                }

                                center.HooksCount = count;
                                center.HooksCount = hunterCount;
                                
                                if(count != 1)
                                    producer.JobData.ExecuteCenter(ref center);

                                if (key.x % 2 == key.y % 2)
                                {
                                    //FillBorder(ref producer, values, in count, bValues, key + new int2(-1, -1));
                                    FillBorder(ref producer, key + new int2(-1, 0), ref center, ref border);
                                    //FillBorder(ref producer, values, in count, bValues, key + new int2(-1, 1));
                                    FillBorder(ref producer, key + new int2(0, -1), ref center, ref border);
                                    FillBorder(ref producer, key + new int2(0, 1), ref center, ref border);
                                    FillBorder(ref producer, key + new int2(1, -1), ref center, ref border);
                                    FillBorder(ref producer, key + new int2(1, 0), ref center, ref border);
                                    FillBorder(ref producer, key + new int2(1, 1), ref center, ref border);
                                }
                            }

                            entryIndex = next[entryIndex];
                        }
                    }
                }
            }
            
            [BurstCompile]
            private static unsafe void FillBorder(ref JobHunterToHookGridProducer<T> producer, int2 key, ref HookAndHunters center, ref HookAndHunters border)
            {
                var found = producer.HooksMap.TryGetFirstValue(key, out var other, out var it);
                border.HooksCount = 0;
                border.HuntersCount = 0;
                int bCount;
                if (found)
                {
                    bCount = 0;
                    while (found)
                    {
                        border.Hooks[bCount++] = other;
                        found = producer.HooksMap.TryGetNextValue(out other, ref it);
                    }
                    border.HooksCount = bCount;
                }
                
                found = producer.HuntersMap.TryGetFirstValue(key, out other, out it);
                if (found)
                {
                    bCount = 0;
                    while (found)
                    {
                        border.Hunters[bCount++] = other;
                        found = producer.HuntersMap.TryGetNextValue(out other, ref it);
                    }
                    border.HooksCount = bCount;
                }
                
                producer.JobData.ExecuteBorder(ref center, ref border);
            }
        }


        public static unsafe JobHandle Schedule<T>(this T jobData, NativeMultiHashMap<int2, Entity> hooksMap, NativeMultiHashMap<int2, Entity> hunterMap, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobHunterToHookGrid
        {
            var jobProducer = new JobHunterToHookGridProducer<T>
            {
                HooksMap = hooksMap,
                HuntersMap = hunterMap,
                JobData = jobData
            };
            
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobProducer),
                JobHunterToHookGridProducer<T>.Initialize(), 
                dependsOn, 
                ScheduleMode.Batched);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hooksMap.GetUnsafeBucketData().bucketCapacityMask + 1, innerloopBatchCount);
        }
    }
}