using System;
using System.Collections.Generic;
using GameRules.Scripts.Pool;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace GameRules.Scripts.ECS.Game.Effects.ChangeTeamEffectGroup
{
    public class ChangeTeamEffectHybrid : MonoBehaviour
    {
        public const string Group = "SpawnEffects";
        
        //private static Queue<Entity> DestroyEntities = new Queue<Entity>();

        [SerializeField]
        private ParticleSystem _particleSystem;

        private Entity _root;
        private bool _waitPlay;
        
        public void Play(Entity entity, Color color)
        {
            _root = entity;
            _particleSystem.Clear();
            
            var main = _particleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
            
            _waitPlay = true;
        }

        private void OnDestroy()
        {
            Free();
        }

        public void OnParticleSystemStopped()
        {
            PoolGameObjects.Destroy(gameObject, Group);
            Free();
        }

        private void Free()
        {
            if(_root == Entity.Null)
                return;
            
            _waitPlay = false;
            //DestroyEntities.Enqueue(_root);
            _root = Entity.Null;
        }

        private void LateUpdate()
        {
            var defWorld = World.DefaultGameObjectInjectionWorld;
            if(defWorld == null)
                return;

            if (!defWorld.EntityManager.Exists(_root))
            {
                Free();
                return;
            }

            var translation = defWorld.EntityManager.GetComponentData<Translation>(_root).Value;
            if (math.isnan(translation.x))
            {
                Free();
                return;
            }
            
            if (_waitPlay)
            {
                _waitPlay = false;
                _particleSystem.Play();
            }
            transform.position = translation;
        }

        /*public static void RemoveAllDestroyed(EntityCommandBuffer commandBuffer)
        {
            while (DestroyEntities.Count > 0)
                commandBuffer.DestroyEntity(DestroyEntities.Dequeue());
        }*/
    }
    
    [ConverterVersion("joe", 1)]
    public class ChangeTeamEffectConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            /*Entities.ForEach((ChangeTeamEffectHybrid component, ParticleSystem particles) =>
            {
                AddHybridComponent(particles);
                AddHybridComponent(component);
            });*/
        }
    }

}