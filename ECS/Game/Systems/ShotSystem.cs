using DefaultNamespace;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Render;
using GameRules.Scripts.Pool;
using GameRules.Scripts.Weapons;
using GameRulez.Modules.PlayerSystems;
using Unity.Entities;
using Unity.Transforms;

namespace GameRules.Scripts.ECS.Game.Systems
{
    //[UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(UnitRenderSystem))]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class ShotSystem : SystemBase
    {
        private static string VisualBulletId = "Bullet";

        private EntityQuery _waitVisual;

        protected override void OnUpdate()
        {
            var gameMatchSystem = World.GetOrCreateSystem<GameMatchSystem>();
            if(!gameMatchSystem.IsActiveMatch)
                return;
            
            var playerSystem = App.GetModule<IPlayerSystem>();
            var bulletColors = gameMatchSystem.BulletColors;
            
            
            var endSimulationBuffer = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>().CreateCommandBuffer();
            Entities.WithName("AddVisual").WithAll<WaitVisualTag, BulletComponent>().ForEach((Entity entity, ref TeamTagComponent team, in Rotation rotation) =>
            {
                endSimulationBuffer.RemoveComponent<WaitVisualTag>(entity);
                var go = PoolGameObjects.GetNextObject(VisualBulletId);
                if(go == null)
                    return;

                var visual = go.GetComponent<BulletVisual>();
                
                visual.transform.parent = null;
                visual.transform.rotation = rotation.Value;
                go.gameObject.SetActive(true);
                visual.Enable(bulletColors[team.Value], entity);
                
                playerSystem.GetPlayer(team.Value - 1)?.Fire();
            })
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref _waitVisual)
                .Run();
        }
    }
}