// Generated by StaticGenerators/PinnedArrayGenerator.cs

#if ENABLE_MONO || ENABLE_IL2CPP
#define MORPEH_UNITY
#endif

namespace Scellecs.Morpeh.Collections {
    using System;
    using System.Runtime.CompilerServices;
#if MORPEH_UNITY
    using Unity.Collections.LowLevel.Unsafe;
#else
    using System.Runtime.InteropServices;
#endif
    using Unity.IL2CPP.CompilerServices;

    // START: Extra usings

    // END: Extra usings

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public unsafe struct LongHashMapSlotPinnedArray : IDisposable {
        public LongHashMapSlot[] data;
        public LongHashMapSlot* ptr;
#if MORPEH_UNITY
        public ulong handle;
#else
        public GCHandle handle;
#endif
        
        public LongHashMapSlot this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.data[index] = value;
        }
        
        public int Length => this.data.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LongHashMapSlotPinnedArray(int size) {
            this.data = new LongHashMapSlot[size];
#if MORPEH_UNITY
            this.ptr = (LongHashMapSlot*)UnsafeUtility.PinGCArrayAndGetDataAddress(this.data, out this.handle);
#else
            this.handle = GCHandle.Alloc(this.data, GCHandleType.Pinned);
            this.ptr = (LongHashMapSlot*)this.handle.AddrOfPinnedObject();
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Resize(int newSize) {
#if MORPEH_UNITY
            UnsafeUtility.ReleaseGCObject(this.handle);
#else
            this.handle.Free();
#endif
            var newArray = new LongHashMapSlot[newSize];
            var len = this.data.Length;
            Array.Copy(this.data, 0, newArray, 0, newSize >= len ? len : newSize);
            this.data = newArray;
#if MORPEH_UNITY
            this.ptr = (LongHashMapSlot*)UnsafeUtility.PinGCArrayAndGetDataAddress(this.data, out this.handle);
#else
            this.handle = GCHandle.Alloc(newArray, GCHandleType.Pinned);
            this.ptr = (LongHashMapSlot*)this.handle.AddrOfPinnedObject();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            Array.Clear(this.data, 0, this.data.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() {
#if MORPEH_UNITY
            UnsafeUtility.ReleaseGCObject(this.handle);
            this.ptr = (LongHashMapSlot*)IntPtr.Zero;
            this.data = null;
#else
            if (this.handle.IsAllocated) {
                this.handle.Free();
                this.ptr = (LongHashMapSlot*)IntPtr.Zero;
                this.data = null;
            }
#endif
        }

        // START: Enumerator

        // END: Enumerator
    }
}