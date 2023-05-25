using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Render;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace GameRules.Scripts.ECS.Game.Systems
{
    public partial class GameMatchSystem : SystemBase
    {
        //private CollisionFilter _gunCollisionFilter;
        private ModiferSettings _modiferSettings;

        private void InitializeForGameState()
        {
            var jSettings = JObject.Parse(RemoteConfig.GetString("bot_ai"));
            _modiferSettings = new ModiferSettings
            {
                defPlayerPriority = (float) jSettings["defPlayerPriority"],
                defUnitPriority = (float) jSettings["defUnitPriority"],
                smallTeamPriority = (float) jSettings["smallTeamPriority"],
                verySmallTeamPriority = (float) jSettings["verySmallTeamPriority"],
                weakTeamPriority = (float) jSettings["weakTeamPriority"],
                weakTeamForceDetect = (float) jSettings["weakTeamForceDetect"],
                smallTeamDetect = (int) jSettings["smallTeamDetect"],
                verySmallTeamDetect = (int) jSettings["verySmallTeamDetect"],
            };
        }
        
        private void UpdateGameState(ref JobHandle handle)
        {
            var time = _endGameMatchTime - Time.ElapsedTime;
            TimeToEndMatch = math.max(0, time);

            TryNextSpawnUnits(ref handle);
            ApplyChangeTeam(ref handle);
            UpdateTeamInfos(ref handle);
            TryBeginShots(ref handle);
            UpdateNextBotTargets(ref handle);
                    
            if (time < 0)
                EndGame(ref handle);
        }

        private void ApplyChangeTeam(ref JobHandle handle)
        {
            var barierEnd = BarrierEnd;
            var barierBegin = BarrierBegin;
            var prefabs = Prefabs;
            
            var time = (float)Time.ElapsedTime;
            var lockedIfRecruit = time + _gameSetting.LockedIfRecruitTime;
            
            //var commandBuffer = barierEnd.CreateCommandBuffer().ToConcurrent();
            var bufferNextFrame = barierBegin.CreateCommandBuffer().AsParallelWriter();
            handle = Entities.WithName("ApplyChangeTeam").ForEach((int nativeThreadIndex, ref RecruitComponent recruitComponent, in Entity entity, in Translation translation, in Rotation rotation, in Scale scale, in UnitRenderComponent urc) =>
            {
                if (recruitComponent.Force < 100 || recruitComponent.LockedToTime > time || recruitComponent.Next == 0)
                    return;
                
                //commandBuffer.AddComponent(nativeThreadIndex, entity, ComponentType.ReadOnly<IsDestroy>());
                bufferNextFrame.DestroyEntity(nativeThreadIndex, entity);
                var newEntity = bufferNextFrame.Instantiate(nativeThreadIndex, prefabs[recruitComponent.Next]);

                bufferNextFrame.SetComponent(nativeThreadIndex, newEntity, translation);
                bufferNextFrame.SetComponent(nativeThreadIndex, newEntity, rotation);
                bufferNextFrame.SetComponent(nativeThreadIndex, newEntity, urc);
                bufferNextFrame.SetComponent(nativeThreadIndex, newEntity, new RecruitComponent
                {
                    Force = 0,
                    LockedToTime = lockedIfRecruit
                });
                bufferNextFrame.AddComponent(nativeThreadIndex, newEntity, scale);
            }).WithReadOnly(prefabs).ScheduleParallel(handle);
            
            //barierEnd.AddJobHandleForProducer(handle);
            barierBegin.AddJobHandleForProducer(handle);
        }
        
        private void TryBeginShots(ref JobHandle handle)
        {
            if (_bulletEntity == Entity.Null)
            {
                if (HasSingleton<BulletPrefabComponent>())
                    _bulletEntity = GetSingleton<BulletPrefabComponent>().Prefab;
                else
                    return;
            }
            
            var time = (float)Time.ElapsedTime;
            var weaponInfos = new NativeArray<PlayerShotParams>(MAX_TEAM_COUNT, Allocator.TempJob);
            var newShots = new NativeQueue<BulletRequest>(Allocator.TempJob);

            
            handle = Entities.WithName("CollectWeaponInfos").ForEach((in TeamTagComponent teamIndex, in Translation translation, in PlayerWeaponData weaponData, in PlayerWeaponInfo weaponInfo) =>
            {
                weaponInfos[teamIndex.Value] = new PlayerShotParams
                {
                    Position = translation.Value.xz,
                    Dispersion = weaponInfo.Dispersion,
                    Range = weaponInfo.Range,
                    Speed = weaponInfo.Speed,
                    IsCanShot = weaponData.Ammo > 0 & time > weaponData.CooldownTo
                };
            }).Schedule(handle);

            var units = GetEntityQuery(ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<TeamTagComponent>(), ComponentType.ReadOnly<UnitRenderComponent>());

            units.AddDependency(handle);
            var unitPositions = units.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out var getPositions);
            var unitTeams = units.ToComponentDataArrayAsync<TeamTagComponent>(Allocator.TempJob, out var getTeams);

            var frameRate = Application.targetFrameRate;
            if (frameRate < 1)
                frameRate = 30;
            var dtFix = 25f / frameRate;
            handle = new UpdateNextShotTargets
            {
                Random = _random,
                Players = weaponInfos,
                Positions = unitPositions,
                Teams = unitTeams,
                dtFix = dtFix,
                Result = newShots.AsParallelWriter()
            }.Schedule(MAX_TEAM_COUNT, 1, JobHandle.CombineDependencies(handle, getPositions, getTeams));
            handle = Entities.WithName("UpdateWeaponsData").ForEach((ref PlayerWeaponData weaponData, in PlayerWeaponInfo weaponInfo, in TeamTagComponent teamIndex) =>
                {
                    var shots = newShots.ToArray(Allocator.Temp);
                    int result = 0;
                    for (int i = 0; i < shots.Length; i++)
                        result += math.@select(0, 1, shots[i].TeamIndex == teamIndex.Value);

                    if (result == 1)
                    {
                        weaponData.Ammo--;
                        weaponData.CooldownTo = time + weaponInfo.CooldownTime;
                    }

                    if (time > weaponData.NextRegenerate)
                    {
                        weaponData.NextRegenerate = time + weaponInfo.RegenerateTime;
                        weaponData.Ammo = math.min(weaponData.Ammo + weaponInfo.AmmoRegenerate, weaponInfo.AmmoMax);
                    }
                })
                .WithReadOnly(newShots)
                .Schedule(handle);
            
            
            var barrier = BarrierEnd;
            var commandBuffer = barrier.CreateCommandBuffer();
            var bulletPrefab = _bulletEntity;
            handle = Job.WithName("Shots").WithCode(() =>
            {
                while (newShots.TryDequeue(out var shotInfo))
                {
                    var bullet = commandBuffer.Instantiate(bulletPrefab);
                    commandBuffer.SetComponent(bullet, new Translation{ Value = shotInfo.Position });
                    commandBuffer.SetComponent(bullet, new Rotation { Value = quaternion.LookRotation(new float3(shotInfo.Direction.x, 0, shotInfo.Direction.y), new float3(0, 1, 0))});
                    commandBuffer.SetComponent(bullet, new TeamTagComponent {Value = shotInfo.TeamIndex});
                    commandBuffer.SetComponent(bullet, new PhysicsVelocity {Linear = new float3(shotInfo.Direction.x, 0, shotInfo.Direction.y) * shotInfo.Speed});
                }
            }).Schedule(handle);
            barrier.AddJobHandleForProducer(handle);
            
            handle = newShots.Dispose(handle);
        }
        
        private struct ModiferSettings
        {
            public float defUnitPriority;
            public float defPlayerPriority;
            public float smallTeamPriority;
            public float verySmallTeamPriority;
            public float weakTeamPriority;

            public float weakTeamForceDetect;

            public int smallTeamDetect;
            public int verySmallTeamDetect;
        }

        
        private void UpdateNextBotTargets(ref JobHandle handle)
        {
            var teamsEntities = GetEntityQuery(ComponentType.ReadOnly<TeamUnitElement>()).ToEntityArrayAsync(Allocator.TempJob, out var getTeams);
            var unitsByTeam = GetBufferFromEntity<TeamUnitElement>(true);

            var playerIndex = PlayerIndex;

            var modifer = _modiferSettings;

            handle = Entities.WithName("UpdateNextBotTargets").ForEach((ref PlayerTag player, in TeamTagComponent teamIndex, in Translation translation) =>
            {
                if(!player.IsBot)
                    return;

                float playerModifer = modifer.defPlayerPriority;
                float weakModifer = modifer.weakTeamPriority;
                float smallModifer = modifer.smallTeamPriority;
                float verySmallTeamPriority = modifer.verySmallTeamPriority;

                float nonTeamPriority = modifer.defUnitPriority;

                float weakDetector = modifer.weakTeamForceDetect;
                int smallDetector = modifer.smallTeamDetect;
                int verySmallDetector = modifer.verySmallTeamDetect;
                
                var playerPosition = translation.Value.xz;

                var nearestPriorities = new NativeArray<float>(teamsEntities.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                for (int i = 1; i < teamsEntities.Length; i++)
                    nearestPriorities[i] = -1;
                var nearest = new NativeArray<float2>(teamsEntities.Length, Allocator.Temp);
                nearestPriorities[0] = nonTeamPriority;

                var force = unitsByTeam[teamsEntities[teamIndex.Value]].Length;
                for (int i = 1, iMax = math.min(teamsEntities.Length, teamsEntities.Length); i < iMax; i++)
                {
                    if(i == teamIndex.Value)
                        continue;
                    
                    var enemyTeam = unitsByTeam[teamsEntities[i]];
                    var enemyForce = enemyTeam.Length;
                    if(enemyForce > force | enemyForce == 0)
                        continue;

                    var priority =  (float)force / enemyForce;

                    priority *= math.select(1f, weakModifer, priority > weakDetector);
                    priority *= math.select(1f, smallModifer, enemyForce < smallDetector);
                    priority *= math.select(1f, playerModifer, i == playerIndex.Value);
                    priority *= math.select(1f, verySmallTeamPriority, enemyForce <= verySmallDetector);    
                    
                    nearestPriorities[i] = priority;
                }
                
                for (int j = 0; j < teamsEntities.Length; j++)
                {
                    var units = unitsByTeam[teamsEntities[j]];
                    if(units.Length == 0)
                        continue;
                    
                    var nearestBestIndex = -1;
                    var nearestDist = float.MaxValue;
                    
                    for (int i = 0; i < units.Length; i++)
                    {
                        var dist = math.distancesq(playerPosition, units[i].Position);
                        nearestBestIndex = math.@select(nearestBestIndex, i, dist < nearestDist);
                        nearestDist = math.min(dist, nearestDist);
                    }
                    
                    nearest[j] = units[nearestBestIndex].Position;
                    nearestPriorities[j] *= nearestDist;
                }

                int bestPriorityIndex = -1;
                float bestPriority = -1;
                for (int i = 0; i < teamsEntities.Length; i++)
                {
                    bestPriorityIndex = math.@select(bestPriorityIndex, i, nearestPriorities[i] > bestPriority);
                    bestPriority = math.max(bestPriority, nearestPriorities[i]);
                }

                player.NextPosition = nearest[bestPriorityIndex];
            })
                .WithReadOnly(playerIndex)
                .WithReadOnly(teamsEntities)
                .WithReadOnly(unitsByTeam)
                .ScheduleParallel(JobHandle.CombineDependencies(handle, getTeams));

            handle = teamsEntities.Dispose(handle);
        }
    }
}