using System;
using System.Collections.Generic;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Events;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.ECS.Jobs;
using GameRules.Scripts.ECS.UnitPathSystem;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using GameRules.Scripts.Modules.Game;
using JacksonDunstan.NativeCollections;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;
using Random = Unity.Mathematics.Random;

namespace GameRules.Scripts.ECS
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup)), 
     UpdateBefore(typeof(GameMatchSystem))]
    public class TargetMoveSystem : SystemBase
    {
        private const int ThreadMax = 8;
        private const int BatchSize = 128;
        
        private NativeArray<PlayerData> _targets;
        
        private NavMeshWorld _navMeshWorld;

        private EntityQuery _fillUnitsToPlayerQuery;
        private EntityQuery _fillPlayerQuery;
        
        private struct PlayerData
        {
            public bool IsSkip;
            
            public NavMeshLocation Location;
            public Vector2 Position;
            public Vector2 Head;
        }

        private float _timeToUpdatePath = 0.3f;
        private bool _worldIsCreated;

        private NavMeshQuery[] _queryList;
        private NavMeshQuery _firstQuery;
        private unsafe void* _queryListPtr;
        private ulong _gcHandle;
        
        protected override void OnCreate()
        {
            _targets = new NativeArray<PlayerData>(GameMatchSystem.MAX_TEAM_COUNT * 2, Allocator.Persistent);

            _fillUnitsToPlayerQuery = GetEntityQuery(ComponentType.ReadOnly<TeamTagComponent>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<NavMeshPathMover>(), ComponentType.Exclude<PlayerTag>());
            _fillPlayerQuery = GetEntityQuery(ComponentType.ReadOnly<TeamTagComponent>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<NavMeshPathMover>(), ComponentType.ReadOnly<PlayerTag>());
        }

        protected override void OnDestroy()
        {
            var handle = _targets.Dispose(Dependency);
            handle.Complete();
            DisposeQuery();
        }

        private unsafe void DisposeQuery()
        {
            if (_worldIsCreated)
            {
                _queryListPtr = null;
                UnsafeUtility.ReleaseGCObject(_gcHandle);
                for (int i = 0; i < _queryList.Length; i++)
                    _queryList[i].Dispose();
                _queryList = null;
                _firstQuery.Dispose();
            }

            _worldIsCreated = false;
        }

        protected override unsafe void OnUpdate()
        {
            if(!App.GetModule<IMatchController>().IsMatchActive)
                return;

            if (HasSingleton<UpdateNavWorldEvent>())
            {
                var navMeshWorld = NavMeshWorld.GetDefaultWorld();
                _navMeshWorld = navMeshWorld;
                if (_worldIsCreated)
                    DisposeQuery();
                
                var queries = new NavMeshQuery[math.min(JobsUtility.JobWorkerMaximumCount, ThreadMax)];
                for (int i = 0; i < queries.Length; ++i)
                {
                    queries[i] = new NavMeshQuery(
                        navMeshWorld,
                        Allocator.Persistent,
                        NavConstants.PATH_NODE_MAX
                    );
                }
                _firstQuery = new NavMeshQuery(
                    navMeshWorld,
                    Allocator.Persistent
                );
                _queryList = queries;
                _queryListPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(_queryList, out _gcHandle);
                
                _worldIsCreated = true;
                EntityManager.DestroyEntity(GetSingletonEntity<UpdateNavWorldEvent>());
            }
            
            if(!_worldIsCreated || !_navMeshWorld.IsValid())
                return;
            
            var targets = (PlayerData*)_targets.GetUnsafePtr();
            var time = Time.ElapsedTime;
            
            var firstQuery = _firstQuery;

            Dependency = Entities.WithName("GetPlayerPositions").WithAll<PlayerTag>()
                .ForEach((in TeamTagComponent teamTag, in Translation translation, in Rotation rotation) =>
                {
                    ref var target = ref targets[teamTag.Value];
                    target.Position = translation.Value.xz;
                    target.Head = math.forward(rotation.Value).xz;
                    target.Location = firstQuery.MapLocation(translation.Value, Vector3.one * 5, 0);
                    target.IsSkip = !firstQuery.IsValid(target.Location);
                })
                .WithNativeDisableUnsafePtrRestriction(targets)
                .Schedule(Dependency);
            
            var nextUpdateTime = time + _timeToUpdatePath;

            var entities = GetEntityTypeHandle();
            var translations = GetComponentTypeHandle<Translation>(true);
            var navMeshPathMoverContainer = GetComponentTypeHandle<NavMeshPathMover>(true);
            var teams = GetComponentTypeHandle<TeamTagComponent>(true);
            var players = GetComponentTypeHandle<PlayerTag>(true);
            
            var results = new NativeList<TargetData>(1024, Allocator.TempJob);
            var handle = Dependency;
            
            handle = new FillPlayers
            {
                Entities = entities,
                Query = firstQuery,
                Result = results,
                Time = Time.ElapsedTime,
                PlayerData = targets,
                TranslationContainer = translations,
                PlayerTagContainer = players,
                TeamTagIndexContainer = teams,
                NavMeshPathMoverContainer = navMeshPathMoverContainer
            }.ScheduleSingle(_fillPlayerQuery, handle);
            
            handle = new FillUnitsToPlayer
            {
                Entities = entities,
                Time = Time.ElapsedTime,
                PlayerData = targets,
                TranslationContainer = translations,
                NavMeshPathMoverContainer = navMeshPathMoverContainer,
                TeamTagIndexContainer = teams,
                Result = results
            }.ScheduleSingle(_fillUnitsToPlayerQuery, handle);
            
            var queryCount = _queryList.Length;
            var batches = new NativeList<int2>(ThreadMax, Allocator.TempJob);
            handle = Job.WithCode(() =>
                {
                    var count = math.min(queryCount * BatchSize, results.Length);
                    int start = 0;
                    while (count > 0)
                    {
                        var batch = math.min(BatchSize, count);
                        var end = start + batch;
                        batches.Add(new int2(start, end));
                        start = end;
                        count -= batch;
                    }
                })
                .WithReadOnly(results)
                .Schedule(handle);
            Dependency = handle;
            
            handle = new CallculateNextPath
            {
                Movers = GetComponentDataFromEntity<NavMeshPathMover>(),
                Ranges = batches,
                Query = _queryListPtr,
                Targets = results,
                PlayerData = targets,
                Paths = GetBufferFromEntity<NavPathBufferElement>(),
                NextUpdate = nextUpdateTime
            }.Schedule(batches, 1, handle);
            _navMeshWorld.AddDependency(handle);

            handle = batches.Dispose(handle);
            handle = results.Dispose(handle);
            Dependency = handle;
        }
        
        private struct TargetData
        {
            public Entity Entity;
            public int IndexLocation;

            public Vector3 From;
            public float2 Head;
            public float3 Target;
        }
        
        [BurstCompile]
        private struct FillUnitsToPlayer : IJobChunk
        {
            [WriteOnly, NativeDisableContainerSafetyRestriction]
            public NativeList<TargetData> Result;

            [ReadOnly]
            public double Time;
            [ReadOnly, NativeDisableUnsafePtrRestriction]
            public unsafe PlayerData* PlayerData;

            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public EntityTypeHandle Entities;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<Translation> TranslationContainer;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<TeamTagComponent> TeamTagIndexContainer;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<NavMeshPathMover> NavMeshPathMoverContainer;
            
            
            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Count == 0)
                    return;
                
                var random = new Random();
                
                var entities = chunk.GetNativeArray(Entities);
                var translations = chunk.GetNativeArray(TranslationContainer);
                var teams = chunk.GetNativeArray(TeamTagIndexContainer);
                var movers = (NavMeshPathMover*)chunk.GetNativeArray(NavMeshPathMoverContainer).GetUnsafeReadOnlyPtr();
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    ref NavMeshPathMover mover = ref movers[i];
                    
                    if(mover.TimeToNewPath > Time | math.isnan(translations[i].Value.x)) //TODO: Поправить nan
                        continue;

                    var target = float3.zero;
                    target.xz = PlayerData[teams[i].Value].Position;
                    target.y = translations[i].Value.y;
                
                    random.InitState(math.asuint(entities[i].Index + 1));
                    var angle = random.NextFloat(-45, 45);
                    var rotate = quaternion.RotateY(angle);
                    
                    var forward = math.mul(rotate, new float3(mover.Head.x, 0, mover.Head.y)) * random.NextFloat(0.1f, 1.5f);
                    target += forward;
                    
                    Result.Add(new TargetData
                    {
                        Entity = entities[i],
                        Head = PlayerData[teams[i].Value].Head,
                        Target = target,
                        IndexLocation = teams[i].Value,
                        From = translations[i].Value
                    });
                }
            }
        }
        
        [BurstCompile]
        private struct FillPlayers : IJobChunk
        {
            [WriteOnly, NativeDisableContainerSafetyRestriction]
            public NativeList<TargetData> Result;

            [ReadOnly]
            public double Time;

            [WriteOnly, NativeDisableUnsafePtrRestriction, NativeDisableContainerSafetyRestriction, NativeDisableParallelForRestriction]
            public unsafe PlayerData* PlayerData;

            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public EntityTypeHandle Entities;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<Translation> TranslationContainer;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<TeamTagComponent> TeamTagIndexContainer;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<NavMeshPathMover> NavMeshPathMoverContainer;
            [NativeDisableContainerSafetyRestriction, ReadOnly]
            public ComponentTypeHandle<PlayerTag> PlayerTagContainer;

            [ReadOnly]
            public NavMeshQuery Query;
            
            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(Entities);
                var translations = chunk.GetNativeArray(TranslationContainer);
                var playerTags = chunk.GetNativeArray(PlayerTagContainer);
                var teams = chunk.GetNativeArray(TeamTagIndexContainer);
                var movers = (NavMeshPathMover*)chunk.GetNativeArray(NavMeshPathMoverContainer).GetUnsafeReadOnlyPtr();
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    ref NavMeshPathMover mover = ref movers[i];
                    
                    if(mover.TimeToNewPath > Time | !playerTags[i].IsBot)
                        continue;

                    var target = float3.zero;
                    target.xz = playerTags[i].NextPosition;
                    target.y = translations[i].Value.y;

                    var locationIndex = GameMatchSystem.MAX_TEAM_COUNT + teams[i].Value;
                    PlayerData[locationIndex] = new PlayerData
                    {
                        Location = Query.MapLocation(target, Vector3.one * 10, 0)
                    };
                        
                    Result.Add(new TargetData
                    {
                        Entity = entities[i],
                        Head = math.normalizesafe(target.xz - translations[i].Value.xz),
                        Target = target,
                        IndexLocation = locationIndex,
                        From = translations[i].Value
                    });
                }
            }
        }
        
        [BurstCompile]
        private struct CallculateNextPath : IJobParallelForDefer
        {
            [ReadOnly]
            public NativeList<TargetData> Targets;
            [ReadOnly]
            public double NextUpdate;
            
            [NativeDisableUnsafePtrRestriction]
            public unsafe PlayerData* PlayerData;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<NavPathBufferElement> Paths;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<NavMeshPathMover> Movers;

            [NativeDisableUnsafePtrRestriction]
            public unsafe void* Query;

            [ReadOnly]
            public NativeList<int2> Ranges;
            
            public unsafe void Execute([AssumeRange(0u, ThreadMax)] int index)
            {
                var range = Ranges[index];
                var navMeshQuery = UnsafeUtility.ReadArrayElement<NavMeshQuery>(Query, index);
                Execute(range.x, range.y, ref navMeshQuery);
            }
            
            public unsafe void Execute([AssumeRange(0u, BatchSize*ThreadMax)] int i, [AssumeRange(0u, BatchSize*ThreadMax)] int end, ref NavMeshQuery navMeshQuery)
            {
                var targets = (TargetData*)Targets.GetUnsafeReadOnlyPtr();
                for (; i < end; i++)
                {
                    ref var target = ref targets[i];
                    
                    var pathBuffer = Paths[target.Entity];
                    //pathBuffer.Clear();
                    
                    var locationNext = PlayerData[target.IndexLocation].Location;
                    if(PlayerData[target.IndexLocation].IsSkip)
                        continue;

                    var location = navMeshQuery.MapLocation(target.From, Vector3.one * 10, 0);
                    if(!navMeshQuery.IsValid(location))
                        continue;
                    
                    var status = navMeshQuery.BeginFindPath(location, locationNext);
                    
                    while (status == PathQueryStatus.InProgress)
                        status = navMeshQuery.UpdateFindPath(NavConstants.ITERATION_MAX, out int iterationsPerformed);

                    var mover = Movers[target.Entity];
                    if (status != PathQueryStatus.Success)
                    {
                        mover.TimeToNewPath = 0;
                        mover.IsPathValid = pathBuffer.Length == 0;
                        
                        Movers[target.Entity] = mover;
                        continue;
                    }
                
                    navMeshQuery.EndFindPath(out int pathLength);
                    var polygonIdArray = new NativeArray<PolygonId>(pathLength + 2, Allocator.Temp);
                    navMeshQuery.GetPathResult(polygonIdArray);
                    
                    var len = pathLength + 1;
                    var straightPath = new NativeArray<NavMeshLocation>(len, Allocator.Temp);
                    var straightPathFlags = new NativeArray<StraightPathFlags>(len, Allocator.Temp);
                    var vertexSide = new NativeArray<float>(len, Allocator.Temp);
                    var straightPathCount = 0;
                    
                    status = PathUtils.FindStraightPath(
                        navMeshQuery,
                        target.From,
                        target.Target,
                        polygonIdArray,
                        pathLength,
                        ref straightPath,
                        ref straightPathFlags,
                        ref vertexSide,
                        ref straightPathCount,
                        NavConstants.PATH_NODE_MAX
                    );
                    
                    if (status == PathQueryStatus.Success)
                    {
                        mover.TimeToNewPath = NextUpdate;
                        mover.IsPathValid = straightPathCount != 0;
                        mover.PathIndex = 0;
                        mover.Head = target.Head;
                    
                        pathBuffer.ResizeUninitialized(straightPathCount);
                        for (int j = 0; j < straightPathCount; ++j)
                        {
                            var p = (float3)straightPath[j].position;
                            pathBuffer[j] = new NavPathBufferElement
                            {
                                Value = p.xz
                            };
                        }
                    }
                    else
                    {
                        mover.TimeToNewPath = 0;
                        mover.IsPathValid = false;
                    }
                    
                    Movers[target.Entity] = mover;
                }
            }
        }
    }
    
    public static class NavConstants
    {
        /// <summary>Whether NavAgent avoidance is enabled upon creation of the
        /// NavAvoidanceSystem. If you don't care about agent avoidance, set
        /// this to false for performance gains.</summary>
        public const bool AVOIDANCE_ENABLED_ON_CREATE = true;

        /// <summary>The cell radius for NavAgent avoidance.</summary>
        public const float AVOIDANCE_CELL_RADIUS = 1;

        /// <summary>Upper limit on the *duration* spent jumping before the
        /// agent is actually considered falling. This limit can be reached 
        /// when the agent tries to jump too close to the edge of a surface
        /// and misses.</summary>
        public const float JUMP_SECONDS_MAX = 5;

        /// <summary>Upper limit on the raycast distance when searching
        /// for an obstacle in front of a given NavAgent.</summary>
        public const float OBSTACLE_RAYCAST_DISTANCE_MAX = 1000;

        /// <summary>Upper limit on the raycast distance when searching for a
        /// surface below a given NavAgent.</summary>
        public const float SURFACE_RAYCAST_DISTANCE_MAX = 1000;

        /// <summary>Upper limit on the NavAgents the NavAvoidanceSystem will
        /// attempt to process per cell. Keeping this low drastically improves
        /// performance. If there's 1000 agents in a single cell, do you really
        /// want to make them all avoid each other? No, because they're already
        /// colliding anyway.</summary>
        public const int AGENTS_PER_CELL_MAX = 25;

        /// <summary>Upper limit when manually batching jobs.</summary>
        public const int BATCH_MAX = 50;

        /// <summary>Upper limit on the iterations performed in a NavMeshQuery
        /// to find a path in the NavPlanSystem.</summary>
        public const int ITERATION_MAX = 128;

        /// <summary>Upper limit on a given jumpable surface buffer.
        /// Exceeding this will merely result in heap memory blocks being
        /// allocated.</summary>
        public const int JUMPABLE_SURFACE_MAX = 30;

        /// <summary>Upper limit on a given path buffer. Exceeding this will
        /// merely result in heap memory blocks being allocated.</summary>
        public const int PATH_NODE_MAX = 512;

        /// <summary>Upper limit on the search area size during path planning.
        /// </summary>
        public const int PATH_SEARCH_MAX = 1000;

        /// <summary>Upper limit on the number of raycasts to attempt in
        /// searching for a surface below the NavAgent. Exceeding this implies
        /// that there is no surface below the agent, its then determined to be
        /// falling which means that no more raycasts will be performed.
        /// </summary>
        public const int SURFACE_RAYCAST_MAX = 100;

        /// <summary>The 'Humanoid' NavMesh agent type as a string.</summary>
        public const string HUMANOID = "Humanoid";
    }
}