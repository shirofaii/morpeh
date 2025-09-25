using Scellecs.Morpeh;
using Scellecs.Morpeh.Collections;
using Scellecs.Morpeh.Providers;
using UnityEngine;

public static class MorpehUtils {
    public static Entity GetEntity(this GameObject go) {
        if(!go) return default;
        if(!EntityProvider.map.TryGetValue(go.GetInstanceID(), out var item)) return default;
        return item.entity;
    }
}
