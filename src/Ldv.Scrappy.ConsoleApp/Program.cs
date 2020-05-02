using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Ldv.Scrappy.Bll;
using Ldv.Scrappy.Dal.Postgres;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
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

            // application insights for console applications
            // https://docs.microsoft.com/en-us/azure/azure-monitor/app/console
            TelemetryConfiguration aiConfiguration = TelemetryConfiguration.CreateDefault();
            aiConfiguration.InstrumentationKey = configuration["ApplicationInsights:InstrumentationKey"];
            aiConfiguration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
            var telemetryClient = new TelemetryClient(aiConfiguration);
            var dependencyTracking = InitializeDependencyTracking(aiConfiguration);
            // -- end AI
            
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
                
                // AI
                dependencyTracking.Dispose();
                // before exit, flush the remaining data
                telemetryClient.Flush();
                // flush is not blocking so wait a bit
                Task.Delay(5000).Wait();
                // -- END AI
            }
        }
        
        static DependencyTrackingTelemetryModule InitializeDependencyTracking(TelemetryConfiguration configuration)
        {
            var module = new DependencyTrackingTelemetryModule();

            // prevent Correlation Id to be sent to certain endpoints. You may add other domains as needed.
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.chinacloudapi.cn");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.cloudapi.de");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.usgovcloudapi.net");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("localhost");
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("127.0.0.1");

            // enable known dependency tracking, note that in future versions, we will extend this list. 
            // please check default settings in https://github.com/microsoft/ApplicationInsights-dotnet-server/blob/develop/WEB/Src/DependencyCollector/DependencyCollector/ApplicationInsights.config.install.xdt

            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.ServiceBus");
            module.IncludeDiagnosticSourceActivities.Add("Microsoft.Azure.EventHubs");

            // initialize the module
            module.Initialize(configuration);

            return module;
        }
    }
}