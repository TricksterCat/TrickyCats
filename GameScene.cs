using System.Collections;
using Core.Base.Modules;
using Firebase.Analytics;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS.Events;
using GameRules.Scripts.ECS.UnitPathSystem;
using GameRules.Scripts.Modules.Game;
using GameRules.TaskManager.Runtime;
using GameRulez.Modules.PlayerSystems;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.AI;

namespace GameRules.Scripts
{
    public class GameScene : MonoBehaviour
    {
        public float Damping;
        public float MaxForce;

        public static bool IsExist { get; private set; }
        
        public static bool CanExecute { get; set; }

        public int FpsLimit;

        [Button]
        public void UpdateValues()
        {
            World.DefaultGameObjectInjectionWorld.GetExistingSystem<NavAvoidanceSystem>()
                .SetSettings(Damping, MaxForce);
        }
        
        public async void Initialize()
        {
            IsExist = true;
                
            var ap = Holder.Instance;
            if (ap != null && ap.Active)
            {
                var dpc = ap.DevicePerformanceControl;
                dpc.CpuLevel = dpc.MaxCpuPerformanceLevel;
                dpc.GpuLevel = dpc.MaxGpuPerformanceLevel;
            }
            Application.targetFrameRate = FpsLimit;
            
#if UNITY_IOS
            QualitySettings.vSyncCount = 0;
#endif
            
            FirebaseApplication.Awake();
            
            await FirebaseApplication.WaitInitialize();
            
            Debug.Log("GameScene::Start()");
            App.GetModule<ITaskSystem>().Subscribe(StartComplete());
            
            FirebaseAnalytics.SetCurrentScreen("game", null);

            var activeWorld = World.DefaultGameObjectInjectionWorld;

            var transformSystemGroup = activeWorld.GetOrCreateSystem<TransformSystemGroup>();
            
            transformSystemGroup.SortSystemUpdateList();
        }

        private void OnDestroy()
        {
            IsExist = false;
        }

        private void Update()
        {
#if UNITY_IOS
            if (Application.targetFrameRate != FpsLimit)
                Application.targetFrameRate = FpsLimit;
#endif
        }

        private IEnumerator StartComplete()
        {
            yield return null;

#if UNITY_EDITOR
            CanExecute = true;
#endif
            while (!CanExecute)
                yield return null;

            while (NavMeshSurface.activeSurfaces.Count == 0)
                yield return null;

            NavMesh.avoidancePredictionTime = 1;
            NavMesh.pathfindingIterationsPerFrame = 250;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            em.CreateEntity(ComponentType.ReadOnly<UpdateNavWorldEvent>());
            
            var world = App.GetModule<IWorldGenerator>();
            var matchController = App.GetModule<IMatchController>();
            var playerSystem = App.GetModule<IPlayerSystem>();
            
            while (!world.CompareStatus(ModuleStatus.CompleteInitialize))
                yield return null;
            
            while (!matchController.CompareStatus(ModuleStatus.CompleteInitialize) || !playerSystem.CompareStatus(ModuleStatus.CompleteInitialize))
                yield return null;

            yield return playerSystem.SpawnPlayers();
            matchController.BeginMatch();
            
            Debug.Log("GameScene::StartComplete()");
        }
    }
}
