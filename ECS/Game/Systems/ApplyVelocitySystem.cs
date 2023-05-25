using DefaultNamespace;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.Modules.Collisions.NavMesh;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateAfter(typeof(CopyFromTransformSystem))]
    [UpdateBefore(typeof(EndFrameTRSToLocalToWorldSystem))]
    [UpdateBefore(typeof(PreTransformCommandBufferSystem))]
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [ExecuteAlways]
    public class ApplyVelocitySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var dt = Time.DeltaTime;

            var dtPhysics = Time.fixedDeltaTime;
            
            var setTranslation = Entities.WithName("VelocityToTranslation").WithNone<PhysicsVelocity>().ForEach((ref Translation translation, in VelocityComponent velocity, in SpeedComponent speedComponent) =>
            {
                translation.Value += math.normalizesafe(velocity.Value) * speedComponent.Value * dt;
            }).Schedule(Dependency);
            var setRotation = Entities.WithName("VelocityToRotation").WithNone<PhysicsVelocity>().ForEach((ref Rotation rotation, in VelocityComponent velocity) =>
            {
                rotation.Value = Quaternion.RotateTowards(rotation.Value, quaternion.LookRotation(velocity.Value, new float3(0, 1, 0)), dt * 90);
            }).Schedule(Dependency);

            var handle = JobHandle.CombineDependencies(setTranslation, setRotation);
            
            var updatePhysicsVelocity = Entities.WithName("VelocityToPhysics").ForEach((ref PhysicsVelocity physicsVelocity, in VelocityComponent velocity, in SpeedComponent speedComponent) =>
            {
                var velocityN = math.normalizesafe(velocity.Value);
                physicsVelocity.Linear = velocityN * speedComponent.Value;
            }).Schedule(handle);

            var updateRotationPhysicsVelocity = Entities.WithAll<PhysicsVelocity>().WithName("VelocityToRotationByPhysics").ForEach(
                (ref Rotation rotation, in VelocityComponent velocity, in RotationSpeed rotationSpeed) =>
                {
                    if (math.lengthsq(velocity.Value.xz) > 0.001)
                    {
                        rotation.Value = Quaternion.RotateTowards(rotation.Value, quaternion.LookRotation(velocity.Value, new float3(0, 1, 0)), dt * rotationSpeed.Value);
                        //rotation.Value = quaternion.LookRotation(velocityN, new float3(0, 1, 0));
                    }
                }).Schedule(handle);

            Dependency = JobHandle.CombineDependencies(updatePhysicsVelocity, updateRotationPhysicsVelocity);
        }
    }
}