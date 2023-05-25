using System.Runtime.InteropServices;
using GameRules.Scripts.ECS;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Game.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace GameRules.Scripts.Modules.Collisions.NavMesh
{
    [WriteGroup(typeof (LocalToWorld))]
    [StructLayout(LayoutKind.Sequential, Size = 1)]
    public struct CopyTransformFrom : IComponentData
    {
    }
    
    [UpdateAfter(typeof(CopyTransformFromGameObjectSystem))]
    [UpdateBefore(typeof(EndFrameRotationEulerSystem))]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [ExecuteAlways]
    public class CopyFromTransformSystem : SystemBase
    {
        private EntityQuery _query;
        
        private struct TransformStash
        {
            public float3 Position;
            public quaternion Rotation;
        }
        
        protected override void OnUpdate()
        {
            var count = _query.CalculateEntityCount();
            if (count == 0)
                return;
            
            var stash = new NativeArray<TransformStash>(count, Allocator.TempJob);
            var inputDeps = new CollectStashJob{ Result = stash }.Schedule(_query.GetTransformAccessArray(), Dependency);

            Dependency = Entities.WithName("SetStashData").WithAll<Transform, CopyTransformFrom>().ForEach((int entityInQueryIndex, ref Translation translation, ref Rotation rotation) =>
            {
                var value = stash[entityInQueryIndex];
                translation.Value = value.Position;
                rotation.Value = value.Rotation;
            })
                .WithReadOnly(stash)
                .WithStoreEntityQueryInField(ref _query)
                .WithDeallocateOnJobCompletion(stash)
                .ScheduleParallel(inputDeps);
        }
        
        [BurstCompile]
        private struct CollectStashJob : IJobParallelForTransform
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<TransformStash> Result;
            
            public void Execute(int index, TransformAccess transform)
            {
                Result[index] = new TransformStash
                {
                    Position = transform.position,
                    Rotation = transform.rotation
                };
            }
        }
    }

    [UpdateAfter(typeof(ApplyVelocitySystem))]
    [UpdateAfter(typeof(EndFrameTRSToLocalToWorldSystem))]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [ExecuteAlways]
    public class CopyToTransformSystem : JobComponentSystem
    {
        private EntityQuery _query;

        protected override void OnCreate()
        {
            _query = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Rotation>(), ComponentType.ReadOnly<CopyToTransform>(), typeof(Transform));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = Entities.WithNone<BulletComponent>().WithAll<PhysicsVelocity>().ForEach((ref Translation tr) => { tr.Value.y = 0; }).Schedule(inputDeps);
            _query.AddDependency(inputDeps);
            
            var translations = _query.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out var copyTranslationHandle);
            var rotations = _query.ToComponentDataArrayAsync<Rotation>(Allocator.TempJob, out var copyRotationsHandle);
            
            return new SetToTransformJob
            {
                Translations = translations,
                Rotations = rotations
            }.Schedule(_query.GetTransformAccessArray(), JobHandle.CombineDependencies(inputDeps, copyTranslationHandle, copyRotationsHandle));
        }
        
        [BurstCompile]
        private struct SetToTransformJob : IJobParallelForTransform
        {
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<Translation> Translations;
            [ReadOnly, DeallocateOnJobCompletion]
            public NativeArray<Rotation> Rotations;
            
            public void Execute(int index, TransformAccess transform)
            {
                transform.position = Translations[index].Value;
                transform.rotation = Rotations[index].Value;
            }
        }
    }
}