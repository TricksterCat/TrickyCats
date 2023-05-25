using System;
using System.Runtime.InteropServices;
using GameRules.Scripts.AnimationSystem.AnimMapBakerTools;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.Extensions;
using GameRules.Scripts.Modules.Database.SupportClasses;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

#if ENABLE_HYBRID_RENDERER
using Unity.Rendering;
#endif

namespace GameRules.Scripts.ECS.Render
{
    public class UnitRenderSystem
    {
        [Serializable]
        public struct DrawSettings
        {
            public Mesh Mesh;
            public Material Material;
            
            [BoxGroup("TRS")]
            public Vector3 Position;
            [BoxGroup("TRS")]
            public Vector3 Rotation;
            [BoxGroup("TRS")]
            public Vector3 Scale;
        }
    }
}
