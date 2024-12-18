﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mimir.Initializer;
using Mimir.Initializer.Initializer;
using Mimir.Initializer.Migrators;
using Mimir.Worker.Services;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(
        (hostingContext, config) =>
        {
            string configPath =
                Environment.GetEnvironmentVariable("INITIALIZER_CONFIG_FILE") ?? "appsettings.json";
            config
                .AddJsonFile(configPath, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("INITIALIZER_");
        }
    )
    .ConfigureServices(
        (hostContext, services) =>
        {
            services.Configure<Configuration>(
                hostContext.Configuration.GetSection("Configuration")
            );

            services.AddSingleton<MongoDbService>();

            services.AddSingleton(serviceProvider =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<Configuration>>().Value;
                return new MongoDbService(
                    config.MongoDbConnectionString,
                    config.PlanetType,
                    config.MongoDbCAFile
                );
            });
            
            services.AddSingleton<IItemProductCalculationService, ItemProductCalculationService>();
            
            services.AddSingleton<ExecuteManager>();
            services.AddSingleton<IExecutor, SnapshotInitializer>();
            services.AddSingleton<IExecutor, ProductMigrator>();
        }
    )
    .UseSerilog(
        (context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        }
    )
    .Build();

using var scope = host.Services.CreateScope();
var executeManager = scope.ServiceProvider.GetRequiredService<ExecuteManager>();

var stoppingToken = new CancellationTokenSource().Token;

await executeManager.ExecuteAsync(stoppingToken);
