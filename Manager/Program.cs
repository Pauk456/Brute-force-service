using Manager.Application.Services;
using System.Threading.Channels;
using Manager.Application.Models;
using Manager.Infrastructure.BackgroundServices;
using Manager.Infrastructure.Options;
using Manager.Infrastructure.Persistence;
using Manager.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace Manager;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigurePipeline(app);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ManagerOptions>(configuration.GetSection(ManagerOptions.SectionName));
        services.AddControllers().AddXmlSerializerFormatters();
        services.AddSingleton<InMemoryCrackRequestRepository>();
        services.AddSingleton<ManagerStatePersistenceService>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ManagerOptions>>().Value;
            var queueCapacity = Math.Max(1, options.MaxQueuedCrackRequests);

            return Channel.CreateBounded<CrackJobQueueItem>(new BoundedChannelOptions(queueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        });
        services.AddSingleton<HashCrackService>();
        services.AddHostedService<CrackTaskQueueBackgroundService>();
        services.AddHostedService<RequestTimeoutBackgroundService>();
        services.AddHttpClient();
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
    }
}
