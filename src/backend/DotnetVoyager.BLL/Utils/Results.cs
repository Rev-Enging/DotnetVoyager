using DotnetVoyager.BLL.Errors;
using FluentResults;

namespace DotnetVoyager.BLL.Utils;

public static class Results
{
    public static Result Ok() => Result.Ok();
    public static Result<T> Ok<T>(T value) => Result.Ok(value);

    public static Result Fail(string message) => Result.Fail(message);
    public static Result<T> Fail<T>(string message) => Result.Fail<T>(message);

    public static Result NotFound(string message) => Result.Fail(new NotFoundError(message));
    public static Result<T> NotFound<T>(string message) => Result.Fail<T>(new NotFoundError(message));
}