using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Options;
using Microsoft.Extensions.Options;

namespace DotnetVoyager.BLL.Logging;

public static class MissingSettingsLogger
{
    public static void LogMissingSettings(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var config = services.GetRequiredService<IConfiguration>();

        LogMissingOptionsSettings<StorageOptions>(
            logger,
            config,
            ProjectConstants.AssemblyStorageSettingsSectionName,
            services.GetRequiredService<IOptions<StorageOptions>>().Value
        );

        LogMissingOptionsSettings<WorkerOptions>(
            logger,
            config,
            ProjectConstants.WorkerSettingsSectionName,
            services.GetRequiredService<IOptions<WorkerOptions>>().Value
        );

        LogMissingOptionsSettings<CorsOptions>(
            logger,
            config,
            ProjectConstants.CorsOptionsSectionName,
            services.GetRequiredService<IOptions<CorsOptions>>().Value
        );
    }

    private static void LogMissingOptionsSettings<T>(
        ILogger logger,
        IConfiguration config,
        string sectionName,
        T optionsInstance) where T : class
    {
        logger.LogInformation("Checking {OptionsType} configuration...", typeof(T).Name);

        var section = config.GetSection(sectionName);
        var configuredKeys = section.GetChildren()
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var properties = typeof(T).GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!configuredKeys.Contains(property.Name))
            {
                var defaultValue = property.GetValue(optionsInstance);

                logger.LogWarning(
                    "The '{SettingName}' was not found in section '{SectionName}'. Using default value: {DefaultValue}",
                    property.Name,
                    sectionName,
                    defaultValue ?? "null"
                );
            }
        }
    }
}