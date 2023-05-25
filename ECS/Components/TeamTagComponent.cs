using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace GameRules.Scripts.ECS.Components
{
    [GenerateAuthoringComponent]
    public struct TeamTagComponent : IComponentData
    {
        public int Value;
    }
    
    /*[StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct TeamTagComponent : ISharedComponentData, IEquatable<TeamTagComponent>
    {
        public byte Team;

        public bool Equals(TeamTagComponent other)
        {
            return Team == other.Team;
        }

        public override bool Equals(object obj)
        {
            return obj is TeamTagComponent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Team.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEqualsOrNull(TeamTagComponent other)
        {
            return Team == byte.MaxValue || Team == other.Team;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEqualsOrNull(byte otherTeam)
        {
            return Team == byte.MaxValue || Team == otherTeam;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqualsOrNull(byte sourceTeam, byte otherTeam)
        {
            return sourceTeam == otherTeam || sourceTeam == byte.MaxValue;
        }
    }*/
}