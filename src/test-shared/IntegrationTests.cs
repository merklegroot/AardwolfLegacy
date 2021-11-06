using mongo_lib;

namespace test_shared
{
    public static class IntegrationTests
    {
        public static MongoConnectionString ConnectionString
        {
            get { return new MongoConnectionString().Host("localhost"); }
        }

        public static MongoDatabaseContext DatabaseContext = new MongoDatabaseContext(ConnectionString, DatabaseName);

        public const string DatabaseName = "integration-testing";
    }
}
