﻿using System.Collections.Concurrent;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationScope : ISolidConfigurationScope
    {
        private ConcurrentDictionary<string, object> _items = new ConcurrentDictionary<string, object>();

        protected SolidConfigurationScope(ISolidConfigurationScope parentScope)
        {
            ParentScope = parentScope;
        }

        public T GetValue<T>(string key, bool searchParentScope)
        {
            object val;
            if (_items.TryGetValue(key, out val))
            {
                return (T)val;
            }
            if(searchParentScope &&  ParentScope != null)
            {
                return ParentScope.GetValue<T>(key, searchParentScope);
            }
            return default(T);
        }

        public void SetValue<T>(string key, T value)
        {
            _items[key] = value;
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