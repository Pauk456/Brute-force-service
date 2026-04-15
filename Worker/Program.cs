using Worker.Application.Services;
using Worker.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));
builder.Services.AddControllers().AddXmlSerializerFormatters();
builder.Services.AddSingleton<BruteForceCrackService>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapControllers();

app.Run();
