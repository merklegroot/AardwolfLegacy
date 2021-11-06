using math_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace math_lib_tests
{
    [TestClass]
    public class MathUtilTests
    {
        [TestMethod]
        public void Math_util__truncate_simple()
        {
            var result = MathUtil.Truncate(1.23m, 1);
            result.ShouldBe(1.2m);
        }

        [TestMethod]
        public void Math_util__truncate_8_places()
        {
            var result = MathUtil.Truncate(966.407560314798090568958844m, 8);
            result.ShouldBe(966.40756031m);
        }

        [TestMethod]
        public void Math_util__contstrain_to_multiples_of__simple()
        {
            var result = MathUtil.ConstrainToMultipleOf(23, 5);
            result.ShouldBe(20);
        }

        [TestMethod]
        public void Math_util__contstrain_to_multiples_of__fractional()
        {
            var result = MathUtil.ConstrainToMultipleOf(0.00020m, 0.00006m);
            result.ShouldBe(0.00018m);
        }
    }
}
