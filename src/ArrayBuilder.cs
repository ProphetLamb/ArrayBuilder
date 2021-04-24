using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace ArrayBuilder
{
    /// <summary>
    /// Represents a strongly typed list of objects that allows retrieving the internal array.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Close"/> to retrieve a reference to the internal array.
    /// This will change the state from open to closed.
    /// Subsequent calls and assignments to publicly visible implicit and explicit members will throw <see cref="ObjectDisposedException"/>, save for <see cref="IDisposable"/> members and <see cref="IsClosed"/>.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    // Most of code taken from https://source.dot.net/#System.Private.CoreLib/List.cs.
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class ArrayBuilder<T> : IList<T>, IList, IReadOnlyList<T>, IDisposable
    {
        internal const int StateOpen = 0;
        internal const int StateClosed = 1;
        internal const int StateDisposed = 3;
        internal const int DefaultCapacity = 4;

        internal T[] _items;
        internal int _size;
        private int _version;
        private int _state;
        
#pragma warning disable CA1825 // Avoid zero-length array allocations.
        private static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations.

        public ArrayBuilder()
        {
            _items = s_emptyArray;
        }

        public ArrayBuilder(int capacity)
        {
            if (capacity < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException("capacity", "ArgumentOutOfRange_NeedNonNegNum");

            _items = capacity == 0 ? s_emptyArray : new T[capacity];
        }

        public ArrayBuilder(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException("collection");
            }
            if (collection is ICollection<T> c)
            {
                _items = new T[c.Count];
                c.CopyTo(_items, 0);
            }
            else
            {
                _items = s_emptyArray;
                using var en = collection.GetEnumerator();
                while (en.MoveNext())
                    Add(en.Current);
            }
        }

        public int Capacity
        {
            get
            {
                EnsureStateOpen();
                return _items!.Length;
            }
            set
            {
                EnsureStateOpen();
                if (value < _size)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException("value", "ArgumentOutOfRange_SmallCapacity");
                }
                if (value == _items.Length)
                    return;
                if (value <= 0)
                {
                    _items = s_emptyArray;
                    return;
                }
                T[] newItems = new T[value];
                if (_size > 0)
                    Array.Copy(_items, newItems, _size);
                _items = newItems;
            }
        }

        public int Count
        {
            get
            {
                EnsureStateOpen();
                return _size;
            }
        }

        /// <summary>
        /// Indicates whether the instance is closed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">If the <see cref="ArrayBuilder{T}"/> is disposed.</exception>
        public bool IsClosed
        {
            get
            {
                if (_state == StateDisposed)
                    ThrowHelper.ThrowObjectDisposedException("this", "disposed");
                return _state == StateClosed;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                EnsureStateOpen();
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                EnsureStateOpen();
                return this;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                EnsureStateOpen();
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                EnsureStateOpen();
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                EnsureStateOpen();
                return false;
            }
        }

        public T this[int index]
        {
            get
            {
                EnsureStateOpen();
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                }
                return _items[index];
            }
            set
            {
                EnsureStateOpen();
                if ((uint)index >= (uint)_size)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                }
                _items[index] = value;
                _version++;
            }
        }

        private static bool IsCompatibleObject(object? value)
        {
            return (value is T) || (value == null && default(T) == null);
        }

        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, "value");
                try
                {
                    this[index] = (T)value!;
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        { 
            EnsureStateOpen();
            _version++;
            T[] array = _items;
            int size = _size;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddWithResize(T item)
        {
            Debug.Assert(_size == _items.Length);
            int size = _size;
            Grow(size + 1);
            _size = size + 1;
            _items[size] = item;
        }

        int IList.Add(object? item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, nameof(item));
            try
            {
                Add((T)item!);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
            }
 
            return Count - 1;
        }
        
        public void AddRange(IEnumerable<T> collection) => InsertRange(_size, collection);
        
        [Pure]
        public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
        {
            EnsureStateOpen();
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (count < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");
            if (_size - index < count)
                ThrowHelper.ThrowArgumentException("Argument_InvalidOffLen");
 
            return Array.BinarySearch<T>(_items, index, count, item, comparer);
        }
 
        [Pure]
        public int BinarySearch(T item)
            => BinarySearch(0, Count, item, null);
 
        [Pure]
        public int BinarySearch(T item, IComparer<T>? comparer)
            => BinarySearch(0, Count, item, comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            EnsureStateOpen();
            _version++;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                int size = _size;
                _size = 0;
                if (size > 0)
                {
                    Array.Clear(_items, 0, size); // Clear the elements so that the gc can reclaim the references.
                }
            }
            else
            {
                _size = 0;
            }
        }

        /// <summary>
        /// Changes the state from open to closed.
        /// </summary>
        /// <returns>A reference to the internal array.</returns>
        /// <remarks>
        /// Subsequent calls and assignments to publicly visible implicit and explicit members will throw <see cref="ObjectDisposedException"/>, save for <see cref="IDisposable"/> members and <see cref="IsClosed"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the <see cref="ArrayBuilder{T}"/> is closed or disposed.</exception>
        public T[] Close()
        {
            EnsureStateOpen();
            _state = StateClosed;
            var internalArray = _items;
            _items = null!;
            return internalArray;
        }

        /// <summary>
        /// Changes the state from open to closed. Return a <see cref="Span{T}"/> of a portion of the internal array, encompassing the virtual size of the list.
        /// </summary>
        /// <returns>A span of a portion of the internal array, encompassing the virtual size of the list.</returns>
        /// <remarks>
        /// Subsequent calls and assignments to publicly visible implicit and explicit members will throw <see cref="ObjectDisposedException"/>, save for <see cref="IDisposable"/> members and <see cref="IsClosed"/>.
        /// </remarks>
        public Span<T> CloseAndSlice()
        {
            return CloseAndSlice(0, _size);
        }
        
        /// <summary>
        /// Changes the state from open to closed. Returns a <see cref="Span{T}"/> of a portion of the internal array.
        /// </summary>
        /// <param name="count">The number of items in the <see cref="Span{T}"/>.</param>
        /// <returns>A span of a portion of the internal array, with specified <paramref name="count"/>.</returns>
        /// <remarks>
        /// Subsequent calls and assignments to publicly visible implicit and explicit members will throw <see cref="ObjectDisposedException"/>, save for <see cref="IDisposable"/> members and <see cref="IsClosed"/>.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="count"/> is greater then <see cref="Count"/>.</exception>
        public Span<T> CloseAndSlice(int count)
        {
            return CloseAndSlice(0, count);
        }
        
        /// <summary>
        /// Changes the state from open to closed. Returns a <see cref="Span{T}"/> of a portion of the internal array.
        /// </summary>
        /// <param name="index">The index at which to begin the <see cref="Span{T}"/>.</param>
        /// <param name="count">The number of items in the <see cref="Span{T}"/>.</param>
        /// <returns>A span of a portion of the internal array, starting at the <paramref name="index"/>, with specified <paramref name="count"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="index"/>, <paramref name="count"/>,
        /// or <paramref name="index"/> + <paramref name="count"/> is not in the range of the <see cref="ArrayBuilder{T}"/>.</exception>
        public Span<T> CloseAndSlice(int index, int count)
        {
            if (_size - index < count)
            {
                ThrowHelper.ThrowArgumentException("Argument_InvalidOffLen");
            }
            return Close().AsSpan(index, count);
        }
 
        public bool Contains(T item)
        {
            EnsureStateOpen();
            return _size != 0 && IndexOf(item) != -1;
        }
 
        bool IList.Contains(object? item)
        {
            EnsureStateOpen();
            if (IsCompatibleObject(item))
            {
                return Contains((T)item!);
            }
            return false;
        }

        public void CopyTo(T[] array) => CopyTo(array, 0);

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            EnsureStateOpen();
            if ((array != null) && (array.Rank != 1))
            {
                ThrowHelper.ThrowArgumentException("Arg_RankMultiDimNotSupported");
            }
 
            try
            {
                Array.Copy(_items, 0, array!, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
            }
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            EnsureStateOpen();
            if (_size - index < count)
            {
                ThrowHelper.ThrowArgumentException("Argument_InvalidOffLen");
            }
            Array.Copy(_items, index, array, arrayIndex, count);
        }
 
        public void CopyTo(T[] array, int arrayIndex)
        {
            EnsureStateOpen();
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }


        public void Dispose()
        {
            if (_state == StateDisposed)
                return;
            _state = StateDisposed;
            _items = null!;
            _size = -1;
            _version = -1;
        }

        /// <summary>
        /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
        /// If the current capacity of the list is less than specified <paramref name="capacity"/>,
        /// the capacity is increased by continuously twice current capacity until it is at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        public int EnsureCapacity(int capacity)
        {
            EnsureStateOpen();
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("capacity", "ArgumentOutOfRange_NeedNonNegNum");
            }
            if (_items.Length < capacity)
            {
                Grow(capacity);
                _version++;
            }
 
            return _items.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureStateOpen()
        {
            if (_state == StateClosed)
                ThrowHelper.ThrowObjectDisposedException_Closed("this");
            else if (_state != StateOpen)
                ThrowHelper.ThrowObjectDisposedException("this", "disposed");
        }
        
        /// <summary>
        /// Increase the capacity of this list to at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        private void Grow(int capacity)
        {
            Debug.Assert(_items.Length < capacity);
 
            int newcapacity = _items.Length == 0 ? DefaultCapacity : 2 * _items.Length;
 
            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newcapacity > 0x7FEFFFFF) newcapacity = 0x7FEFFFFF;
 
            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding MaxArrayLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < capacity) newcapacity = capacity;
 
            Capacity = newcapacity;
        }

        public IEnumerator<T> GetEnumerator()
        {
            EnsureStateOpen();
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        [Pure]
        public int IndexOf(T item)
        {
            EnsureStateOpen();
            return Array.IndexOf(_items, item, 0, _size);
        }

        [Pure]
        int IList.IndexOf(object? item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T)item!);
            }
            return -1;
        }
        
        [Pure]
        public int IndexOf(T item, int index)
        {
            EnsureStateOpen();
            if (index > _size)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            return Array.IndexOf(_items, item, index, _size - index);
        }
        
        [Pure]
        public int IndexOf(T item, int index, int count)
        {
            EnsureStateOpen();
            if (index > _size)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
 
            if (count < 0 || index > _size - count)
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
 
            return Array.IndexOf(_items, item, index, count);
        }

        public void Insert(int index, T item)
        {
            EnsureStateOpen();
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("index", "ArgumentOutOfRange_ListInsert");
            }
            if (_size == _items.Length) Grow(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }
 
        void IList.Insert(int index, object? item)
        {
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(item, "item");
 
            try
            {
                Insert(index, (T)item!);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(item, typeof(T));
            }
        }
        
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            EnsureStateOpen();
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException("collection");
            }
 
            if ((uint)index > (uint)_size)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }
 
            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    if (_items.Length - _size < count)
                    {
                        Grow(_size + count);
                    }
                    if (index < _size)
                    {
                        Array.Copy(_items, index, _items, index + count, _size - index);
                    }
 
                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (ReferenceEquals(this, c))
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        c.CopyTo(_items, index);
                    }
                    _size += count;
                }
            }
            else
            {
                using IEnumerator<T> en = collection.GetEnumerator();
                while (en.MoveNext())
                {
                    Insert(index++, en.Current);
                }
            }
            _version++;
        }
        
        [Pure]
        public int LastIndexOf(T item)
        {
            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }
            else
            {
                return LastIndexOf(item, _size - 1, _size);
            }
        }
 
        [Pure]
        public int LastIndexOf(T item, int index)
        {
            if (index >= _size)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            return LastIndexOf(item, index, index + 1);
        }
 
        [Pure]
        public int LastIndexOf(T item, int index, int count)
        {
            EnsureStateOpen();
            if ((Count != 0) && (index < 0))
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }
 
            if ((Count != 0) && (count < 0))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");
            }
 
            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }
 
            if (index >= _size)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("index", "ArgumentOutOfRange_BiggerThanCollection");
            }
 
            if (count > index + 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("count", "ArgumentOutOfRange_BiggerThanCollection");
            }
 
            return Array.LastIndexOf(_items, item, index, count);
        }

        public bool Remove(T item)
        {
            EnsureStateOpen();
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
 
            return false;
        }
 
        void IList.Remove(object? item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T)item!);
            }
        }
        
        public void RemoveAt(int index)
        {
            EnsureStateOpen();
            if ((uint)index >= (uint)_size)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _items[_size] = default!;
            }
            _version++;
        }

        public void RemoveRange(int index, int count)
        {
            EnsureStateOpen();
            if (index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }
 
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException("count", "ArgumentOutOfRange_NeedNonNegNum");
            }
 
            if (_size - index < count)
                ThrowHelper.ThrowArgumentException("Argument_InvalidOffLen");
 
            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }
 
                _version++;
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(_items, _size, count);
                }
            }
        }

        /// <summary>
        /// Trims the internal array to the virtual size, then <see cref="Close"/>s the instance.
        /// </summary>
        /// <returns>A reference to the internal array.</returns>
        /// <remarks>
        /// Subsequent calls and assignments to publicly visible implicit and explicit members will throw <see cref="ObjectDisposedException"/>, save for <see cref="IDisposable"/> members and <see cref="IsClosed"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">If the <see cref="ArrayBuilder{T}"/> is closed or disposed.</exception>
        public T[] TrimAndClose()
        {
            EnsureStateOpen();
            if (_size == _items.Length)
            {
                return Close();
            }
            T[] trimmed = new T[_size];
            T[] items = Close();
            _items = trimmed;
            Array.Copy(items, trimmed, trimmed.Length);
            return trimmed;
        }
        
        public void TrimExcess()
        {
            EnsureStateOpen();
            int threshold = (int)(((double)_items.Length) * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }
        
        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayBuilder<T> _list;
            private int _index;
            private readonly int _version;
            [AllowNull] private T _current;

            internal Enumerator(ArrayBuilder<T> list)
            {
                list.EnsureStateOpen();
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ArrayBuilder<T> localList = _list;
                localList.EnsureStateOpen();

                if (_version == localList._version && ((uint)_index < (uint)localList._size))
                {
                    _current = localList._items[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = _list._size + 1;
                _current = default;
                return false;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._size + 1)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                _list.EnsureStateOpen();
                if (_version != _list._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _current = default;
            }
        }
    }
}
