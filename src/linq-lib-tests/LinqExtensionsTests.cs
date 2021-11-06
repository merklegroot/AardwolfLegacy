using System.Linq;
using linq_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace linq_lib_tests
{
    [TestClass]
    public class LinqExtensionsTests
    {
        [TestMethod]
        public void Linq_extensions__shuffle__size_should_match_and_should_have_the_same_elements()
        {
            var original = Enumerable.Range(1, 10).ToList();
            var cloned = original.Clone();
            var shuffled = original.Shuffle();

            shuffled.Count.ShouldBe(original.Count());
            original.All(item => shuffled.Contains(item)).ShouldBe(true);
        }

        [TestMethod]
        public void Linq_extensions__clone__adding_to_the_original_should_not_affect_the_clone()
        {
            var original = Enumerable.Range(1, 10).ToList();
            var cloned = original.Clone();

            original.Add(1234);

            cloned.Count().ShouldBe(10);
        }
    }
}
