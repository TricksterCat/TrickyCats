using GameRules.Scripts.ECS.Components;
using Unity.Entities;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(GameMatchSystem))]
    public class PostGameSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var commandsBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            var commandsBuffer = commandsBufferSystem.CreateCommandBuffer().AsParallelWriter();
            Dependency = Entities.ForEach(
                    (int nativeThreadIndex, in Entity entity, in BulletComponent bulletComponent) =>
                    {
                        if(bulletComponent.RequestDestroy)
                            commandsBuffer.DestroyEntity(nativeThreadIndex, entity);
                    })
                .ScheduleParallel(Dependency);
            commandsBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}