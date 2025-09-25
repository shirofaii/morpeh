namespace Scellecs.Morpeh {
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using System;
    using System.Reflection;
    using Collections;
    using Unity.IL2CPP.CompilerServices;
    using Unity.Profiling;
    using UnityEngine;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    internal class UnityRuntimeHelper : MonoBehaviour {
        internal static Action             onApplicationFocusLost = () => {};
        internal static UnityRuntimeHelper instance;
        
#if MORPEH_METRICS
        private static readonly ProfilerCounterValue<int> entitiesCounter = new(ProfilerCategory.Scripts, "Entities", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static readonly ProfilerCounterValue<int> archetypesCounter = new(ProfilerCategory.Scripts, "Archetypes", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static readonly ProfilerCounterValue<int> filtersCounter = new(ProfilerCategory.Scripts, "Filters", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static readonly ProfilerCounterValue<int> systemsCounter = new(ProfilerCategory.Scripts, "Systems", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static readonly ProfilerCounterValue<int> commitsCounter = new(ProfilerCategory.Scripts, "Commits", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static readonly ProfilerCounterValue<int> migrationsCounter = new(ProfilerCategory.Scripts, "Migrations", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
        private static readonly ProfilerCounterValue<int> stashResizeCounter = new(ProfilerCategory.Scripts, "Stash Resizes", ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
#endif
        

        private void OnEnable() {
            if (instance == null) {
                instance = this;
#if UNITY_EDITOR
                EditorApplication.playModeStateChanged += this.OnEditorApplicationOnplayModeStateChanged;
#endif
            }
            else {
                Destroy(this.gameObject);
            }
        }

        private void OnDisable() {
            if (instance == this) {
                instance = null;
            }
        }
        
#if UNITY_EDITOR
        private void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredEditMode) {
                if (this != null && this.gameObject != null) {
                    DestroyImmediate(this.gameObject);
                }

                EditorApplication.playModeStateChanged -= this.OnEditorApplicationOnplayModeStateChanged;
            }
        }
#endif

        private void Update() {
            WorldLoopExtensions.GlobalUpdate(Time.deltaTime);
        }

        private void FixedUpdate() {
            WorldLoopExtensions.GlobalFixedUpdate(Time.fixedDeltaTime);
        }

        private void LateUpdate() {
            WorldLoopExtensions.GlobalLateUpdate(Time.deltaTime);
#if MORPEH_METRICS
            var w = World.Default;
            if (w != null) {
                var m = w.metrics;
                entitiesCounter.Value = w.metrics.entities;
                archetypesCounter.Value = w.metrics.archetypes;
                filtersCounter.Value = w.metrics.filters;
                systemsCounter.Value = w.metrics.systems;
                commitsCounter.Value = w.metrics.commits;
                migrationsCounter.Value = w.metrics.migrations;
                stashResizeCounter.Value = w.metrics.stashResizes;
            }
#endif
        }

        internal void OnApplicationPause(bool pauseStatus) {
            if (pauseStatus) {
                onApplicationFocusLost.Invoke();
            }
        }

        internal void OnApplicationFocus(bool hasFocus) {
            if (!hasFocus) {
                onApplicationFocusLost.Invoke();
            }
        }

        internal void OnApplicationQuit() {
            onApplicationFocusLost.Invoke();
        }
    }
}
