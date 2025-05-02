namespace Scellecs.Morpeh {
    using System;
    
    public interface IStash : IDisposable { 
        internal void Clean(Entity entity);
    }
}