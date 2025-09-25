using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public readonly struct EqArray<T> : IEquatable<EqArray<T>>, IEnumerable<T>
where T : IEquatable<T>
{
    readonly T[] array;

    public EqArray() // for collection literal []
    {
        array = [];
    }

    public EqArray(T[] array)
    {
        this.array = array;
    }

    public static implicit operator EqArray<T>(T[] array)
    {
        return new EqArray<T>(array);
    }

    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref array![index];
    }

    public int Length => array!.Length;

    public ReadOnlySpan<T> AsSpan()
    {
        return array.AsSpan();
    }

    public ReadOnlySpan<T>.Enumerator GetEnumerator()
    {
        return AsSpan().GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return array.AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return array.AsEnumerable().GetEnumerator();
    }

    public bool Equals(EqArray<T> other)
    {
        return AsSpan().SequenceEqual(other.AsSpan());
    }
}