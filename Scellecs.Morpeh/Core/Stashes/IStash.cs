namespace Scellecs.Morpeh {
    using System;
    
    public interface IStash : IDisposable {
        public Type Type { get; }

        internal void Clean(Entity entity);
    }
}