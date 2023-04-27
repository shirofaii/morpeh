#if UNITY_EDITOR
#define MORPEH_DEBUG
#define MORPEH_PROFILING
#endif
#if !MORPEH_DEBUG
#define MORPEH_DEBUG_DISABLED
#endif

namespace Scellecs.Morpeh {
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Collections;
    using JetBrains.Annotations;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public static class FilterExtensions {
        internal static void Dispose(this Filter filter) {
            if (filter.archetypes != null) {
                foreach (var archetype in filter.archetypes) {
                    archetype.RemoveFilter(filter);
                }

                filter.archetypes.Clear();
                filter.archetypes = null;

                filter.chunks.Clear();
                filter.chunks = null;
                filter.archetypesLength = 0;
            }
            
            filter.includedTypeIds?.Clear();
            filter.includedTypeIds = null;
            filter.excludedTypeIds?.Clear();
            filter.excludedTypeIds = null;
            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddArchetype(this Filter filter, Archetype archetype, Entity entity) {
            foreach (var excludedTypeId in filter.excludedTypeIds) {
                var stash = filter.world.GetStash(excludedTypeId);
                if (stash == null) {
                    continue;
                }
                if (stash.Has(entity)) {
                    return;
                }
            }
        
            filter.archetypes.Add(archetype);
            filter.archetypesLength++;
            archetype.AddFilter(filter);
            if (filter.chunks.capacity < filter.archetypesLength) {
                filter.chunks.Resize(filter.archetypes.capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddArchetypes(this Filter filter) {
            foreach (var arch in filter.world.archetypes) {
                filter.CheckArchetype(filter.world.archetypes.GetValueByIndex(arch));
            }
            if (filter.chunks.capacity < filter.archetypes.length) {
                filter.chunks.Resize(filter.archetypes.capacity);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveArchetype(this Filter filter, Archetype archetype) {
            filter.archetypes.RemoveSwapSave(archetype, out _);
            filter.archetypesLength--;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckArchetype(this Filter filter, Archetype archetype) {
            int entityId;
            
            //todo fix it
            if (archetype.entities.length == 0) {
                return;
            }
            var enumerator = archetype.entities.GetEnumerator();
            enumerator.MoveNext();
            entityId = enumerator.Current;
           
            var ent = filter.world.GetEntity(entityId);

            foreach (var includedTypeId in filter.includedTypeIds) {
                var stash = filter.world.GetStash(includedTypeId);
                if (stash == null) {
                    return;
                }
                if (!stash.Has(ent)) {
                    return;
                }
            }

            foreach (var excludedTypeId in filter.excludedTypeIds) {
                var stash = filter.world.GetStash(excludedTypeId);
                if (stash == null) {
                    continue;
                }
                if (stash.Has(ent)) {
                    return;
                }
            }

            filter.archetypes.Add(archetype);
            filter.archetypesLength++;
            archetype.AddFilter(filter);
        }

        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity GetEntity(this Filter filter, in int id) {
            // ReSharper disable once GenericEnumeratorNotDisposed
            var enumerator = filter.GetEnumerator();
            for (int i = 0, length = id + 1; i < length; i++) {
                if (enumerator.MoveNext() == false) {
                    throw new IndexOutOfRangeException();
                }
            }

            return enumerator.Current;
        }

        [NotNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity First(this Filter filter) {
            // ReSharper disable once GenericEnumeratorNotDisposed
            var enumerator = filter.GetEnumerator();
            if (enumerator.MoveNext()) {
                return enumerator.Current;
            }

            throw new InvalidOperationException("The source sequence is empty.");
        }

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity FirstOrDefault(this Filter filter) {
            if (filter.archetypesLength == 0) {
                return default;
            }
            // ReSharper disable once GenericEnumeratorNotDisposed
            var enumerator = filter.GetEnumerator();
            if (enumerator.MoveNext()) {
                return enumerator.Current;
            }

            return default;
        }
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLengthSlow(this Filter filter) {
            filter.world.ThreadSafetyCheck();
            int accum = 0;

            for (int i = 0, length = filter.archetypes.length; i < length; i++) {
                var arch = filter.archetypes.data[i];
                accum += arch.entities.count;
            }
            return accum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this Filter filter) {
            filter.world.ThreadSafetyCheck();

            return filter.archetypesLength == 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotEmpty(this Filter filter) {
            filter.world.ThreadSafetyCheck();

            return filter.archetypesLength != 0;
        }

        public static FilterBuilder With<T>(this FilterBuilder builder) where T : struct, IComponent
            => new FilterBuilder {
                parent = builder,
                world = builder.world,
                mode = Filter.Mode.Include,
                typeId = TypeIdentifier<T>.info.id,
                offset = TypeIdentifier<T>.info.offset,
                level = builder.level + 1
            };

        public static FilterBuilder Without<T>(this FilterBuilder builder) where T : struct, IComponent
            => new FilterBuilder {
                parent = builder,
                world = builder.world,
                mode = Filter.Mode.Exclude,
                typeId = TypeIdentifier<T>.info.id,
                level = builder.level + 1
            };

        public static FilterBuilder Extend<T>(this FilterBuilder builder) where T : struct, IFilterExtension {
            var newFilter = default(T).Extend(builder);
            return newFilter;
        }

        public static Filter Build(this FilterBuilder builder) {
            var includedTypeIds = new FastList<long>();
            var excludedTypeIds = new FastList<long>();
            var includedOffsets = new FastList<long>();
            
            var current = builder;

            while (current.parent != null) {
                if (current.mode == Filter.Mode.Include) {
                    includedTypeIds.Add(current.typeId);
                    includedOffsets.Add(current.offset);
                }
                else if (current.mode == Filter.Mode.Exclude) {
                    excludedTypeIds.Add(current.typeId);
                }
                current = current.parent;
            }
            
            includedOffsets.data.InsertionSort(0, includedOffsets.length);
            return new Filter(builder.world, includedTypeIds, excludedTypeIds, includedOffsets);
        }
    }
}
