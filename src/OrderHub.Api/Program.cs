using Microsoft.EntityFrameworkCore;
using OrderHub.Api;
using OrderHub.Application;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Persistence;
using OrderHub.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureSerilog();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseApiMiddleware();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();
    await dbContext.Database.MigrateAsync();
    DataSeeder.Seed(dbContext);
}

app.Run();

public partial class Program;
