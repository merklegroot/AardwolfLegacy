using dump_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using rabbit_lib;
using test_shared;

namespace rabbit_lib_tests
{
    [TestClass]
    public class RabbitUtilTests
    {
        private RabbitConnectionContext _context = new RabbitConnectionContext { Host = "localhost" };

        [TestMethod]
        public void Rabbit__get_queue()
        {
            var results = RabbitUtil.GetQueues(_context);
            results.Dump();
        }

        [TestMethod]
        public void Rabbit__get_consumer_count()
        {
            var results = RabbitUtil.GetConsumerCount(_context, "etherscan-queue");
            results.Dump();
        }
    }
}
