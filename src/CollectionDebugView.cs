﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ArrayBuilder
{
    internal sealed class ICollectionDebugView<T>
    {
        private readonly ICollection<T> _collection;
 
        public ICollectionDebugView(ICollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }
 
            _collection = collection;
        }
 
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] items = new T[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}
