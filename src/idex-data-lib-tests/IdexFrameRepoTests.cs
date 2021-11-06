using idex_data_lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using config_client_lib;

namespace idex_data_lib_tests
{
    [TestClass]
    public class IdexFrameRepoTests
    {
        private Mock<IConfigClient> _configMock;
        private IdexFrameRepo _repo;

        [TestInitialize]
        public void Setup()
        {
            var realConfigClient = new ConfigClient();
            _configMock = new Mock<IConfigClient>();
            _configMock.Setup(mock => mock.GetConnectionString()).Returns(() => realConfigClient.GetConnectionString());
            _repo = new IdexFrameRepo(_configMock.Object);
        }

        [TestMethod]
        public void Idex_frame_repo__process()
        {
            _repo.Process();
        }

        [TestMethod]
        public void Idex_frame_repo__truncate_old_data()
        {
            const string ConnectionString = "mongodb://tradeAdmin:Trade5273!@192.168.1.118:27017";
            _configMock.Setup(mock => mock.GetConnectionString()).Returns(() => ConnectionString);

            _repo.TruncateOldData();
        }
    }
}
