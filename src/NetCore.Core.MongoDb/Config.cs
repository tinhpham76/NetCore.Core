namespace NetCore.Core.MongoDb
{
    public class Config
    {
        public const string ConfigName = "MongoDb";

        public string Username { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Database { get; set; }
    }
}