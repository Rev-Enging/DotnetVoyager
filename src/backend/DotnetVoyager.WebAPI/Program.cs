using DotnetVoyager.BLL;
using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Logging;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Workers;
using DotnetVoyager.DAL.Data;
using DotnetVoyager.DAL.Initialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure options
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(ProjectConstants.AssemblyStorageSettingsSectionName));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(ProjectConstants.WorkerSettingsSectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(ProjectConstants.CorsOptionsSectionName));

// Configure services
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IDependencyAnalyzerService, DependencyAnalyzerService>();
builder.Services.AddScoped<IMetadataReaderService, MetadataReaderService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IAssemblyValidator, AssemblyValidator>();
builder.Services.AddScoped<IAnalysisStatusService, DatabaseAnalysisStatusService>();
builder.Services.AddScoped<IStructureAnalyzerService, StructureAnalyzerService>();
builder.Services.AddScoped<ICodeDecompilationService, CodeDecompilationService>();
builder.Services.AddScoped<IFullDecompilationService, FullDecompilationService>();
builder.Services.AddSingleton<IDecompilationTaskQueue, DecompilationTaskQueue>();
builder.Services.AddSingleton<IAnalysisTaskQueue, AnalysisTaskQueue>();

builder.Services.AddDbContext<AnalysisDbContext>((serviceProvider, optionsBuilder) =>
{
    var storageOptions = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;

    var storagePath = Path.IsPathRooted(storageOptions.Path)
        ? storageOptions.Path
        : Path.Combine(builder.Environment.ContentRootPath, storageOptions.Path);

    Directory.CreateDirectory(storagePath);

    var dbPath = Path.Combine(storagePath, "analysis_state.db");
    var connectionString = $"Data Source={dbPath}";

    optionsBuilder.UseSqlite(connectionString);
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<BllAssemblyMarker>();

// Configure background services
builder.Services.AddHostedService<AnalysisWorkerService>();
builder.Services.AddHostedService<DecompilationWorkerService>();

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

MissingSettingsLogger.LogMissingSettings(app);

await DatabaseInitializer.InitializeAndRecoverTasksAsync(app);

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
