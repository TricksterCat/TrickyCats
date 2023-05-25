using System;
using System.Collections.Generic;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.Modules.Collisions;
using GameRules.Scripts.Modules.Collisions.NavMesh;
using GameRules.Scripts.Modules.Game;
using GameRules.Scripts.Pool;
using GameRulez.Modules.PlayerSystems;
using Players;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

namespace GameRules.Scripts.Weapons
{
    public class BulletVisual : MonoBehaviour
    {
        [SerializeField]
        private Renderer _renderer;
        [SerializeField]
        private ParticleSystem _particle;

        private MaterialPropertyBlock _propertyBlock;

        private Entity _entity;

        public void Run(IPlayerController player, float bulletSpeed, float distance)
        {
            /*_waitSetTeam = true;
            gameObject.tag = "Bullet";

            var team = player.Team;
            Material material;
            if (!_teamMaterials.TryGetValue(team, out material))
            {
                material = new Material(_renderer.sharedMaterial)
                {
                    color = team.PlayerColor
                };
                _teamMaterials[team] = material;
            }
            _renderer.sharedMaterial = material;
            _bulletDistMax = distance * distance;
            
            Owner = player;
            _bulletSpeed = bulletSpeed;
            _startPoint = transform.position;

            if (_particle != null)
            {
                new UpdateColor
                {
                    Color = team.PlayerColor
                }.Schedule(_particle).Complete();
            }*/
        }

        private void LateUpdate()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if(entityManager.Exists(_entity))
                transform.position = entityManager.GetComponentData<Translation>(_entity).Value;
            else
            {
                _particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var emission = _particle.emission;
                emission.enabled = false;
                PoolGameObjects.Destroy(gameObject, "Bullet");
            }
        }

        public void Enable(Color teamColor, Entity entity)
        {
            _entity = entity;
            _particle.Clear(true);
            
            if(_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", teamColor);
            _renderer.SetPropertyBlock(_propertyBlock);
            
            _renderer.enabled = true;
            var main = _particle.main;
            main.startColor = teamColor;
            
            //
            _particle.Play();
            var emission = _particle.emission;
            emission.enabled = true;
        }
    }
}