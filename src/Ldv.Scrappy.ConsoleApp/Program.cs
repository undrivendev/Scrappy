using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Ldv.Scrappy.Bll;
using Ldv.Scrappy.Dal.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using SimpleInjector;

namespace Ldv.Scrappy.ConsoleApp
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting application...");

                var container = new Container();
                container.Options.DefaultLifestyle = Lifestyle.Singleton;
                container.Options.ResolveUnregisteredConcreteTypes = false;

                // basic 
                container.Register<Bll.ILogger>(() => new SerilogILoggerWrapper(Log.Logger));
                container.Register(() => new HttpClient());

                // mapper
                var config = new MapperConfiguration((cfg) =>
                {
                    Ldv.Scrappy.Dal.Postgres.AutoMapperConfiguration.Configure(cfg);
                });
                config.AssertConfigurationIsValid();
                var mapper = new AutoMapperWrapper(config.CreateMapper());
                container.Register<Bll.IMapper>(() => mapper);

                // services
                var repoParameters = new PsqlRepositoryParameters()
                {
                    ConnectionString = configuration["PsqlRepositoryConnectionString"],
                };
                container.Register(() => repoParameters);

                var psqlRepo = new PsqlRepository(repoParameters, mapper);
                container.Register(() => psqlRepo);
                container.Register<IRepository>(() => psqlRepo);
                var rules = configuration.GetSection("Rules").Get<Rule[]>();
                container.Register(() => new ScrappyServiceParameters()
                {
                    Rules = rules,
                });
                
                container.Register(() => new SendGridNotifierParameters()
                {
                    Recipients = configuration["SendGridNotifierRecipients"].Split(";").ToList(), 
                    ApiKey = configuration["SendGridNotifierApiKey"]
                });
                container.Register<INotifier, SendGridNotifier>();
                container.Register<ScrappyService>();

                container.Verify();
                
                // ensure schema initialized
                await psqlRepo.EnsureInitialized();
                var service = container.GetInstance<ScrappyService>();
                await service.DownloadAndNotify();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}