using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace GameRules.Scripts.ECS.Render.Static
{    
    [ExecuteAlways]
#if ENABLE_HYBRID_RENDERER
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup)]
#else
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(UnitRenderSystem))]
#endif
    public class StaticRendererSystem : SystemBase
    {
        private class RenderGroup
        {
            public Entity Entity => _entity;
            
            private readonly StaticRendererSystem _owner;
            private readonly BatchRendererGroup _batchGroups;
            
            private Entity _entity;
            private NativeArray<int> _batches;
            private NativeArray<AABB> _aabbs;
            
            private bool _isDisposable;

            public unsafe RenderGroup(StaticRendererSystem owner, StaticMeshGroup group, 
                Entity entity, EntityManager entityManager)
            {
                _owner = owner;
                _entity = entity;
                
                var resources = entityManager.GetComponentData<StaticMeshGroupResources>(entity).Assets;
                ref var groups = ref group.Data.Value.Values;
                
                _batchGroups = new BatchRendererGroup(CullingCallback);
                _batches = new NativeArray<int>(groups.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                int aabbsCount = 0;
                for (int i = 0; i < groups.Length; i++)
                {
                    ref var batchInfo = ref groups[i];
                    aabbsCount += batchInfo.Bounds.Length;
                }
                _aabbs = new NativeArray<AABB>(aabbsCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                var aabbUnsafe = (AABB*)_aabbs.GetUnsafePtr();

                aabbsCount = 0;
                for (int i = 0; i < groups.Length; i++)
                {
                    ref var batchInfo = ref groups[i];
            
                    var count = batchInfo.Positions.Length;
                    var managed = batchInfo.Managed;
                    var batchIndex = _batchGroups.AddBatch((Mesh)resources[managed.Mesh], batchInfo.Managed.SubMeshIndex, (Material)resources[managed.Material], 0,
                        ShadowCastingMode.Off, false, false, new Bounds(Vector3.zero, Vector3.one * 2000),
                        count, null, null);
                
                    UnsafeUtility.MemCpy(_batchGroups.GetBatchMatrices(batchIndex).GetUnsafePtr(), batchInfo.Positions.GetUnsafePtr(), UnsafeUtility.SizeOf<Matrix4x4>() * count);
                    UnsafeUtility.MemCpy(aabbUnsafe + aabbsCount, batchInfo.Bounds.GetUnsafePtr(), UnsafeUtility.SizeOf<AABB>() * count);
                    
                    _batches[batchIndex] = aabbsCount;
                    aabbsCount += count;
                }
            }


            public bool IsDestroy(EntityManager entityManager)
            {
                return !entityManager.Exists(_entity);
            }
            
            public void Dispose(ref JobHandle handle)
            {
                handle = _batches.Dispose(handle);
                handle = _aabbs.Dispose(handle);
                
                _batchGroups.Dispose();
                _isDisposable = true;
            }
            
            private unsafe JobHandle CullingCallback(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
            {
                if (_isDisposable || !_owner.Enabled)
                    return default;
                
                var batches = cullingContext.batchVisibility;
                var batchCount = batches.Length;
                if (batchCount == 0)
                    return default;
            
                return new CullingAndFill_BatchJob
                {
                    Planes = CullingHelper.BuildSOAPlanePackets(cullingContext.cullingPlanes, Allocator.TempJob),
                    AABBs = (AABB*)_aabbs.GetUnsafeReadOnlyPtr(),
                    Batches = _batches,
                    BatchVisibility = (BatchVisibility*)batches.GetUnsafePtr(),
                    VisibleIndices = (int*)cullingContext.visibleIndices.GetUnsafePtr()
                }.Schedule(batchCount, 8, _owner.Dependency);
            }
        }
        
        private readonly List<RenderGroup> _groups = new List<RenderGroup>();
        private NativeHashMap<Entity, StaticMeshGroup> _groupsData;
        
        private int _batchesCount;
        public int BatchesCount => _batchesCount;
        
        private ProfilerMarker _completeLastFrameInStaticRendererSystem = new ProfilerMarker("LastFrameInStaticRendererSystem");
        private JobHandle _last;
        
        protected override void OnCreate()
        {
            _groupsData = new NativeHashMap<Entity, StaticMeshGroup>(64, Allocator.Persistent);
            RequireForUpdate(GetEntityQuery(ComponentType.ReadOnly<StaticMeshGroup>()));
        }

        protected override void OnDestroy()
        {
            _batchesCount = 0;
            
            var handle = Dependency;
            foreach (var renderGroup in _groups)
                renderGroup.Dispose(ref handle);
            Dependency = _groupsData.Dispose(handle);
            _groups.Clear();
        }
        
        [BurstCompile]
        private unsafe struct CullingAndFill_BatchJob : IJobParallelFor
        {
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<PlanePacket4> Planes;
            [ReadOnly]
            public NativeArray<int> Batches;
            [NativeDisableUnsafePtrRestriction]
            public AABB* AABBs;

            [NativeDisableUnsafePtrRestriction]
            public BatchVisibility* BatchVisibility;
            [NativeDisableUnsafePtrRestriction]
            public int* VisibleIndices;

            [return:AssumeRange(0u, 1024u)]
            private static int InRange([AssumeRange(0, 1024u)]int value)
            {
                return value;
            }

            public void Execute([AssumeRange(0, 1024u)]int batch)
            {
                if(!Batches.IsCreated)
                    return;
                var batchBounds = AABBs + Batches[batch];
                
                var batchVisibility = BatchVisibility + batch;
                int count = InRange(batchVisibility->instancesCount);
                var start = VisibleIndices + batchVisibility->offset;
                
                int index = 0;
                for (int i = 0; i < count; i++)
                {
                    if (CullingHelper.IsVisible(Planes, batchBounds[i]))
                        *(start + index++) = i;
                }
                batchVisibility->visibleCount = index;
            }
        }

        protected override void OnStopRunning()
        {
            _groupsData.Clear();

            var handle = Dependency;
            foreach (var renderGroup in _groups)
                renderGroup.Dispose(ref handle);
            _groups.Clear();
            Dependency = handle;
            
            _batchesCount = 0;
        }

        protected override void OnUpdate()
        {
            _last.Complete();

            var commandBuffer = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            _completeLastFrameInStaticRendererSystem.Begin();
            
            var groups = _groupsData;
            if (groups.Count() > 0)
            {
                var newBatches = groups.GetKeyArray(Allocator.Temp);
                for (int i = newBatches.Length - 1; i >= 0; i--)
                {
                    var entity = newBatches[i];
                    if (AddBatch(entity))
                        groups.Remove(entity);
                }
                newBatches.Dispose();
            }
            
            var em = EntityManager;
            var handle = Dependency;
            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                var group = _groups[i];
                if (group.IsDestroy(em))
                {
                    group.Dispose(ref handle);
                    _groups.RemoveAtSwapBack(i);
                    _groupsData.Remove(group.Entity);
                }
            }

            _batchesCount = _groups.Count;
            _completeLastFrameInStaticRendererSystem.End();
            
            var buffer = commandBuffer.CreateCommandBuffer();
            Dependency = Entities.WithName("GetNewBatchGroupsJob").WithNone<InjectToRendererSystemTag>().ForEach((in Entity entity, in StaticMeshGroup rendererGroup) =>
            {
                groups.TryAdd(entity, rendererGroup);
                buffer.AddComponent(entity, ComponentType.ReadOnly<InjectToRendererSystemTag>());
            }).Schedule(handle);
            commandBuffer.AddJobHandleForProducer(Dependency);
            
            _last = Dependency;
        }


        private bool AddBatch(Entity entity)
        {
            if(!EntityManager.Exists(entity))
                return false;

            if (!_groupsData.TryGetValue(entity, out var group) || !group.Data.IsCreated || group.Data == BlobAssetReference<GroupInfosArray>.Null)
                return false;

            _groups.Add(new RenderGroup(this, group, entity, EntityManager));
            

            return true;
        }
    }
}
