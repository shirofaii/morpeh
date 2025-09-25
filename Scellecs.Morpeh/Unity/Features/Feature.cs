using System.Runtime.CompilerServices;

public abstract class Feature {
    protected const MethodImplOptions inline = MethodImplOptions.AggressiveInlining;

    [MethodImpl(inline)]
    protected bool IsEnabled() => true;
    
    [MethodImpl(inline)]
    protected bool UpdateWhile() => false;
    
    [MethodImpl(inline)]
    public void Init() {}
}