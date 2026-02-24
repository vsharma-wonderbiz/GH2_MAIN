using Application.Interface;
using Application.Services;
using Infrastructure.Implementation;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Sedding;
using Infrastructure.Persistence.Seeding;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
 
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConn")));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<BackfillSensorDataService>();




var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TagsSeeder>>();

    // Apply migrations
    context.Database.Migrate();

    // Run your seeder
    TagTypeSeeder.Seeder(context);
    TagsSeeder.Seeder(context, logger);
    AssetSeeder.Seed(context);
    MappingSeeder.Seed(context);
    await ProtocolDataSeeder.SeedAsync(context);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
