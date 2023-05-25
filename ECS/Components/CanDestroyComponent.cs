using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Entities;

namespace GameRules.Scripts.Modules.Collisions
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CanDestroyComponent : ISharedComponentData, IEquatable<CanDestroyComponent>
    {
        public bool WaitDestroy;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CanDestroyComponent(bool value)
        {
            WaitDestroy = value;
        }

        public bool Equals(CanDestroyComponent other)
        {
            return other.WaitDestroy == WaitDestroy;
        }

        public override bool Equals(object obj)
        {
            return obj is CanDestroyComponent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return WaitDestroy ? 1 : 0;
        }
    }
}