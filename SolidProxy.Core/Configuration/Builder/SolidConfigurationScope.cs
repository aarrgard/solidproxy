using System.Collections.Concurrent;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        protected SolidConfigurationScope(ISolidConfigurationScope parentScope)
        {
            ParentScope = parentScope;
        }

        public object this[string key]
        {
            get
            {
                object val;
                if (_items.TryGetValue(key, out val))
                {
                    return val;
                }
                if (ParentScope != null)
                {
                    return ParentScope[key];
                }
                return null;
            }
            set
            {
                _items[key] = value;
            }
        }


        /// <summary>
        /// Returns the parent scope
        /// </summary>
        public ISolidConfigurationScope ParentScope { get; }

        /// <summary>
        /// Specifies if this scope is locked
        /// </summary>
        public bool Locked { get; private set; }

        /// <summary>
        /// Locks this scope.
        /// </summary>
        public void Lock()
        {
            Locked = true;
        }
    }

    public abstract class SolidConfigurationScope<T> : SolidConfigurationScope, ISolidConfigurationScope<T> where T : class
    {
        protected SolidConfigurationScope(ISolidConfigurationScope parentScope)
            : base(parentScope)
        {
        }
    }
}