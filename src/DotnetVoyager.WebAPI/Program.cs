using DotnetVoyager.BLL;
using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Workers;
using FluentValidation;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure options
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(ProjectConstants.StorageSettingsSectionName));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(ProjectConstants.WorkerSettingsSectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(ProjectConstants.CorsOptionsSectionName));

// Configure services
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IDependencyAnalyzerService, DependencyAnalyzerService>();
builder.Services.AddScoped<IMetadataReaderService, MetadataReaderService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IAssemblyValidator, AssemblyValidator>();
builder.Services.AddScoped<IAnalysisStatusService, AnalysisStatusService>();
builder.Services.AddScoped<IStructureAnalyzerService, StructureAnalyzerService>();
builder.Services.AddScoped<ICodeDecompilationService, CodeDecompilationService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<BllAssemblyMarker>();

// Configure background services
builder.Services.AddHostedService<QueuedHostedService>();

// Configure logging
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.UseUtcTimestamp = false;
});

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(BllAssemblyMarker).Assembly);
});

// Configure swagger
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}


// Configure CORS
var corsOptions = builder.Configuration.GetSection(ProjectConstants.CorsOptionsSectionName).Get<CorsOptions>();

if (corsOptions == null || corsOptions.AllowedOrigins == null || !corsOptions.AllowedOrigins.Any())
{
    throw new InvalidOperationException("CORS configuration is missing or invalid.");
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Build the app
var app = builder.Build();

ValidateAndLogSettings(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static void ValidateAndLogSettings(WebApplication webApp)
{
    using var scope = webApp.Services.CreateScope();
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILogger<Program>>();
    var config = services.GetRequiredService<IConfiguration>();

    // StorageOptions
    var storageOptions = services.GetRequiredService<IOptions<StorageOptions>>().Value;
    var storageSection = config.GetSection(ProjectConstants.StorageSettingsSectionName);

    logger.LogInformation($"Validating {nameof(StorageOptions)} settings...");
    LogIfSettingIsMissing(logger, storageSection, nameof(StorageOptions.Path), storageOptions.Path);
    LogIfSettingIsMissing(logger, storageSection, nameof(StorageOptions.FileLifetimeMinutes), storageOptions.FileLifetimeMinutes);

    // WorkerOptions
    var workerOptions = services.GetRequiredService<IOptions<WorkerOptions>>().Value;
    var workerSection = config.GetSection(ProjectConstants.WorkerSettingsSectionName);

    logger.LogInformation($"Validating {nameof(WorkerOptions)} settings...");
    LogIfSettingIsMissing(logger, workerSection, nameof(WorkerOptions.AnalysisConcurrentWorkers), workerOptions.AnalysisConcurrentWorkers);
    LogIfSettingIsMissing(logger, workerSection, nameof(WorkerOptions.AnalysisTimeoutMinutes), workerOptions.AnalysisTimeoutMinutes);
}

static void LogIfSettingIsMissing(ILogger logger, IConfigurationSection section, string key, object defaultValue)
{
    if (!section.GetChildren().Any(x => x.Key == key))
    {
        logger.LogWarning(
            "The '{SettingName}' was not found in section '{SectionName}'. Using default value: {DefaultValue}",
            key,
            section.Key,
            defaultValue
        );
    }
}
