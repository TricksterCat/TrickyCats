using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace GameRules.Scripts.ECS.Components
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct RecruitComponent : IComponentData
    {
        public float LockedToTime;
        public int Next;
        public float Force;
    }
}