using Worker.Application.Services;
using Worker.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigurePipeline(app);

app.Run();

return;

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));
    services.AddControllers().AddXmlSerializerFormatters();
    services.AddSingleton<BruteForceCrackService>();
    services.AddHttpClient();
}

static void ConfigurePipeline(WebApplication app)
{
    app.MapControllers();
}
