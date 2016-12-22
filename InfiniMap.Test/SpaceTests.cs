using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace InfiniMap.Test
{
    [TestFixture]
    public class SpaceTests
    {
        [Test]
        public void Equality()
        {
            var tupleToSpace2D = ((1, 2) == new WorldSpace2D(1, 2));
            var tupleToSpace = (1, 2, 0) == new WorldSpace(1, 2, 0);

            var space2DToSpace = (new WorldSpace2D(2, 2) == new WorldSpace(2, 2, 0));

            Assert.That(tupleToSpace2D, Is.True);
            Assert.That(tupleToSpace, Is.True);
            Assert.That(space2DToSpace, Is.True);
        }

        /// <summary>
        /// Ensure that two WorldSpaces(2D) have the same identity as each other
        /// </summary>
        [Test]
        public void KeyIdentity()
        {
            WorldSpace a = new WorldSpace(1, 2, 3);
            WorldSpace b = new WorldSpace(1, 2, 3);

            var dict = new Dictionary<WorldSpace, string>();
            dict.Add(a, a.ToString());

            Assert.Throws<ArgumentException>(() => dict.Add(b, b.ToString()));
            Assert.That(dict.Count == 1);

            WorldSpace2D c = new WorldSpace2D(1, 2);
            WorldSpace2D d = new WorldSpace2D(1, 2);

            var dict2D = new Dictionary<WorldSpace2D, string>();
            dict2D.Add(c, d.ToString());

            Assert.Throws<ArgumentException>(() => dict2D.Add(d, d.ToString()));
            Assert.That(dict2D.Count == 1);
        }

        [Test]
        public void Covariance()
        {
            WorldSpace2D a = new WorldSpace2D(10, 20);
            WorldSpace b = a;

            Assert.That(b.Z, Is.EqualTo(0));
            Assert.That(b.X, Is.EqualTo(10));
            Assert.That(b.Y, Is.EqualTo(20));

            WorldSpace c = new WorldSpace(10, 20, 0);
            WorldSpace2D d = c;

            Assert.That(d.X, Is.EqualTo(10));
            Assert.That(d.Y, Is.EqualTo(20));

            WorldSpace e = (10, 20, 0);
            Assert.That(e.X, Is.EqualTo(10));
            Assert.That(e.Y, Is.EqualTo(20));
            Assert.That(e.Z, Is.EqualTo(0));

            WorldSpace2D f = (10, 20);
            Assert.That(f.X, Is.EqualTo(10));
            Assert.That(f.Y, Is.EqualTo(20));

            Assert.That(a == b); Assert.That(b == a);
            Assert.That(b == c); Assert.That(c == b);
            Assert.That(c == d); Assert.That(d == c);
            Assert.That(d == f); Assert.That(f == d);
        }
    }
}