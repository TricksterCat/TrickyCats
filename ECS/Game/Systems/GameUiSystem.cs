using System.Collections.Generic;
using DefaultNamespace;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.Modules;
using GameRules.Scripts.UI.Results;
using GameRulez.Modules.PlayerSystems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public class GameUiSystem : SystemBase
    {
        public const float UpdatePlayerTarget_TimeRate = 0.04f;
        
        private Entity _playerEntity;
        private Entity _playerTeamEntity;
        
        private int BulletCount;

        private double _nextUpdatePlayerTarget;

        private List<PlayerTarget> _playerTargets;
        private Timer _timer;
        private CountAmmo _countAmmo;

        private RectTransform _rootUi;

        private bool _isWaitShowEndGame;
        
        protected override void OnCreate()
        {
            _playerTargets = new List<PlayerTarget>();
            
            GameMatchSystem.OnMatchBegin += OnMatchBegin;
            GameMatchSystem.OnMatchEnd += OnMatchEnd;
        }

        protected override void OnDestroy()
        {
            GameMatchSystem.OnMatchBegin -= OnMatchBegin;
            GameMatchSystem.OnMatchEnd -= OnMatchEnd;
        }

        private void OnMatchBegin()
        {
            _rootUi = (RectTransform)GameObject.FindWithTag("RootUI").transform;
        }
        
        private struct SortComparer : IComparer<KeyValuePair<Team, int>>
        {
            public int Compare(KeyValuePair<Team, int> x, KeyValuePair<Team, int> y)
            {
                return -x.Value.CompareTo(y.Value);
            }
        }

        private void OnMatchEnd()
        {
            _isWaitShowEndGame = true;
        }

        public void Attach(PlayerTarget playerTarget)
        {
            _playerTargets.Add(playerTarget);
        }
        
        public void Attach(Timer playerTarget)
        {
            _timer = playerTarget;
        }
        
        public void Attach(CountAmmo countAmmo)
        {
            _countAmmo = countAmmo;
        }
        
        protected override void OnUpdate()
        {
            var gameMatch = World.GetExistingSystem<GameMatchSystem>();
            if(gameMatch == null)
                return;

            if (_isWaitShowEndGame)
            {
                ShowEndGame();
                return;
            }
            
            if(gameMatch.CurrentState != GameMatchSystem.GAME_STATE.GAME)
                return;
            
            if (_playerTeamEntity == Entity.Null)
            {
                var teamEntities = GetEntityQuery(ComponentType.ReadOnly<TeamInfo>()).ToEntityArray(Allocator.TempJob);
                _playerTeamEntity = teamEntities[gameMatch.PlayerIndex.Value];
                Dependency = teamEntities.Dispose(Dependency);
            }
            
            if (_playerEntity == Entity.Null)
            {
                var playersInfo = GetComponentDataFromEntity<TeamTagComponent>(true);

                var players = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>(), ComponentType.ReadOnly<TeamTagComponent>());
                var playersEntityes = players.ToEntityArray(Allocator.TempJob);

                var playerIndex = gameMatch.PlayerIndex.Value;
                for (int i = 0; i < playersEntityes.Length; i++)
                {
                    if (playersInfo[playersEntityes[i]].Value == playerIndex)
                        _playerEntity = playersEntityes[i];
                }
                Dependency = playersEntityes.Dispose(Dependency);
            }

            if (_countAmmo != null)
            {
                var weaponData = GetComponentDataFromEntity<PlayerWeaponData>(true);
                _countAmmo.UpdateValue(weaponData[_playerEntity].Ammo);
            }
            
            _timer?.UpdateTimer((int)math.max(0, gameMatch.TimeToEndMatch));
            
            if (_nextUpdatePlayerTarget < Time.ElapsedTime)
                UpdatePlayerTargetPositions();
            for (int i = 0; i < _playerTargets.Count; i++)
                _playerTargets[i].OnUpdate();
        }

        private void ShowEndGame()
        {
            _isWaitShowEndGame = false;
            
            _playerEntity = Entity.Null;
            _playerTeamEntity = Entity.Null;
            
            _timer.UpdateTimer(0);
            
            _playerTargets.Clear();
            _timer = null;
            _countAmmo = null;
            
            Resources.LoadAsync<GameObject>("Results").completed += handle =>
            {
                var playerSystem = App.GetModule<IPlayerSystem>();
                var gameSystem = World.GetExistingSystem<GameMatchSystem>();

                var playerIndex = gameSystem.PlayerIndex.Value;
            
                Team playerTeam = null;
            
                var teams = TmpList<KeyValuePair<Team, int>>.Get();
                var teamInfo = GetEntityQuery(ComponentType.ReadOnly<TeamInfo>()).ToComponentDataArray<TeamInfo>(Allocator.TempJob);
                for (int i = 1, iMax = playerSystem.PlayersCount + 1; i < iMax; i++)
                {
                    var player = playerSystem.GetPlayer(i - 1);
                    teams.Add(new KeyValuePair<Team, int>(player.Team, (int)teamInfo[i].Score));
                    if (i == playerIndex)
                        playerTeam = player.Team;
                }
            
                teams.Sort(new SortComparer());
            
                GetOrPush.LastMatchResult = teams.FindIndex(pair => pair.Key == playerTeam) + 1;
                GetOrPush.PlayGames++;

                if (GetOrPush.PlayGames % 5 == 0)
                    CrowdAnalyticsMediator.Instance.BeginEvent("play_5").CompleteBuild();
                
                var result = GameObject.Instantiate((GameObject)((ResourceRequest)handle).asset, _rootUi).GetComponent<GameResult>();
                result.Show(teams, playerTeam, (int)teamInfo[playerIndex].Score, teamInfo[playerIndex].TeamSize);
                TmpList<KeyValuePair<Team, int>>.Release(teams);
                teamInfo.Dispose();
            };
        }

        private void UpdatePlayerTargetPositions()
        {
            _nextUpdatePlayerTarget = Time.ElapsedTime + UpdatePlayerTarget_TimeRate;
            
            var legacyPlayerSystem = App.GetModule<IPlayerSystem>();
            var playersCount = legacyPlayerSystem.PlayersCount;

            var camera = Camera.main;
            var updateTargetsInput = new PlayerTarget.InputData
            {
                P = camera.projectionMatrix,
                V = camera.transform.worldToLocalMatrix,
                canvasSize = _rootUi.sizeDelta,

                Min = new float2(0.1f, .1f),
                Max = new float2(0.9f, .9f),
                ScaleRange = new float2(1, 0.5f),
                ScaleDist = new float2(0.4f, 3f),

                PlayerPositions = new NativeArray<float3>(playersCount, Allocator.TempJob)
            };

            var updateTargetsOutput = new PlayerTarget.OutputData
            {
                RectPositions = new NativeArray<float2>(playersCount, Allocator.TempJob),
                Dirs = new NativeArray<int>(playersCount, Allocator.TempJob),
                Scale = new NativeArray<float>(playersCount, Allocator.TempJob),
                Views = new NativeArray<float2>(playersCount, Allocator.TempJob),
            };

            for (int i = 0; i < _playerTargets.Count; i++)
                _playerTargets[i].SetInputCalculatePositions(ref updateTargetsInput);
            new CalculatePosition
            {
                Input = updateTargetsInput,
                Output = updateTargetsOutput
            }.Run(playersCount);
            for (int i = 0; i < _playerTargets.Count; i++)
                _playerTargets[i].OnUpdatePositions(in updateTargetsOutput);

            JobHandle jobHandle = default;
            updateTargetsInput.Dispose(ref jobHandle);
            updateTargetsOutput.Dispose(ref jobHandle);
            jobHandle.Complete();
        } 
        
        [BurstCompile]
        private struct CalculatePosition : IJobParallelFor
        {
            public PlayerTarget.InputData Input;
            public PlayerTarget.OutputData Output;
                
            
            public void Execute(int index)
            {
                float4x4 VP = math.mul(Input.P, Input.V);

                var point3D = Input.PlayerPositions[index];
                //var fromViewPost = GetViewPort(ref VP, point3D);
                point3D.y  += 3.2f;

                var view =  GetViewPort(ref VP, point3D);
                Output.Views[index] = view;

                var dist = math.distance(view, new float2(0.5f, 0.5f));
                if (dist > Input.ScaleDist.x)
                    Output.Scale[index] = Mathf.Lerp(Input.ScaleRange.x, Input.ScaleRange.y, (dist - Input.ScaleDist.x) / Input.ScaleDist.y);
                else
                    Output.Scale[index] = Input.ScaleRange.x;
                
                int2 result = int2.zero;
                if (view.x < Input.Min.x)
                    result.x = -1;
                if (view.y < Input.Min.y)
                    result.y = -1;
                if (view.x > Input.Max.x)
                    result.x = 1;
                if (view.y > Input.Max.y)
                    result.y = 1;

                if (result.Equals(int2.zero))
                    Output.Dirs[index] = 3;
                else
                {
                    if (result.x == -1 && result.y == -1)
                        Output.Dirs[index]  = view.x < view.y ? 0 : 3;
                    else if(result.x == 1 && result.y == 1)
                        Output.Dirs[index]  = view.x < view.y ? 2 : 1;
                    else if(result.x == -1)
                        Output.Dirs[index]  = 0;
                    else if(result.y == -1)
                        Output.Dirs[index]  = 3;
                    else if(result.y == 1)
                        Output.Dirs[index]  = 1;
                    else if(result.x == 1)
                        Output.Dirs[index]  = 2;
                }
                
                view.x = math.clamp(view.x, 0.15f, 0.85f);
                view.y = math.clamp(view.y, 0.1f, 0.9f);
                
                //var dir = fromViewPost - view;
                
                Output.RectPositions[index] = new float2(
                    view.x * Input.canvasSize.x - Input.canvasSize.x * 0.5f,
                    view.y * Input.canvasSize.y - Input.canvasSize.y * 0.5f);
            }
            
            private float2 GetViewPort(ref float4x4 VP, float3 point3D)
            {
                float4 point4 = new float4(point3D.x, point3D.y, point3D.z, 1.0f);  // turn into (x,y,z,1)
                float4 result4 = math.mul(VP, point4);// VP * point4;  // multiply 4 components
     
                
                float3 result = result4.xyz;  // store 3 components of the resulting 4 components
     
                // normalize by "-w"
                result /= -result4.w;
     
                // clip space => view space
                result.x = result.x / 2 + 0.5f;
                result.y = result.y / 2 + 0.5f;

                return -result4.w < 0 ? -result.xy : result.xy;
            }
        }

    }
}