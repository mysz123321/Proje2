using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Staj2.Agent;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Staj2 Agent Service";
    })
    .ConfigureServices((ctx, services) =>
    {
        services.AddHttpClient("backend", http =>
        {
            var baseUrl = ctx.Configuration["Agent:BackendBaseUrl"];
            if (!string.IsNullOrWhiteSpace(baseUrl))
                http.BaseAddress = new Uri(baseUrl);
            http.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddSingleton<IMetricsCollector, DefaultMetricsCollector>();
        services.AddHostedService<TelemetryWorker>();
    })
    .Build();

await host.RunAsync();
