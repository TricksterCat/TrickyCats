using System;
using Firebase.Crashlytics;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Render;
using GameRules.Scripts.Modules.Database;
using GameRulez.Modules.PlayerSystems;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;
using Entity = Unity.Entities.Entity;
using Random = Unity.Mathematics.Random;
using SystemBase = Unity.Entities.SystemBase;

using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

namespace GameRules.Scripts.ECS.Game.Systems
{
    public partial class GameMatchSystem : SystemBase
    {
        private AnimationCurve _chanceBotSpawnUnits;
        private bool _invalidParseChangeBotSpawnUnits;
        
        private void UpdateCallSpawn(ref JobHandle handle)
        {
            _nextUnitSpawnBuffer = 0;
            _random = new Random((uint)UnityEngine.Random.Range(1, 1000000));
            _endGameMatchTime = Time.ElapsedTime + _gameSetting.MatchTime;
            SpawnNonTeamUnits(_gameSetting.SpawnUnitsOnStartGame, ref handle);
            UpdateTeamInfos(ref handle);
            SpawnStartBigger(ref handle);
                    
            World.GetOrCreateSystem<StepPhysicsWorld>().Enabled = true;
            OnMatchBegin?.Invoke();
            _gameState = GAME_STATE.GAME;
        }

        private struct SpawnInfo
        {
            public int2 Range;
            public int Team;
            public float2 Position;
        }
        
        private void SpawnStartBigger(ref JobHandle handle)
        {
            if (_chanceBotSpawnUnits == null && !_invalidParseChangeBotSpawnUnits)
            {
                try
                {
                    var json = RemoteConfig.GetString("StartBiggerBots");
                    var startBigger = JArray.Parse(json);
                    Keyframe[] keyframes = new Keyframe[startBigger.Count];
                    for (int i = 0; i < startBigger.Count; i++)
                    {
                        var data = (JObject)startBigger[i];
                        keyframes[i] = new Keyframe
                        {
                            time = (int)data["games"],
                            value = (float)data["chance"]
                        };
                    }
                    _chanceBotSpawnUnits = new AnimationCurve(keyframes);
                }
                catch (Exception e)
                {
                    FirebaseApplication.LogException(e);
                    Debug.LogException(e);
                    _invalidParseChangeBotSpawnUnits = true;
                }
            }
            
            NativeArray<float2> playerPositions = new NativeArray<float2>(MAX_TEAM_COUNT, Allocator.TempJob);
            handle = Entities.WithName("SpawnStartBigger_GetPlayerPositions").WithAll<PlayerTag>().ForEach((in Translation translation, in TeamTagComponent teamIndex) =>
                {
                    playerPositions[teamIndex.Value] = translation.Value.xz;
                }).Schedule(handle);
            handle.Complete();
            
            var spawnUnits = RemoteConfig.GetInt("AdsCrowdBonus");
            var playerIndex = PlayerIndex.Value;
            
            var units = new NativeList<Entity>(Allocator.TempJob);
            var unitsInfo = new NativeList<SpawnInfo>(Allocator.TempJob);

            int index = 0;
            var playerCrowd = GetOrPush.TotalCrowdSize;
            if (playerCrowd > 0)
            {
                GetOrPush.AdsBonusActive = false;
                GetOrPush.AdCrowdBonus = 0;
                
                unitsInfo.Add(new SpawnInfo
                {
                    Team = playerIndex,
                    Position = playerPositions[playerIndex],
                    Range = new int2(0, playerCrowd)
                });

                var array = EntityManager.Instantiate(_prefabs[playerIndex], playerCrowd, Allocator.TempJob);
                units.AddRange(array);
                handle = array.Dispose(handle);
                index += playerCrowd;
            }
            
            var random = _random;
            if (_chanceBotSpawnUnits != null)
            {
                var gamesCount = GetOrPush.PlayGames;
                var chance = _chanceBotSpawnUnits.Evaluate(gamesCount);
                
                var playerCount = App.GetModule<IPlayerSystem>().PlayersCount;
                for (int i = 0; i < playerCount; i++)
                {
                    var teamIndex = i + 1;
                    if(teamIndex == playerIndex || _random.NextFloat() > chance)
                        continue;

                    var spawnUnitsCurrent = spawnUnits + random.NextInt(0, Inventory.Crowd.Value);
                    unitsInfo.Add(new SpawnInfo
                    {
                        Team = teamIndex,
                        Position = playerPositions[teamIndex],
                        Range = new int2(index, index + spawnUnitsCurrent)
                    });
                    
                    var array = EntityManager.Instantiate(_prefabs[teamIndex], spawnUnitsCurrent, Allocator.TempJob);
                    units.AddRange(array);
                    handle = array.Dispose(handle);
                    index += spawnUnitsCurrent;
                }
            }
            
            var navMeshWorld = NavMeshWorld.GetDefaultWorld();
            var navMeshQuery = new NavMeshQuery(navMeshWorld, Allocator.TempJob);
            var commandBuffer = BarrierEnd.CreateCommandBuffer();
            Job.WithName("SetSpawnUnitsData").WithCode(() =>
            {
                var unitsAsArray = units.AsArray();
                for (int i = 0; i < unitsInfo.Length; i++)
                {
                    var unitInfo = unitsInfo[i];
                    var range = unitInfo.Range;
                    
                    var spawnEntities = unitsAsArray.Slice(range.x, range.y - range.x);
                    for (int j = 0; j < spawnEntities.Length; j++)
                    {
                        var position = float3.zero;
                        position.xz = unitInfo.Position + random.NextFloat2Direction() * 5;
                        
                        commandBuffer.SetComponent(spawnEntities[j], new Translation
                        {
                            Value = navMeshQuery.MapLocation(position, Vector3.one * 5, 0).position
                        });
                        commandBuffer.SetComponent(spawnEntities[j], new Rotation
                        {
                            Value = quaternion.RotateY(math.radians(random.NextFloat(0, 360)))
                        });
                        commandBuffer.SetComponent(spawnEntities[j], new TeamTagComponent
                        {
                            Value = unitInfo.Team
                        });
                        commandBuffer.SetComponent(spawnEntities[j], new UnitRenderComponent
                        {
                            StartAnimation = random.NextFloat(0, 100) + random.NextFloat(0, 10) + random.NextFloat()
                        });
                        commandBuffer.AddComponent(spawnEntities[j], new Scale
                        {
                            Value = random.NextFloat(0.9f, 1.06f)
                        });
                    }
                }
            })
                .WithReadOnly(units)
                .WithReadOnly(unitsInfo)
                .Schedule(handle).Complete();
            
            handle = JobHandle.CombineDependencies(units.Dispose(default), unitsInfo.Dispose(default), playerPositions.Dispose(default));
            navMeshQuery.Dispose();
        }
    }
}