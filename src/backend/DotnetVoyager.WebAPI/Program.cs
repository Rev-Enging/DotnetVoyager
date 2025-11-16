using DotnetVoyager.BLL;
using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Factories;
using DotnetVoyager.BLL.Logging;
using DotnetVoyager.BLL.Options;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Services.AnalysisSteps;
using DotnetVoyager.BLL.Services.Analyzers;
using DotnetVoyager.BLL.Validators;
using DotnetVoyager.BLL.Workers;
using DotnetVoyager.DAL.Data;
using DotnetVoyager.DAL.Initialization;
using DotnetVoyager.WebAPI.Exensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ==================== OPTIONS ====================
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(ProjectConstants.AssemblyStorageSettingsSectionName));
builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(ProjectConstants.WorkerSettingsSectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(ProjectConstants.CorsOptionsSectionName));

// ==================== CORE SERVICES ====================
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IAnalysisStatusService, DatabaseAnalysisStatusService>();
builder.Services.AddScoped<IAnalysisStepService, AnalysisStepService>();
builder.Services.AddScoped<IAnalysisOrchestrator, AnalysisOrchestrator>();
builder.Services.AddScoped<IAssemblyValidator, AssemblyValidator>();

// ==================== ANALYSIS SERVICES ====================
builder.Services.AddScoped<IAssemblyReferenceAnalyzer, AssemblyReferenceAnalyzer>();
builder.Services.AddScoped<IMetadataReaderService, MetadataReaderService>();
builder.Services.AddScoped<IStatisticsAnalyzer, StatisticsAnalyzer>();
builder.Services.AddScoped<IAssemblyTreeAnalyzer, AssemblyTreeAnalyzer>();
builder.Services.AddScoped<IInheritanceGraphBuilderService, InheritanceGraphBuilderService>();
builder.Services.AddScoped<ICodeDecompilationService, CodeDecompilationService>();
builder.Services.AddScoped<IFullDecompilationService, FullDecompilationService>();

// ==================== ANALYSIS STEPS ====================
builder.Services.AddScoped<IAnalysisStep, MetadataAnalysisStep>();
builder.Services.AddScoped<IAnalysisStep, StatisticsAnalysisStep>();
builder.Services.AddScoped<IAnalysisStep, AssemblyTreeAnalysisStep>();
builder.Services.AddScoped<IAnalysisStep, InheritanceGraphAnalysisStep>();
builder.Services.AddScoped<IAnalysisStep, ZipGenerationStep>();
builder.Services.AddScoped<IAnalysisStep, AssemblyDependencyAnalysisStep>();

// ==================== VALIDATION ====================
builder.Services.AddValidatorsFromAssemblyContaining<BllAssemblyMarker>();

// ==================== FACTORIES & SINGLETONS ====================
builder.Services.AddSingleton<IDecompilerFactory, DecompilerFactory>();
builder.Services.AddSingleton<IAnalysisTaskQueue, AnalysisTaskQueue>();

// ==================== BACKGROUND WORKERS ====================
builder.Services.AddHostedService<AnalysisWorkerService>();

// ==================== MEDIATR ====================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(BllAssemblyMarker).Assembly);
});

// ==================== LOGGING ====================
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "HH:mm:ss ";
    options.UseUtcTimestamp = false;
});

// ==================== SWAGGER ====================
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

// ==================== DATABASE ====================
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

// ==================== CORS ====================
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

// ==================== BUILD APP ====================
var app = builder.Build();

app.VerifyAnalysisStepRegistrations();

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
