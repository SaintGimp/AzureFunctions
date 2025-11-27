using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSingleton<SaintGimp.Functions.EmailService>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
    loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information);
});

// // Remove default Application Insights logging rule which filters out logs below Warning level.
// builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
//     {
//         LoggerFilterRule? defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
//             == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
//         if (defaultRule is not null)
//         {
//             options.Rules.Remove(defaultRule);
//         }
//     });

builder.Build().Run();
