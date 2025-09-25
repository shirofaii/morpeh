namespace Scellecs.Morpeh {
    using System;
    using System.Collections.Generic;
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
        public readonly   int                 Id;
        internal readonly int                 generation;

        public Entity(int id, int generation) {
            this.Id         = id;
            this.generation = generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Entity lhs, Entity rhs) {
            return lhs.Id == rhs.Id && lhs.generation == rhs.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Entity lhs, Entity rhs) {
            return lhs.Id != rhs.Id || lhs.generation != rhs.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Entity other) {
            return other.Id == this.Id && other.generation == this.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is Entity other && this.Equals(other);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() {
            return ((long)this.Id + this.generation).GetHashCode();
        }
        
        public int CompareTo(Entity other) {
            return this.Id.CompareTo(other.Id);
        }

        public override string ToString() {
            return $"{this.Id}:{this.generation}";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create() => World.Default.CreateEntity();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity Create(string name) => World.Default.CreateEntity(name);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove() => World.Default.RemoveEntity(this);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDisposed() => World.Default.EntityIsDisposed(this);
    }
}
