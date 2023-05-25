using System;
using System.Collections;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS.Game.Systems;
using GameRulez.Modules.PlayerSystems;
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace GameRules.Scripts.ECS.Render
{
    [ExecuteAlways]
    public class RenderSettings : MonoBehaviour
    {
        [InlineProperty]
        [HideLabel]
        public UnitRenderSystem.DrawSettings NoTeamSettings;

        private IEnumerator Start()
        {
            while (!App.IsInitialize)
                yield return null;
            
            ForceUpdate();
        }

        [Button]
        public void ForceUpdate()
        {
            if(World.DefaultGameObjectInjectionWorld == null)
                return;
            
            var matchSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GameMatchSystem>();
            var color = NoTeamSettings.Material.GetColor("_BaseColor");
            matchSystem.UpdateTeamInfo(0, "no_team", color, color, NoTeamSettings);
        }
    }
}