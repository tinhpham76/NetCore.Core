using MongoDB.Driver;

namespace NetCore.Core.MongoDb
{
    public interface IConnection
    {
        string ConnectionString { get; }
        IMongoClient Client { get; }
        MongoUrl Url { get; }
        IMongoDatabase Database { get; }
    }

    public class Connection : IConnection
    {
        public Connection(Config config)
        {
            this.ConnectionString = this.getConnectionString(config);
            this.Client = new MongoClient(this.ConnectionString);
            this.Url = new MongoUrl(this.ConnectionString);
            this.Database = this.Client.GetDatabase(this.Url.DatabaseName);
        }
        
        public string ConnectionString { get; }
        public IMongoClient Client { get; }
        public MongoUrl Url { get; }
        public IMongoDatabase Database { get; }
        
        private string getConnectionString(Config config)
        {
            var connection = "mongodb://";

            if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
                connection += config.Username + ":" + config.Password + "@";

            connection += config.Host + ":" + config.Port + "/" + config.Database;
            
            return connection;
        }
    }
}