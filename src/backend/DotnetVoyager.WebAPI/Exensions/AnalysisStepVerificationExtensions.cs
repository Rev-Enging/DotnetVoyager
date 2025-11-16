using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Services.AnalysisSteps;
using System.Text;

namespace DotnetVoyager.WebAPI.Exensions;

public static class AnalysisStepVerificationExtensions
{
    /// <summary>
    /// Verifies that all analysis steps defined in AnalysisStepNames.AllSteps
    /// have a corresponding implementation registered in the DI container.
    /// </summary>
    /// <param name="app">The host application.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if one or more IAnalysisStep implementations are not registered.
    /// </exception>
    public static void VerifyAnalysisStepRegistrations(this IHost app)
    {
        // We need a service scope to resolve scoped services
        using (var scope = app.Services.CreateScope())
        {
            var serviceProvider = scope.ServiceProvider;

            // 1. Get all registered IAnalysisStep implementations from the container
            var registeredSteps = serviceProvider.GetServices<IAnalysisStep>();

            // 2. Get a set of their names
            var registeredStepNames = registeredSteps.Select(s => s.StepName).ToHashSet();

            // 3. Get all step names that are *expected* to exist
            var expectedStepNames = AnalysisStepNames.AllSteps;

            // 4. Find the difference
            var missingSteps = expectedStepNames
                .Where(expectedName => !registeredStepNames.Contains(expectedName))
                .ToList();

            // 5. If any are missing, build a descriptive error message and throw
            if (missingSteps.Any())
            {
                var errorBuilder = new StringBuilder();
                errorBuilder.AppendLine("Missing IAnalysisStep DI Registrations!");
                errorBuilder.AppendLine("The following steps are defined in 'AnalysisStepNames.AllSteps' but were not found in the service provider:");

                foreach (var missingStep in missingSteps)
                {
                    // This error message provides a helpful hint on how to fix it
                    errorBuilder.AppendLine($"  - \"{missingStep}\" (Did you forget to register '...{missingStep}AnalysisStep' in Program.cs?)");
                }

                throw new InvalidOperationException(errorBuilder.ToString());
            }
        }
    }
}
