using System.Collections;
using System.Collections.Generic;

namespace SolidProxy.Core.Configuration.Builder
{
    public abstract class SolidConfigurationScopeCollection
    {
        public SolidConfigurationScopeCollection()
        {
        }
        public abstract SolidConfigurationScopeCollection Parent { get; set; }
    }
    public class SolidConfigurationScopeCollection<T> : SolidConfigurationScopeCollection, ICollection<T>
    {
        private ICollection<T> _items;
        public SolidConfigurationScopeCollection()
        {
            _items = new List<T>();
        }
        public override SolidConfigurationScopeCollection Parent { get; set; }
        public SolidConfigurationScopeCollection<T> TypedParent => (SolidConfigurationScopeCollection<T>)Parent;

        public int Count => _items.Count + TypedParent.Count;

        public bool IsReadOnly => throw new System.NotImplementedException();


        public void Add(T item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(T item)
        {
            return _items.Contains(item) || TypedParent.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if(Parent != null)
            {
                foreach(var o in TypedParent)
                {
                    yield return o;
                }
            }
            foreach (var o in _items)
            {
                yield return o;
            }
        }

        public bool Remove(T item)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}