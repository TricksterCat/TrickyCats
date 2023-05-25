using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cinemachine;
using Core.Base;
using Core.Base.Modules;
using GameRules;
using GameRules.Core.Runtime;
using GameRules.Core.Runtime.Extension;
using GameRules.Firebase.Runtime;
using GameRules.ModuleAdapters.Runtime;
using GameRules.Scripts;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.ECS.Render;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.Modules.Database.Items;
using GameRules.Scripts.Modules.Game;
using GameRules.Scripts.Players;
using GameRules.Scripts.Thread;
using GameRules.TaskManager.Runtime;
using Newtonsoft.Json.Linq;
using Players;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;
using Task = System.Threading.Tasks.Task;

namespace GameRulez.Modules.PlayerSystems
{
    public class PlayerSystem : IPlayerSystem, IUpdateListener
    {
        private ModuleStatus _status;
        
        public ModuleStatus Status => _status;

        [SerializeField]
        private bool _onlyBots;
        
        [Required, OdinSerialize, ListDrawerSettings(AddCopiesLastElement = true, DraggableItems = false), DisableIf (nameof(IsPlayMode))]
        private Team[] _teams;
        [OdinSerialize, HideInPlayMode, Range(1, 6)]
        private int _maxPlayer;
        
        [NonSerialized, ShowInInspector, HideInEditorMode, DictionaryDrawerSettings(IsReadOnly = true)]
        private Dictionary<string, IPlayerController> _players;
        
        [NonSerialized]
        private IPlayerController[] _playersByIndex;

        private List<Transform> _spawnPoints;
        
        private List<IInputLogic> _inputs;

        private Dictionary<string, int> _teamToIndex;
        private AnimationCurve _curveStartBiggerBots;


        public int PlayersCount { get; private set; }
        
        string IUpdateListener.UpdateListenerName => nameof(PlayerSystem);
        bool IUpdateListener.IsRequiredUpdate => true;

        private static bool IsPlayMode()
        {
            return Application.isPlaying;
        }
        
        [ShowInInspector, ReadOnly]
        private string[] _nickNames;
        
        public void Initialize()
        {
            Debug.Log("PlayerSystem:: BeginInitialize...");
            
            _players = new Dictionary<string, IPlayerController>();
            _inputs = new List<IInputLogic>();
            _status = ModuleStatus.Disable | ModuleStatus.ProcessingInitialize;
            
            var maxPlayer = math.min(_teams.Length, _maxPlayer);
            
            _teamToIndex = new Dictionary<string, int>(maxPlayer);
            _playersByIndex = new IPlayerController[maxPlayer];
            
            _nickNames = ParseNickNames();
            Debug.Log("TeamsCount: " +_players.Count);

            App.GetModule<ITaskSystem>().Subscribe(CompleteInitialize());
            
            Application.quitting += Dispose;
            
            PlayersCount = maxPlayer;
        }

        private IEnumerator CompleteInitialize()
        {
            yield return Database.WaitInitializeEnumerator();

            _status.ReplaceFlags(ModuleStatus.ProcessingInitialize, ModuleStatus.CompleteInitialize);
            App.GetModule<ITaskSystem>().AddUpdate(this);

            while (FirebaseApplication.Status == StatusInitialize.Wait)
                yield return null;

            var startBigger = JArray.Parse(RemoteConfig.GetString("StartBiggerBots"));
            var frames = new Keyframe[startBigger.Count];
            for (int i = 0; i < frames.Length; i++)
            {
                var frameData = startBigger[i];
                frames[i] = new Keyframe((int)frameData["games"], (float)frameData["chance"]);
            }
            _curveStartBiggerBots = new AnimationCurve(frames);
        }

        private void MinMax(ref int min, ref int max)
        {
            if (min <= max) 
                return;
            int t = min;
                
            min = max;
            max = t;
        }

        private string[] ParseNickNames()
        {
            return Resources.Load<TextAsset>("nicknames").text.Split('\n');
        }
        
