using FluentResults;

namespace DotnetVoyager.WebAPI.Exensions;

public static class FluentResultsExtensions
{
    /// <summary>
    /// Gets the first error of a specific type <T> from the result.
    /// </summary>
    /// <typeparam name="T">The type of error to find (must implement IError).</typeparam>
    public static T? GetError<T>(this IResultBase result) where T : class, IError
    {
        return result.Errors.OfType<T>().FirstOrDefault();
    }
}
