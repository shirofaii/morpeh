#if UNITY_EDITOR
using System;
namespace Scellecs.Morpeh.Utils.Editor {
    internal readonly struct EntityHandle : IEquatable<EntityHandle> {
        internal readonly Entity entity;
        internal readonly long archetypeHash;

        internal World World => World.Default;
        internal bool IsValid => !this.World.IsDisposed && !this.World.EntityIsDisposed(this.entity);
        internal Archetype Archetype => this.World.entities[this.entity.id].currentArchetype;

        public EntityHandle(Entity entity, long archetypeHash) {
            this.entity = entity;
            this.archetypeHash = archetypeHash;
        }

        public bool ArchetypesEqual(EntityHandle other) {
            return this.archetypeHash.Equals(other.archetypeHash);
        }

        public bool EntitiesEqual(EntityHandle other) {
            return this.entity.Equals(other.entity);
        }

        public bool Equals(EntityHandle other) {
            return this.entity.Equals(other.entity) && this.archetypeHash.Equals(other.archetypeHash);
        }

        public override string ToString() {
            return $"{entity.id}:{entity.generation}, IsValid:{IsValid}, archetypeHash:{archetypeHash}";
        }
    }
}
#endif
