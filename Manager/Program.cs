using Manager.Application.Services;
using Manager.Infrastructure.BackgroundServices;
using Manager.Infrastructure.Options;
using Manager.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ManagerOptions>(builder.Configuration.GetSection(ManagerOptions.SectionName));
builder.Services.AddControllers().AddXmlSerializerFormatters();
builder.Services.AddSingleton<InMemoryCrackRequestRepository>();
builder.Services.AddSingleton<HashCrackService>();
builder.Services.AddHostedService<RequestTimeoutBackgroundService>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapControllers();

app.Run();
