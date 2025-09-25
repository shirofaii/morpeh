namespace Scellecs.Morpeh {
    using System;
    using Sirenix.OdinInspector;
    using Unity.IL2CPP.CompilerServices;
    using System.Runtime.CompilerServices;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

#if DEBUG && !DEVELOPMENT_BUILD
    [DebuggerTypeProxy(typeof(EntityDebuggerProxy))]
#endif
    [Serializable]
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public readonly struct Entity : IEquatable<Entity> {
        public readonly int id;
        internal readonly int generation;
        
        public Entity(int id, int generation) {
            this.id         = id;
            this.generation = generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Entity lhs, Entity rhs) {
            return lhs.id == rhs.id && lhs.generation == rhs.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Entity lhs, Entity rhs) {
            return lhs.id != rhs.id || lhs.generation != rhs.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other) {
            return other.id == this.id && other.generation == this.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is Entity other && this.Equals(other);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() {
            return ((long)this.id + this.generation).GetHashCode();
        }
        
        public int CompareTo(Entity other) {
            return this.id.CompareTo(other.id);
        }

        public override string ToString() {
            return $"{this.id}:{this.generation}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create() => World.Default.CreateEntity();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove() => World.Default.RemoveEntity(this);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDisposed() => World.Default.EntityIsDisposed(this);
        
    }
}
