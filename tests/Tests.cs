using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace ArrayBuilder.Tests
{
    public class Tests
    {
        [Test]
        public void TestClose()
        {
            using (var builder = new ArrayBuilder<string?>())
            {
                builder.AddRange(Enumerable.Repeat("element", 4));
                var array = builder.Close();
                Helpers.AssertSequenceEqual(Enumerable.Repeat("element", 4), array);

                Assert.DoesNotThrow(() => _ = builder.IsClosed);
                AssertPublicMembersThrow(builder, array);
                AssertListMembersThrow(builder, array);
                AssertCollectionMembersThrow(builder, array);
                AssertEnumerableMembersThrow(builder, array);
                AssertGenericCollectionMembersThrow(builder, array);
            }
            Assert.Pass();
        }

        [Test]
        public void TestDispose()
        {
            string?[] array;
            ArrayBuilder<string?> builder;
            using (builder = new ArrayBuilder<string?>())
            {
                builder.AddRange(Enumerable.Repeat("element", 4));
                array = builder.Close();
                Helpers.AssertSequenceEqual(Enumerable.Repeat("element", 4), array);
            }
            Assert.Throws<ObjectDisposedException>(() => _ = builder.IsClosed);
            AssertPublicMembersThrow(builder, array);
            AssertListMembersThrow(builder, array);
            AssertCollectionMembersThrow(builder, array);
            AssertEnumerableMembersThrow(builder, array);
            AssertGenericCollectionMembersThrow(builder, array);
            Assert.Pass();
        }

        public void AssertListMembersThrow(ArrayBuilder<string?> builder, string?[] array)
        {
            // List explicit methods & properties
            IList list = builder;
            Assert.Throws<ObjectDisposedException>(() => _ = list.Add("element"));
            Assert.Throws<ObjectDisposedException>(() => list.Insert(0, "item"));
            Assert.Throws<ObjectDisposedException>(() => list.Remove("element"));
            Assert.Throws<ObjectDisposedException>(() => list.RemoveAt(0));
            Assert.Throws<ObjectDisposedException>(() => list.GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => list.Clear());
            Assert.Throws<ObjectDisposedException>(() => _ = list.IsFixedSize);
            Assert.Throws<ObjectDisposedException>(() => _ = list.IsReadOnly);
            Assert.Throws<ObjectDisposedException>(() => _ = list.IsSynchronized);
            Assert.Throws<ObjectDisposedException>(() => _ = list.Count);
            Assert.Throws<ObjectDisposedException>(() => _ = list.SyncRoot);
        }
        
        public void AssertCollectionMembersThrow(ArrayBuilder<string?> builder, string?[] array)
        {
            // Collection explicit methods & properties
            ICollection collection = builder;
            Assert.Throws<ObjectDisposedException>(() => _ = collection.IsSynchronized);
            Assert.Throws<ObjectDisposedException>(() => _ = collection.SyncRoot);
            Assert.Throws<ObjectDisposedException>(() => _ = collection.Count);
            Assert.Throws<ObjectDisposedException>(() => collection.CopyTo(array, 0));
            Assert.Throws<ObjectDisposedException>(() => _ = collection.GetEnumerator());
        }
        
        public void AssertEnumerableMembersThrow(ArrayBuilder<string?> builder, string?[] array)
        {
            // Enumerable explicit methods & properties
            IEnumerable sequence = builder;
            Assert.Throws<ObjectDisposedException>(() => _ = sequence.GetEnumerator());
        }

        public void AssertGenericCollectionMembersThrow(ArrayBuilder<string?> builder, string?[] array)
        {
            // Generic collection explicit methods & properties
            ICollection<string?> genericCollection = builder;
            Assert.Throws<ObjectDisposedException>(() => _ = genericCollection.IsReadOnly);
            Assert.Throws<ObjectDisposedException>(() => _ = genericCollection.Count);
            Assert.Throws<ObjectDisposedException>(() => genericCollection.CopyTo(array, 0));
            Assert.Throws<ObjectDisposedException>(() => genericCollection.Clear());
            Assert.Throws<ObjectDisposedException>(() => genericCollection.Add(null));
            Assert.Throws<ObjectDisposedException>(() => genericCollection.Remove("element"));
            Assert.Throws<ObjectDisposedException>(() => _ = genericCollection.Contains("item"));
            Assert.Throws<ObjectDisposedException>(() => _ = genericCollection.Contains("item"));
        }

        public void AssertPublicMembersThrow(ArrayBuilder<string?> builder, string?[] array)
        {
            // Public methods & properties except for IsClosed
            Assert.Throws<ObjectDisposedException>(() => builder.Capacity -= 1);
            Assert.Throws<ObjectDisposedException>(() => builder[0] = "closed");
            Assert.Throws<ObjectDisposedException>(() => builder.Add("closed"));
            Assert.Throws<ObjectDisposedException>(() => builder.AddRange(Enumerable.Repeat("closed", 2)));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.BinarySearch(0, 4, "element", StringComparer.Ordinal));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.BinarySearch("element"));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.BinarySearch("element", StringComparer.Ordinal));
            Assert.Throws<ObjectDisposedException>(() => builder.Clear());
            Assert.Throws<ObjectDisposedException>(() => _ = builder.Close());
            Assert.Throws<ObjectDisposedException>(() => _ = builder.CloseAndSlice());
            Assert.Throws<ObjectDisposedException>(() => _ = builder.CloseAndSlice(1));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.CloseAndSlice(1, 1));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.Contains("element"));
            Assert.Throws<ObjectDisposedException>(() => builder.CopyTo(array));
            Assert.Throws<ObjectDisposedException>(() => builder.CopyTo(0, array, 0, 4));
            Assert.Throws<ObjectDisposedException>(() => builder.CopyTo(array, 0));
            Assert.Throws<ObjectDisposedException>(() => builder.EnsureCapacity(12));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.GetEnumerator());
            Assert.Throws<ObjectDisposedException>(() => _ = builder.IndexOf("element"));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.IndexOf("element", 0));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.IndexOf("element", 0, 4));
            Assert.Throws<ObjectDisposedException>(() => builder.Insert(0, "closed"));
            Assert.Throws<ObjectDisposedException>(() => builder.InsertRange(0, Enumerable.Repeat("closed", 2)));
            Assert.Throws<ObjectDisposedException>(() => builder.Remove("element"));
            Assert.Throws<ObjectDisposedException>(() => builder.RemoveAt(2));
            Assert.Throws<ObjectDisposedException>(() => builder.RemoveRange(0, 3));
            Assert.Throws<ObjectDisposedException>(() => _ = builder.TrimAndClose());
            Assert.Throws<ObjectDisposedException>(() => builder.TrimExcess());
        }
    }
}