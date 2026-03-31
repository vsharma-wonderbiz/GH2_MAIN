using System.Text.Json.Serialization;
using Application.Interface;
using Application.Services;
using Infrastructure.BackgroundServices;
using Infrastructure.Implementation;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Sedding;
using Infrastructure.Persistence.Seeding;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using GH2_Main.Extension;
using Serilog;
 
var builder = WebApplication.CreateBuilder(args);

//Log.Logger = new LoggerConfiguration()
//    .MinimumLevel.Debug()
//    .WriteTo.Console()
//    .CreateLogger();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});
// Add services to the container.


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {

        // allows "tagId", "TagId", "TAGID" — all work
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConn")
      
     ));

builder.Configuration.AddJsonFile("kpiDependencies.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .WithOrigins("http://localhost:5173") // ? exact frontend URL
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // ? now works because origin is specific
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<BackfillSensorDataService>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddHostedService<WeeklyAvgCalculatorBackgroundService>();
builder.Services.AddHostedService<AlarmConsumer>();
builder.Services.AddScoped<PastWeeksAggregatedData>();
builder.Services.AddScoped<IMappingRepositary, MappingRepositary>();
builder.Services.AddScoped<ITagRepositary, TagRepositary>();
builder.Services.AddScoped<IKpiResultRepository, KpiResultRepository>();
builder.Services.AddScoped<KpiCalulationService>();
builder.Services.AddScoped<KpiFormulaService>();
builder.Services.AddHostedService<KpiBackgroundService>();
builder.Services.AddScoped<KpiHistoryService>();
builder.Services.AddScoped<KpiQueryService>();
builder.Services.AddScoped<MappingService>();
builder.Services.AddScoped<IAlarmRepositary, AlarmRepository>();
builder.Services.AddCustomServices();

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);




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



app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
