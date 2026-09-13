using DotNetEnv;
using Serilog;
using Sofra.Worker.Messaging;
using Sofra.Worker.Options;

try
{
    Env.TraversePath().Load();
}
catch (FileNotFoundException)
{
    // U Docker kontejneru .env fajl ne postoji — varijable su već postavljene kroz docker-compose.
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/sofra-worker-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<SmtpOptions>()
    .Bind(builder.Configuration.GetSection(SmtpOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<RabbitMqConnectionService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConnectionService>());

var host = builder.Build();
host.Run();
