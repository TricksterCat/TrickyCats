using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace GameRules.Scripts.ECS.UnitPathSystem
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(PathMoveSystem))]
    [UpdateBefore(typeof(ApplyVelocitySystem))]
    public class NavAvoidanceSystem : SystemBase
    {
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        private float _damping;
        private float _maxForce;

        protected override void OnCreate()
        {
        }


        protected override void OnUpdate()
        {
	        
        }
        
		public void SetSettings(float damping, float maxForce)
		{
			_damping = damping;
			_maxForce = maxForce;
		}
    }
}