        private async Task CompleteSpawnPlayers()
        {
            _inputs.Clear();
            _players.Clear();
            
            var matchController = App.GetModule<IMatchController>();
            await matchController.CompareStatusAsync(ModuleStatus.CompleteInitialize);

            await Database.WaitInitialize();
            
            GetSpawnPoints();

            var matchSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GameMatchSystem>();

            _teams.Sort((team, team1) => Random.Range(0, 1000).CompareTo(Random.Range(0, 1000)));
            var targets = GameObject.FindObjectsOfType<PlayerTarget>();
            int playerId = 0;

            var playerSkins = Database.Players;
            var unitsSkins = Database.Units;

            var waitPlayerSpawn = AsyncEventWaitHandle.GetNext();

            int i = 0;
            var maxPlayer = PlayersCount;
            for (; _spawnPoints != null && i < maxPlayer; i++)
            {
                int localIndex = i;
                var team = _teams[i];
                UnitItem unitSkin;
                
                if (playerId == i)
                {
                    unitSkin = Inventory.UnitSkin.Value;
                    team.UpdateName(GetOrPush.UserName);
                    
                    team.SetSkin(Inventory.PlayerSkin.Value);
                    Spawn(_teams[i], i, false, controller =>
                    {
                        _inputs.Add(new PlayerInputLogic(controller));
                        targets[localIndex].Attach(controller);
                        waitPlayerSpawn.Set();
                    });

                    matchSystem.PlayerIndex.Value = playerId + 1;
                }
                else
                {
                    unitSkin = unitsSkins[Random.Range(0, unitsSkins.Count)];
                    var name = _nickNames[Random.Range(0, _nickNames.Length)];
                    while(_teams.Any(t => t.Name == name))
                        name = _nickNames[Random.Range(0, _nickNames.Length)];
                    
                    team.UpdateName(name);
                    
                    team.SetSkin(playerSkins[Random.Range(0, playerSkins.Count)]);
                    
                    Spawn(_teams[i], i, true, controller =>
                    {
                        targets[localIndex].Attach(controller);
                    });
                    //_inputs.Add(new BotLogic(player));
                }
                
                matchSystem.UpdateTeamInfo(i + 1, team.Name, team.PlayerColor, team.UnitColor, unitSkin.DrawSettings);
            }
            
            _teamToIndex.Clear();
            int index = 0;
            foreach (var team in _teams)
            {
                _teamToIndex[team.Name] = index++;
                if(maxPlayer == index)
                    break;
            }
            
            CompleteSpawn();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GameMatchSystem>().PlayerIndex.Value = 1;
            
            PlayerTarget.SetMainIndex(playerId);
            _playerIndex = playerId;

            await waitPlayerSpawn.WaitAsyncAndFree();
            
            BindToCamera(_playersByIndex[_playerIndex].transform);
        }
        
        
        private void CompleteSpawn()
        {
            if(_spawnPoints == null)
                return;
            
            TmpList<Transform>.Release(ref _spawnPoints);
        }

        private void GetSpawnPoints()
        {
            _spawnPoints = TmpList<Transform>.Get();
            var points = GameObject.FindGameObjectsWithTag("SpawnPoint");
            foreach (var point in points)
                _spawnPoints.Add(point.transform);
        }

        private void Spawn(Team team, int index, bool isBot, Action<IPlayerController> onSpawnCompleted)
        {
            if (_spawnPoints == null)
            {
                Debug.LogError("Spawn points is empty!!!");
            }

            var pointIndex = Random.Range(0, _spawnPoints.Count);
            var spawnPoint = _spawnPoints[pointIndex];
            _spawnPoints.RemoveAt(pointIndex);
            if (_spawnPoints.Count == 0)
                TmpList<Transform>.Release(ref _spawnPoints);
            
            var spawnOperation = Addressables.InstantiateAsync(team.Skin.PrefabReference, spawnPoint.position, spawnPoint.rotation);
            spawnOperation.Completed += handle =>
            {
                var playerGo = handle.Result;
            
                playerGo.name = "Player_" + team.Name;
                var controller = playerGo.GetComponent<IPlayerController>();
                controller.SpawnInfo = new SpawnInfo
                {
                    IsBot = isBot,
                    TeamIndex = index + 1
                };
                
                controller.SetTeam(team, index + 1);
                
                _players[team.Name] = controller;
                _playersByIndex[index] = controller;
                
                onSpawnCompleted.Invoke(controller);
            };
        }

        private void BindToCamera(Transform transform)
        {
            GameObject.FindObjectOfType<CinemachineVirtualCamera>().Follow = transform;
        }

        public IPlayerController GetPlayer(int teamIndex)
        {
            if (teamIndex >= _playersByIndex.Length)
                return null;
            
            return _playersByIndex[teamIndex];
        }

        public Team GetTeam(int teamIndex)
        {
            if (teamIndex >= _playersByIndex.Length)
                return null;
            
            return _playersByIndex[teamIndex].Team;
        }

        public IPlayerController GetPlayer(string team)
        {
            return _players[team];
        }

        public IEnumerable<Team> GetTeams()
        {
            return _teams;
        }

        public IEnumerator SpawnPlayers()
        {
            var task = CompleteSpawnPlayers();
            while (!task.IsCompleted)
                yield return null;
            yield return null;
            SetEnable(true);
        }

        public string PlayerTeam => _teams[_playerIndex].Name;
        public int PlayerTeamIndex => _playerIndex;
        private int _playerIndex;

        public IEnumerable<IPlayerController> GetPlayers()
        {
            return _players.Values;
        }

        public int TeamNameToIndex(string team)
        {
            if (team == null)
                return -1;
            return _teamToIndex[team];
        }

        public string TeamNameByIndex(int teamIndex)
        {
            if (teamIndex == -1)
                return null;
            return _teams[teamIndex].Name;
        }

        private void UpdateInputs()
        {
            JobHandle jobHandle = default;
            
            foreach (var inputLogic in _inputs)
                inputLogic.Update(ref jobHandle);
            
            jobHandle.Complete();
            
            foreach (var inputLogic in _inputs)
                inputLogic.LateUpdate();
        }

        public void SetEnable(bool value)
        {
            _status.SetEnable(value);
            if (!value)
            {
                foreach (var player in _players.Values)
                    player.SetEnable(false);
                _players.Clear();
                
                foreach (var input in _inputs)
                    input.Dispose();
                _inputs.Clear();
            }
        }
        
        public void Dispose()
        {
            if(_inputs == null)
                return;
            
            foreach (var input in _inputs)
                input.Dispose();
        }

        public void GetDetails(StringBuilder detailsBuilder, out object rootGameObject)
        {
            rootGameObject = null;
        }

        void IUpdateListener.Update()
        {
            if (this.CompareStatus(ModuleStatus.Disable))
                return;
            
            UpdateInputs();
        }
    }
}