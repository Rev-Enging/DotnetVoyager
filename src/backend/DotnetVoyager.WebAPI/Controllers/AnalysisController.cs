using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.MediatR.Commands.UploadAssembly;
using DotnetVoyager.BLL.MediatR.Queries.GetDecompiledCode;
using DotnetVoyager.BLL.MediatR.Queries.GetStatus;
using DotnetVoyager.BLL.MediatR.Queries.GetStructure;
using DotnetVoyager.WebAPI.Dtos;
using DotnetVoyager.WebAPI.Exensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DotnetVoyager.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        IMediator mediator,
        ILogger<AnalysisController> logger)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadAssemblyResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProjectConstants.MaxAssemblySizeInBytes)]
    public async Task<IActionResult> Upload([FromForm] UploadAssemblyRequestDto request, CancellationToken cancellationToken)
    {
        var bllDto = request.ToBllDto();

        var result = await _mediator.Send(new UploadAssemblyCommand(bllDto), cancellationToken);

        if (result.IsSuccess)
        {
            return Accepted(result.Value);
        }

        if (result.HasError<ValidationError>())
        {
            var validationError = result.GetError<ValidationError>()!;
            return HandleValidationError(validationError);
        }

        _logger.LogError("Unexpected error during assembly upload: {Errors}", result.Errors.Select(e => e.Message));
        return Problem(
            detail: "An unexpected error occurred during upload.",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId}/status")]
    [ProducesResponseType(typeof(AnalysisStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetStatusQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        
        if (result.HasError<NotFoundError>())
        {
            return Problem(
                detail: result.GetError<NotFoundError>()!.Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        _logger.LogWarning("Failed to get status for {AnalysisId}: {Errors}", analysisId, result.Errors.Select(e => e.Message));
        return Problem(
            detail: "An unexpected error occured during status query",
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    [HttpGet("{analysisId}/structure")]
    [ProducesResponseType(typeof(StructureNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStructure(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetStructureQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<NotFoundError>())
        {
            return Problem(
                detail: result.GetError<NotFoundError>()!.Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (result.HasError<AnalysisNotCompletedError>())
        {
            return Problem(
                detail: result.GetError<AnalysisNotCompletedError>()!.Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        _logger.LogWarning("Bad request for GetStructure {AnalysisId}: {Errors}", analysisId, result.Errors.Select(e => e.Message));
        return Problem(
            detail: "Unexpected error occured during structure query",
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    [HttpGet("{analysisId}/decompile/{lookupToken:int}")]
    [ProducesResponseType(typeof(DecompiledCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDecompiledCode(string analysisId, int lookupToken, CancellationToken cancellationToken)
    {
        var query = new GetDecompiledCodeQuery(analysisId, lookupToken);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<NotFoundError>())
        {
            return Problem(
                detail: result.GetError<NotFoundError>()!.Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        return Problem(
            detail: "Unexpected error occured during decompile query",
            statusCode: StatusCodes.Status400BadRequest
        );
    }

    private IActionResult HandleValidationError(ValidationError validationError)
    {
        var errors = validationError.ValidationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return ValidationProblem(new ValidationProblemDetails(errors));
    }
}
