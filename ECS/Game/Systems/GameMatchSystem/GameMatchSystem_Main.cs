using System;
using DefaultNamespace;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Render;
using GameRules.Scripts.ECS.UnitPathSystem;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using GameRules.Scripts.Extensions;
using GameRulez.Modules.PlayerSystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;
using Material = UnityEngine.Material;
using Random = Unity.Mathematics.Random;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(ShotSystem))]
    [AlwaysUpdateSystem]
    public partial class GameMatchSystem : SystemBase
    {
        public const int MAX_TEAM_COUNT = 9;
        
        public static event Action OnMatchBegin;
        public static event Action OnMatchEnd;
        
        private string[] _teamNames;
        
        
        public NativeElement<int> PlayerIndex;
        
        public NativeArrayReadOnly<Color> BulletColors;
        private NativeArray<Color> _bulletColors;
        private UnitSpawnData[] _unitsInfo;
        
        private class UnitSpawnData
        {
            public Color Color;
            public Mesh Mesh;
            public Material Material;
            public Vector3 Rotate;
        }
        
        public string GetTeamName(int index) => _teamNames[index];

        public bool IsActiveMatch => _gameState == GAME_STATE.GAME;

        public double TimeToEndMatch { get; private set; }
        public GameSetting GameSetting => _gameSetting;
        public NativeArray<Entity> Prefabs => _prefabs;

        private EntityQuery _unitByTeam;

        private GameSetting _gameSetting;
        private GAME_STATE _gameState;

        private NativeArray<Entity> _prefabs; 
        
        private ProfilerMarker _spawnGroupUnitsMaker = new ProfilerMarker("SpawnGroupUnits");
        private ProfilerMarker _spawnSingleUnitsMaker = new ProfilerMarker("SpawnSingleUnits");

        
        private double _endGameMatchTime;
        private float _nextUnitSpawnBuffer;
        private Random _random;

        private Entity _bulletEntity;

        public GAME_STATE CurrentState => _gameState;
        
        EntityCommandBufferSystem BarrierEnd => World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        EntityCommandBufferSystem BarrierBegin => World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        PathMoveSystem PathMoveSystem => World.GetOrCreateSystem<PathMoveSystem>();
        
        public enum GAME_STATE
        {
            NONE,
            WAIT_PREFABS,
            CALL_SPAWN,
            GAME,
            CLEAR
        }
        
        protected override void OnCreate()
        {
            PlayerIndex = new NativeElement<int>(-1, Allocator.Persistent);
            
            _bulletColors = new NativeArray<Color>(MAX_TEAM_COUNT, Allocator.Persistent);
            BulletColors = new NativeArrayReadOnly<Color>(_bulletColors);
            
            _teamNames = new string[MAX_TEAM_COUNT];
            for (int i = 0; i < _teamNames.Length; i++)
                _teamNames[i] = string.Empty;
            
            _prefabs = new NativeArray<Entity>(MAX_TEAM_COUNT, Allocator.Persistent);

            _unitByTeam = GetEntityQuery(ComponentType.ReadOnly<UnitRenderComponent>());
            App.SafeGetModule<IPlayerSystem>(OnGetPlayerSystem);
        }

        private void OnGetPlayerSystem(IPlayerSystem system)
        {
            _unitsInfo = new UnitSpawnData[system.PlayersCount + 1];
        }

        public void UpdateTeamInfo(int index, string name, Color bulletColor, Color unitsColors, UnitRenderSystem.DrawSettings settings)
        {
            _teamNames[index] = name;
            _bulletColors[index] = bulletColor;
            
            _unitsInfo[index] = new UnitSpawnData
            {
                Mesh = settings.Mesh,
                Color = unitsColors,
                Material = settings.Material,
                Rotate = settings.Rotation
            };
        }
        
        protected override void OnDestroy()
        {
            _prefabs.Dispose();
            PlayerIndex.Dispose();
            _bulletColors.Dispose();

            CallClear(Dependency, true);
            _unitsInfo = null;
        }

        public void CallClear(JobHandle handle, bool isDestroy = false)
        {
            if (_unitsInfo != null)
            {
                for (int i = 0; i < _unitsInfo.Length; i++)
                    _unitsInfo[i] = null;
            }
            
            handle = Entities.ForEach((ref TeamInfo teamInfo) =>
            {
                teamInfo.Score = 0;
                teamInfo.TeamSize = 0;
            }).Schedule(handle);
            handle.Complete();

            if(!isDestroy)
                World.GetExistingSystem<PathMoveSystem>()?.FreePaths();
            
            _bulletEntity = Entity.Null;
            EntityManager.DestroyEntity(GetEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<PhysicsCollider>(),
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<TeamTagComponent>(), 
                    ComponentType.ReadOnly<StaticOptimizeEntity>(), 
                    ComponentType.ReadOnly<PathComponent>(),
                    ComponentType.ReadOnly<Translation>(), 
                    ComponentType.ReadOnly<PhysicsJoint>(), 
                },
                Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
            }));
        }
        

        public void StartMatch(GameSetting gameSetting)
        {
            _gameSetting = gameSetting;
            _gameState = GAME_STATE.WAIT_PREFABS;
        }

        protected override void OnUpdate()
        {
            JobHandle handle = Dependency;
            switch (_gameState)
            {
                case GAME_STATE.WAIT_PREFABS:
                    if(!UpdateWaitPrefabs(ref handle))
                        return;
                    _gameState = GAME_STATE.CALL_SPAWN;
                    break;
                case GAME_STATE.CALL_SPAWN:
                    UpdateCallSpawn(ref handle);
                    break;
                case GAME_STATE.GAME:
                    UpdateGameState(ref handle);
                    break;
                case GAME_STATE.CLEAR:
                    _gameState = GAME_STATE.NONE;
                    CallClear(handle);
                    break;
                default:
                    return;
            } 

            Dependency = JobHandle.CombineDependencies(Dependency, handle);
        }

        public void CompleteDestroyLastGame()
        {
            _gameState = GAME_STATE.CLEAR;
        }

        private void EndGame(ref JobHandle handle)
        {
            _gameState = GAME_STATE.NONE;
            World.GetOrCreateSystem<StepPhysicsWorld>().Enabled = false;

            handle = Entities.ForEach((ref VelocityComponent velocity) => { velocity.Value = float3.zero; }).Schedule(handle);
            handle = Entities.ForEach((ref PhysicsVelocity velocity) => { velocity.Linear = float3.zero; }).Schedule(handle);

            
            OnMatchEnd?.Invoke();
        }

        private void TryNextSpawnUnits(ref JobHandle handle)
        {
            _unitByTeam.ResetFilter();
            var totalUnits = _unitByTeam.CalculateEntityCount();
            var maxSpawn = math.max(GameSetting.MaxSpawnUnit - totalUnits, 0);
            
            if(maxSpawn <= 0)
                return;
            
            _nextUnitSpawnBuffer += _random.NextFloat(_gameSetting.SpawnUnitsPerSecond.x, _gameSetting.SpawnUnitsPerSecond.y) * Time.DeltaTime;
            var count = (int)_nextUnitSpawnBuffer;
            if (count > 0)
            {
                _nextUnitSpawnBuffer %= 1f;
                SpawnNonTeamUnits(math.min(count, maxSpawn), ref handle);
            }
        }


        private void SpawnNonTeamUnits(int count, ref JobHandle handle)
        {
            var prefab = _prefabs[0];
            
            switch (count)
            {
                case 0:
                    return;
                case 1:
                    _spawnSingleUnitsMaker.Begin();
                    var barrier = BarrierBegin;
                    var buffer = barrier.CreateCommandBuffer();
                    var entity = buffer.Instantiate(prefab);// EntityManager.Instantiate(prefab);
                    PathMoveSystem.SpawnToRandomPosition(entity, _random, buffer);
                    _spawnSingleUnitsMaker.End();
                    return;
                default:
                    _spawnGroupUnitsMaker.Begin();
                    PathMoveSystem.SpawnToRandomPosition(prefab, count, ref handle);
                    _spawnGroupUnitsMaker.End();
                    return;
            }
        }

        /*private void SpawnUnitsToTeam(int teamIndex, NativeArray<float3> positions, ref JobHandle handle)
        {
            var barrier = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
            var commands = barrier.CreateCommandBuffer();
            var newUnits = EntityManager.Instantiate(_prefabs[teamIndex], positions.Length, Allocator.TempJob);
            handle = Job.WithName("SetPositionToSpawnUnits").WithCode(() =>
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    commands.SetComponent(newUnits[i], new Translation
                    {
                        Value = positions[i]
                    });
                }
            }).WithDeallocateOnJobCompletion(newUnits).Schedule(handle);
            barrier.AddJobHandleForProducer(handle);
        }*/

        private void UpdateTeamInfos(ref JobHandle handle)
        {
            var dt = Time.DeltaTime;
            var gameSetting = _gameSetting;
            
            //var unitsByTeam = new NativeArray<int>(MAX_TEAM_COUNT, Allocator.TempJob);
            var scorePerSecond = dt * gameSetting.UnitScorePerSecond;
            
            var unitsByTeam = new UnsafeMultiHashMap<int, float2>(1024, Allocator.TempJob);
            var unitsByTeamParallel = unitsByTeam.AsParallelWriter();
            
            handle = Entities.WithAll<RecruitComponent>().WithName("CountUnitsByTeams")
                .ForEach((in Entity e, in TeamTagComponent teamIndex, in Translation translation) =>
                {
                    if (math.any(math.isnan(translation.Value.xz)))
                        return;
                    unitsByTeamParallel.Add(teamIndex.Value, translation.Value.xz);
                }).ScheduleParallel(handle);
            handle = Entities.WithName("UpdateTeamInfos").ForEach((int entityInQueryIndex, ref TeamInfo teamInfo, ref DynamicBuffer<TeamUnitElement> unitsInTeam) =>
                {
                    unitsInTeam.Clear();
                    
                    int index = 0;
                    var found = unitsByTeam.TryGetFirstValue(entityInQueryIndex, out var position, out var it);
                    while (found)
                    {
                        index++;
                        unitsInTeam.Add(new TeamUnitElement{Position = position});
                        found = unitsByTeam.TryGetNextValue(out position, ref it);
                    }
                    
                    var dif = math.max(0, index - teamInfo.TeamSize);
                    
                    teamInfo.TeamSize = index;
                    //teamInfo.LastTeamSize = index;
                    teamInfo.Score += teamInfo.TeamSize * scorePerSecond + dif * gameSetting.UnitScoreFromRecruit;
            })
                .Schedule(handle);
            handle = unitsByTeam.Dispose(handle);
        }
    }
    
}