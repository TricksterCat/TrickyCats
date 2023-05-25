using DefaultNamespace;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Game.Effects.ChangeTeamEffectGroup;
using GameRules.Scripts.Pool;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateBefore(typeof(GameUiSystem))]
    public class EffectsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            _attachEffects = new NativeQueue<AttachEffect>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            Dependency = _attachEffects.Dispose(Dependency);
            Dependency.Complete();
        }

        private struct AttachEffect
        {
            public Entity Entity;
            public Color Color;
        }

        private NativeQueue<AttachEffect> _attachEffects;

        private EntityQuery _initChangeTeamQuery;
        protected override void OnUpdate()
        {
            var gameMatchSystem = World.GetExistingSystem<GameMatchSystem>();
            if(gameMatchSystem == null)
                return;

            //Dependency.Complete();
            
            var colors = gameMatchSystem.BulletColors; //TODO: Фиксить
            
            var attachEffects = _attachEffects;
            
            var endSimulationBuffer = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>();
            var buffer = endSimulationBuffer.CreateCommandBuffer();
            Entities.WithName("SpawnEffectEventJob").WithAll<InitChangeTeam>().ForEach((Entity entity, in TeamTagComponent teamTagIndexComponent) =>
            {
                var teamComponent = teamTagIndexComponent;
                attachEffects.Enqueue(new AttachEffect
                {
                    Entity = entity,
                    Color = colors[teamComponent.Value]
                });
                buffer.RemoveComponent<InitChangeTeam>(entity);
            })
                .WithStoreEntityQueryInField(ref _initChangeTeamQuery)
                .Run();
            
            while (attachEffects.TryDequeue(out var attachEffect))
            {
                var effect = PoolGameObjects.GetNextObject(ChangeTeamEffectHybrid.Group);
                
                effect.gameObject.SetActive(true);
                //EntityManager.AddComponentObject(attachEffect.Entity, effect.transform);
                
                effect.GetComponent<ChangeTeamEffectHybrid>().Play(attachEffect.Entity, attachEffect.Color);
            }
        }
    }
}