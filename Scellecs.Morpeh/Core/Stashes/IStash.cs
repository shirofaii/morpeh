namespace Scellecs.Morpeh {
    using System;
    
    public interface IStash : IDisposable {
        public Type Type { get; }

        public bool Has(Entity entity);
        internal void Clean(Entity entity);
    }
}