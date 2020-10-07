using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetCore.Core.MongoDb.Test.Repositories;

namespace NetCore.Core.MongoDb.Test
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "../../../"))
                    .AddJsonFile("appsetting.json", optional: true);

            var configuration = builder.Build();

            var services = new ServiceCollection();

            services.RegisterMongoDb(configuration);

            services.AddSingleton<BookRepository>();

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                var config = serviceProvider.GetService<IOptions<Config>>().Value;

                var rpBook = serviceProvider.GetService<BookRepository>();

                var obj = rpBook.GetByIdAsync(1).Result;

            }
            catch (System.Exception ex)
            {
                var message = ex.Message;

            }
        }
    }
}