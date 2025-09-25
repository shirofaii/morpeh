namespace Scellecs.Morpeh {
    using System.Runtime.CompilerServices;
    using JetBrains.Annotations;
    using Scellecs.Morpeh.Collections;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class WorldEntityExtensions {
        [PublicAPI]
        public static Entity CreateEntity(this World world) {
            world.ThreadSafetyCheck();

            if (!world.freeEntityIDs.TryPop(out var id)) {
                id = ++world.entitiesLength;
            }

            if (world.entitiesLength >= world.entitiesCapacity) {
                world.ExpandEntities();
            }

            ++world.entitiesCount;
            return world.GetEntityAtIndex(id);
        }

        [PublicAPI]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateEntity(this World world, string name) {
            var entity = world.CreateEntity();
#if DEBUG
            world.SetDebugLabel(entity.id, name);
#endif
            return entity;
        }

        [PublicAPI]
        public static Entity GetEntity(this World world, int id) {
            return id == 0 ? default : world.GetEntityAtIndex(id);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ExpandEntities(this World world) {
            var oldCapacity = world.entitiesCapacity;
            var newCapacity = HashHelpers.GetCapacity(world.entitiesCapacity) + 1;
            
            ArrayHelpers.Grow(ref world.entities, newCapacity);
            for (var i = oldCapacity; i < newCapacity; i++)
            {
                world.entities[i].Initialize();
            }
            
            ArrayHelpers.Grow(ref world.entitiesGens, newCapacity);
            
            world.dirtyEntities.Resize(newCapacity);
            world.disposedEntities.Resize(newCapacity);

            world.entitiesCapacity = newCapacity;
        }

        [PublicAPI]
        public static void RemoveEntity(this World world, Entity entity) {
            world.ThreadSafetyCheck();
            
            if (world.EntityIsDisposed(entity)) {
#if MORPEH_DEBUG
                MLogger.LogError($"You're trying to dispose disposed entity {entity}.");
#endif
                return;
            }
            
            ref var entityData = ref world.entities[entity.id];
            
            // Clear new components if entity is transient
            
            if (world.dirtyEntities.Remove(entity.id)) {
                var addedComponentsCount = entityData.addedComponentsCount;
                
                for (var i = 0; i < addedComponentsCount; i++) {
                    var typeId = entityData.addedComponents[i];
                    world.GetExistingStash(typeId)?.Clean(entity);
                }
            }
            
            // Clear components from existing archetype
            
            if (entityData.currentArchetype != null) {
                foreach (var typeId in entityData.currentArchetype.components) {
                    world.GetExistingStash(typeId)?.Clean(entity);
                }
            }
            
            world.disposedEntities.Add(entity.id);
            
            world.IncrementGeneration(entity.id);
            --world.entitiesCount;
        }

        [PublicAPI]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EntityIsDisposed(this World world, Entity entity) {
            world.ThreadSafetyCheck();
            
            return entity.id <= 0 ||
                   entity.id >= world.entitiesCapacity ||
                   world.entitiesGens[entity.id] != entity.generation;
        }

        [PublicAPI]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this World world, Entity entity) {
            world.ThreadSafetyCheck();
            
            return entity.id > 0 &&
                   entity.id < world.entitiesCapacity &&
                   world.entitiesGens[entity.id] == entity.generation;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Entity GetEntityAtIndex(this World world, int entityId) {
            return new Entity(entityId, world.entitiesGens[entityId]);
        }
        
#if DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetDebugLabel(this World world, int entityId, string label) {
            world.entities[entityId].debugLabel = label;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string GetDebugLabel(this World world, int entityId) {
            return world.entities[entityId].debugLabel;
        }
#endif
    }
}