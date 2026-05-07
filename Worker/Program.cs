using Worker.Application.Services;
using Worker.Infrastructure.Options;

namespace Worker;

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
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));
        services.AddControllers().AddXmlSerializerFormatters();
        services.AddSingleton<BruteForceCrackService>();
        services.AddHttpClient();
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.MapControllers();
    }
}
