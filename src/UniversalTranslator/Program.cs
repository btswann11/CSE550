using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UniversalTranslator;



var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
       .AddSignalR();

builder.Services
       .AddSingleton<StorageService>()
       .AddCors(options =>
       {
           options.AddDefaultPolicy(policy =>
           {
               policy.AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod();
           });
       })
       .AddSingleton<TableClient>(sp => new(
                Environment.GetEnvironmentVariable(Constants.ConnectionStringKey)
                    ?? throw new InvalidOperationException($"{Constants.ConnectionStringKey} environment variable is not set.")
              , Environment.GetEnvironmentVariable(Constants.ChatsTableNameKey)
                    ?? throw new InvalidOperationException($"{Constants.ChatsTableNameKey} environment variable is not set."))
       )
       .AddHttpClient<TranslationService>(client =>
       {
           var baseUri = Environment.GetEnvironmentVariable(Constants.TranslationServiceBaseUrlKey)
                             ?? throw new InvalidOperationException($"{Constants.TranslationServiceBaseUrlKey} environment variable is not set.");
           client.BaseAddress = new Uri(baseUri);
           client.DefaultRequestHeaders
                    .Add("Ocp-Apim-Subscription-Key"
                        , Environment.GetEnvironmentVariable(Constants.TranslationServiceApiKey)
                        ?? throw new InvalidOperationException($"{Constants.TranslationServiceApiKey} environment variable is not set."));

           client.DefaultRequestHeaders
                     .Add("Ocp-Apim-Subscription-Region", Environment.GetEnvironmentVariable(Constants.TranslationServiceLocationKey)
                     ?? throw new InvalidOperationException($"{Constants.TranslationServiceLocationKey} environment variable is not set."));
       });       

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
