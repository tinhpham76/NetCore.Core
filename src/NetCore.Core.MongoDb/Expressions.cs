using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using NetCore.Core.MongoDb.Utils;

namespace NetCore.Core.MongoDb
{
    public static class Expressions
    {
        public static bool IsObjectId(this ObjectId objId)
        {
            return objId != ObjectId.Empty;
        }
        public static bool IsObjectId(this string text)
        {
            if (!Validate.IsRequired(text))
                return false;
                
            if (!Validate.IsValidLength(text, maxLength: 32))
                return false;

            return true;
        }

        public static IServiceCollection RegisterMongoDb(
            this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<Config>(configuration.GetSection(Config.ConfigName));

            services.AddSingleton<IConnection>(provider =>
            {
                var config = provider.GetService<IOptions<Config>>().Value;
                return new Connection(config);
            });
            
            services.AddSingleton<IMongoDatabase>(provider =>
            {
                var connection = provider.GetService<IConnection>();
                return connection.Database;
            });
            
            services.AddSingleton<SequenceRepository>();

            return services;
        }
    }   
}