using System;
using System.Collections.Generic;
using GameRules.Core.Runtime;
using GameRules.Core.Runtime.Modules;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using GameRules.Scripts.Modules.Game;
using GameRules.Scripts.Weapons;
using GameRules.TaskManager.Runtime;
using GameRulez.Modules.PlayerSystems;
using GameRulez.Units;
using Players;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using PlayerWeaponData = GameRules.Scripts.ECS.Components.PlayerWeaponData;

namespace GameRules.Scripts.Players
{
    public struct SpawnInfo
    {
        public int TeamIndex;
        public bool IsBot;
    }

    public class Player3D_navMesh : MonoBehaviour, IPlayerController, IDeclareReferencedPrefabs
    {
        public SpawnInfo SpawnInfo { get; set; }
        //public Skin Skin { get; set; }

        [ShowInInspector, ReadOnly] public Team Team { get; private set; }

        public float Speed { get; private set; }

        [ShowInInspector, ReadOnly] public int TeamSize { get; set; }

        public int Score { get; set; }

        [SerializeField] private Animator _animator;

        public Vector2 Position => new Vector2(transform.position.x, transform.position.z);
        public string TeamName => Team.Name;
        public int TeamIndex { get; private set; }

        [NonSerialized] private bool _startMove;
        [NonSerialized] private Vector3 _direction;

        private string _team;

        [NonSerialized] private bool _isBegin;

        private static readonly int ShotAnimation = Animator.StringToHash("Shot");

        private float _nextUpdate;

        public bool IsMain { get; private set; }

        public GameObject EntityPrefab;
        private Entity _entity;
        private bool _isCompleteSpawn;

        public void SetEnable(bool value)
        {
            enabled = value;
            _startMove = false;
        }

        public void MoveInDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.03)
                return;

            TestBegin();

            if (!_startMove)
                _startMove = true;
            _direction = new Vector3(direction.x, 0f, direction.y).normalized;
        }

        private void TestBegin()
        {
            if (_isBegin)
                return;

            _isBegin = true;
            _animator.SetInteger("State", 3);
        }


        private void Update()
        {
            var gameSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<GameMatchSystem>();
            if (_entity == Entity.Null || gameSystem == null || !gameSystem.IsActiveMatch)
                return;

            if (!_isCompleteSpawn)
                CompleteSpwan(gameSystem.EntityManager);
            
            if(!_startMove)
                return;

            //transform.position = gameSystem.EntityManager.GetComponentData<Translation>(_entity).Value;
            //transform.rotation = gameSystem.EntityManager.GetComponentData<Rotation>(_entity).Value;

            //transform.rotation = Quaternion.LookRotation(_direction);
            var velocity = _direction;
            World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(_entity, new VelocityComponent
            {
                Value = velocity
            });

            //_aiAgent.Move(velocity);
            //_rvoController.velocity = velocity;
        }

        private void Start()
        {
            GetComponentInChildren<Renderer>().allowOcclusionWhenDynamic = false;
        }

        public void SetTeam(Team team, int index)
        {
            Team = team;
            TeamIndex = index;
            IsMain = !SpawnInfo.IsBot;

            var meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        
            var propery = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propery);
            propery.SetColor("_BaseColor", team.PlayerColor);
            meshRenderer.SetPropertyBlock(propery);
        }

        public void Fire()
        {
            _animator.SetTrigger(ShotAnimation);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _entity = conversionSystem.GetPrimaryEntity(EntityPrefab);
        }

        private void CompleteSpwan(EntityManager dstManager)
        {
            _isCompleteSpawn = true;
            
            TeamIndex = SpawnInfo.TeamIndex;
            IsMain = !SpawnInfo.IsBot;
        
            var speed = (float)RemoteConfig.GetDouble("AllPlayersSpeed");
            if(IsMain)
                speed *= (float)RemoteConfig.GetDouble("PlayerSpeed");

            Speed = speed;

            _entity = dstManager.Instantiate(_entity);
                
            #if UNITY_EDITOR
            dstManager.SetName(_entity, name);
            #endif
            if (SpawnInfo.IsBot)
            {
                dstManager.AddComponent<NavMeshPathMover>(_entity);
                dstManager.AddComponent<NavPathBufferElement>(_entity);
            
                TestBegin();
            }
            else
            {
                dstManager.AddComponent<VelocityComponent>(_entity);
            }
        
            dstManager.AddComponentData(_entity, new PlayerTag
            {
                IsBot = SpawnInfo.IsBot
            });
            dstManager.AddComponentData(_entity, new TeamTagComponent
            {
                Value = TeamIndex
            });
            dstManager.AddComponentData(_entity, new SpeedComponent
            {
                Value = Speed
            });
            dstManager.SetComponentData(_entity, new Translation
            {
                Value = transform.position
            });
            dstManager.SetComponentData(_entity, new Rotation
            {
                Value = transform.rotation
            });
            dstManager.AddComponentObject(_entity, transform);
            //dstManager.AddComponent(_entity, ComponentType.ReadOnly<CopyToTransform>());

            var weapon = App.GetModule<IWeaponSystem>().DefaultWeapon;
            dstManager.AddComponentData(_entity, new PlayerWeaponData
            {
                Ammo = weapon.Ammo
            });
            dstManager.AddComponentData(_entity, new PlayerWeaponInfo
            {
                Range = weapon.Distance,
                CooldownTime = weapon.Cooldown,
                AmmoMax = weapon.Ammo,
                RegenerateTime = weapon.AmmoRegenerateInSecond,
                AmmoRegenerate = weapon.AmmoCountRegenerate,
                Speed = weapon.BulletSpeed,
                Dispersion = math.lerp(weapon.Dispersion, 0, RemoteConfig.GetInt(SpawnInfo.IsBot ? "BotFireAccuracy" : "PlayerFireAccuracy", 70) / 100f)
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(EntityPrefab);
        }
    }
}
