﻿#if MORPEH_BURST
namespace Morpeh.Native {
    using Unity.Collections.LowLevel.Unsafe;
    
    public struct NativeWorld {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe int* entitiesGens;

        [NativeDisableUnsafePtrRestriction]
        internal unsafe int* entitiesCapacity;
    }
}
#endif