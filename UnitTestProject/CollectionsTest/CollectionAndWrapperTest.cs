using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TX.Collections;

namespace UnitTestProject.CollectionsTest
{
    class TestObservableCollection<T> : 
        ObservableCollection<T>, 
        ISyncableEnumerable<T> { }

    [TestClass]
    public class CollectionAndWrapperTest
    {
        [TestMethod]
        public void TestBaseOperations()
        {
            var testSrc1 = new TestObservableCollection<int>();
            var testDest1 = new Collection<int>();
            var testSrc2 = new TestObservableCollection<int>();
            var testDest2 = new Collection<int>();
            Func<int, int> caster = src => src;
            Func<int, int, bool> comparer = (src, dest) => dest == caster(src);

            var bind1 = new CollectionBind<int, int>(
                testSrc1, testDest1, caster, comparer);
            var bind2 = new CollectionBind<int, int>(
                testSrc2, testDest2, caster, comparer);
            bind1.IsEnabled = true;
            bind2.IsEnabled = true;

            for (int batch = 0; batch < 50; ++batch)
            {
                bind1.IsEnabled = bind2.IsEnabled = (batch % 4 != 0);

                for (int i = 0; i < 100; ++i)
                    TestAdd(testSrc1, testSrc2);

                for (int i = 0; i < 50; ++i)
                    TestRemove(testSrc1, testSrc2);

                TestClear(testSrc1, testSrc2);

                bind1.IsEnabled = bind2.IsEnabled = true;
                Assert.IsTrue(CompareCollection(testSrc1, testDest1));
                Assert.IsTrue(CompareCollection(testSrc2, testDest2));
                Assert.IsTrue(CompareCollection(testDest1, testDest2));
                Assert.AreEqual(testSrc1.Count, testDest1.Count);
                Assert.AreEqual(testSrc2.Count, testDest2.Count);
                Assert.AreEqual(testDest1.Count, testDest2.Count);
            }
        }

        private bool CompareCollection(ICollection<int> col1, ICollection<int> col2)
            => !(col1.Except(col2).Any() || col2.Except(col1).Any());

        private void TestAdd(params ICollection<int>[] srcs)
        {
            int newVal = randomEngine.Next() % 1024;
            foreach (var src in srcs) src.Add(newVal);
        }

        private void TestRemove(params ICollection<int>[] srcs)
        {
            int removedVal = srcs[0].FirstOrDefault();
            foreach (var src in srcs) src.Remove(removedVal);
        }

        private void TestClear(params ICollection<int>[] srcs)
        {
            foreach (var src in srcs) src.Clear();
        }

        Random randomEngine = new Random();
    }
}
