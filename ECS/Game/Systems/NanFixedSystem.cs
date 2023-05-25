using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class NanFixedSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency = Entities.ForEach((Entity entity, ref PhysicsVelocity velocity, ref Translation translation, ref Rotation rotation, in LocalToWorld l2w) =>
            {
                velocity.Linear = math.select(velocity.Linear, float3.zero, math.any(math.isnan(velocity.Linear)));
                velocity.Angular = math.select(velocity.Angular, float3.zero, math.any(math.isnan(velocity.Angular)));
                translation.Value = math.select(translation.Value, l2w.Position, math.any(math.isnan(translation.Value)));
                rotation.Value = math.select(rotation.Value.value, l2w.Rotation.value,math.any(math.isnan(rotation.Value.value)));
            }).ScheduleParallel(Dependency);
        }
    }
}