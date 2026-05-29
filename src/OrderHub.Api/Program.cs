using OrderHub.Api;
using OrderHub.Application;
using OrderHub.Application.Common.Security;
using OrderHub.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting OrderHub API");

    var builder = WebApplication.CreateBuilder(args);

    builder.ConfigureSerilog();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    builder.Services
        .AddOptions<JwtOptions>()
        .BindConfiguration(JwtOptions.SectionName)
        .ValidateOnStart();

    var app = builder.Build();

    app.UseApiMiddleware();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
