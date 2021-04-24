using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace ArrayBuilder.Tests
{
    public static class Helpers
    {
        public static void AssertSequenceEqual<T>(IEnumerable<T> comparable, IEnumerable<T> probe)
        {
            Assert.IsTrue(comparable.SequenceEqual(probe));
        }
    }
}