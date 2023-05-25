using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace GameRules.Scripts.ECS.Jobs
{
    [JobProducerType(typeof(IJobParallelForBatchDeferExtensions.JobParallelForBatchDeferProducer<>))]
    public interface IJobParallelForBatchDefer
    {
        void Execute(int start, int end);
    }

    public static class IJobParallelForBatchDeferExtensions
    {
        internal struct JobParallelForBatchDeferProducer<T> where T : struct, IJobParallelForBatchDefer
        {
            static IntPtr s_JobReflectionData;

            public static unsafe IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T),
                        JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return s_JobReflectionData;
            }

            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int begin, out int end))
                        break;
                    

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif
                    jobData.Execute(begin, end);
                }
            }
        }
        
        public static unsafe JobHandle ScheduleBatch<T, U>(this T jobData, NativeList<U> list, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForBatchDefer
            where U : struct
        {
            void* atomicSafetyHandlePtr = null;
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                JobParallelForBatchDeferProducer<T>.Initialize(), dependsOn, ScheduleMode.Batched);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
#endif
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount,
                NativeListUnsafeUtility.GetInternalListDataPtrUnchecked(ref list), atomicSafetyHandlePtr);
        }
        
        public static unsafe JobHandle ScheduleBatch<T>(this T jobData, int* forEachCount, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForBatchDefer
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                JobParallelForBatchDeferProducer<T>.Initialize(), dependsOn, ScheduleMode.Batched);

            var forEachListPtr = (byte*)forEachCount - sizeof(void*);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, forEachListPtr, null);
        }
    }
}