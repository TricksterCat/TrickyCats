using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace GameRules.Scripts.ECS
{
    /// <summary>
    /// NativeElement is a wrapper for a single blittable element to be passed between managed and unmanaged code in jobs
    /// </summary>
    /// <typeparam name="T">The blittable type to be stored</typeparam>
    [NativeContainer]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{Value}")]
    public struct NativeElement<T> : IDisposable where T : unmanaged
    {
        // The pointer to the element
        [NativeDisableUnsafePtrRestriction]
        unsafe void* m_ptr;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // Safety
        AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
        
        // The allocator label
        Allocator m_AllocatorLabel;

        // Constructor which only takes a label and then calls the main constructor with an instance of T
        public NativeElement(Allocator label) : this(new T(), label) { }

        // Main constructor logic that takes an element and a label
        public unsafe NativeElement(T element, Allocator label)
        {
    #if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Label check
            if (label <= Allocator.None)
                throw new ArgumentException("NativeElement must be allocated using Job, TempJob or Persistent");
    #endif
            IsUnmanagedAndThrow();

            // Label set
            m_AllocatorLabel = label;

            // Allocate memory for a single T
            m_ptr = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), 1, label);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, label);    
#endif
            // Create element to avoid unitialised data
            Value = element;
        }
        
        [BurstDiscard]
        internal static void IsUnmanagedAndThrow()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
                throw new InvalidOperationException(string.Format("{0} used in NativeElement<{1}> must be unmanaged (contain no managed types) and cannot itself be a native container type.", (object) typeof (T), (object) typeof (T)));
        }

        // Property for the Element stored
        public unsafe T Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                
                // Return the element
                return *(T*)m_ptr;
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Write check
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                
                // Set the element
                *(T*)m_ptr = value;
            }
        }

        // Dispose of all resources
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            Deallocate();
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);  
#endif
            JobHandle jobHandle = new DisposeJob
            {
                Container = this
            }.Schedule(inputDeps);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(this.m_Safety);
#endif
            this.m_ptr = null;
            return jobHandle;
        }
        

        private struct DisposeJob : IJob
        {
            public NativeElement<T> Container;

            public void Execute()
            {
                this.Container.Deallocate();
            }
        }
        
        private unsafe void Deallocate()
        {
            UnsafeUtility.Free(this.m_ptr, this.m_AllocatorLabel);
            this.m_ptr =  (void*) null;
        }
    }
}