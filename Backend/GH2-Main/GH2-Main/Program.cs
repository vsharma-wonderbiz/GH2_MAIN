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
using DotNetEnv;
using Serilog;
using Domain.Services;

var builder = WebApplication.CreateBuilder(args);

Env.Load();


builder.Configuration["ConnectionStrings:DefaultConn"] = Environment.GetEnvironmentVariable("AUTH_CONN_STR");
builder.Configuration["Jwt:Key"] = Environment.GetEnvironmentVariable("JWT_KEY");
builder.Configuration["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER");
builder.Configuration["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE");


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

        // allows "tagId", "TagId", "TAGID" all work
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConn"),
        o => o.CommandTimeout(120)
    );
});

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
builder.Services.AddScoped<IExportRequestRepository, ExportRequestRepository>();
builder.Services.AddScoped<IStackEfficiencyRepository, StackEfficiencyRepository>();
builder.Services.AddSingleton<EfficiencyForecastCalculator>();


builder.Services.AddHostedService<WeeklyAvgCalculatorBackgroundService>();
builder.Services.AddHostedService<AlarmConsumer>();

builder.Services.AddScoped<PastWeeksAggregatedData>();
builder.Services.AddScoped<HistoricalEfficiencyBatchProcessor>();
builder.Services.AddScoped<IMappingRepositary, MappingRepositary>();
builder.Services.AddScoped<ITagRepositary, TagRepositary>();
builder.Services.AddScoped<IKpiResultRepository, KpiResultRepository>();
builder.Services.AddScoped<KpiCalulationService>();
builder.Services.AddScoped<KpiFormulaService>();

builder.Services.AddHostedService<KpiBackgroundService>();
builder.Services.AddHostedService<ExportBackGroundService>();

builder.Services.AddScoped<KpiHistoryService>();
builder.Services.AddScoped<KpiQueryService>();
builder.Services.AddScoped<MappingService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IStackEfficiencyService, StackEfficiencyService>();
builder.Services.AddScoped<IAlarmRepositary, AlarmRepository>();
builder.Services.AddScoped<IRecommendationRepositary, RecommendationRepositary>();
builder.Services.AddScoped<IRabbitMqServices, RabbitMqServices>();
builder.Services.AddScoped<ProtocolDataSeeder>();
builder.Services.AddCustomServices();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<IRabbitMqConnectionService, RabbitMqConnectionService>();
builder.Services.AddSingleton<SeedingGate>();
builder.Services.AddCustomAuthentication(builder.Configuration);

var modbusStateFilePath = builder.Configuration["ModbusConfig:ModbusStateFilePath"]
    ?? throw new InvalidOperationException("ModbusStateFilePath not configured in appsettings.json");

builder.Services.AddSingleton(new ModbusStateUpdater(modbusStateFilePath));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TagsSeeder>>();
    var seeder = scope.ServiceProvider.GetRequiredService<ProtocolDataSeeder>();
    // Run seeders
    TagTypeSeeder.Seeder(context);
    TagsSeeder.Seeder(context, logger);
    AssetSeeder.Seed(context);
    MappingSeeder.Seed(context);

    await seeder.SeedAsync(context);
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
