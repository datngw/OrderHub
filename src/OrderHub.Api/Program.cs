using OrderHub.Api;
using OrderHub.Application;
using OrderHub.Application.Features.Auth;
using OrderHub.Infrastructure;

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

public partial class Program;
