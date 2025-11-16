using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.MediatR.Commands.PrepareZip;
using DotnetVoyager.BLL.MediatR.Commands.UploadAssembly;
using DotnetVoyager.BLL.MediatR.Queries.GetAssemblyDependencies;
using DotnetVoyager.BLL.MediatR.Queries.GetDecompiledCode;
using DotnetVoyager.BLL.MediatR.Queries.GetFullDecompiledCodeInZip;
using DotnetVoyager.BLL.MediatR.Queries.GetInheritanceGraph;
using DotnetVoyager.BLL.MediatR.Queries.GetMetadata;
using DotnetVoyager.BLL.MediatR.Queries.GetStatistic;
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

    public AnalysisController(
        IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadAssemblyResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProjectConstants.MaxAssemblySizeInBytes)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadAssemblyRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UploadAssemblyCommand(new UploadAssemblyDto
        {
            File = request.File.ToFileDto()
        });

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Accepted(result.Value);
        }

        if (result.HasError<ValidationError>())
        {
            return HandleValidationError(result.GetError<ValidationError>()!);
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/status")]
    [ProducesResponseType(typeof(AnalysisStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatus(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetStatusQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<NotFoundError>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/metadata")]
    [ProducesResponseType(typeof(AssemblyMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetMetadata(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetMetadataQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<AnalysisNotFound>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (result.HasError<StepNotCompletedError>(out var notCompletedErrors))
        {
            return Problem(
                detail: notCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (result.HasError<StepFailedError>(out var failedErrors))
        {
            return Problem(
                detail: failedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/statistics")]
    [ProducesResponseType(typeof(AssemblyStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetStatistics(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetStatisticQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<AnalysisNotFound>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (result.HasError<StepNotCompletedError>(out var notCompletedErrors))
        {
            return Problem(
                detail: notCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (result.HasError<StepFailedError>(out var failedErrors))
        {
            return Problem(
                detail: failedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/dependencies")]
    [ProducesResponseType(typeof(AssemblyDependenciesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAssemblyDependencies(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetAssemblyDependenciesQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<AnalysisNotFound>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (result.HasError<StepNotCompletedError>(out var notCompletedErrors))
        {
            return Problem(
                detail: notCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (result.HasError<StepFailedError>(out var failedErrors))
        {
            return Problem(
                detail: failedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/assembly-tree")]
    [ProducesResponseType(typeof(AssemblyTreeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAssemblyTree(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetAssemblyTreeQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<AnalysisNotFound>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (result.HasError<StepNotCompletedError>(out var notCompletedErrors))
        {
            return Problem(
                detail: notCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (result.HasError<StepFailedError>(out var failedErrors))
        {
            return Problem(
                detail: failedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId}/inheritance-graph")]
    [ProducesResponseType(typeof(InheritanceGraphDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetInheritanceGraph(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetInheritanceGraphQuery(analysisId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<AnalysisNotFound>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        if (result.HasError<StepNotCompletedError>(out var notCompletedErrors))
        {
            return Problem(
                detail: notCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        if (result.HasError<StepFailedError>(out var failedErrors))
        {
            return Problem(
                detail: failedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/decompile/{lookupToken:int}")]
    [ProducesResponseType(typeof(DecompiledCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDecompiledCode(string analysisId, int lookupToken, CancellationToken cancellationToken)
    {
        var query = new GetDecompiledCodeQuery(analysisId, lookupToken);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.HasError<NotFoundError>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpPost("{analysisId:guid}/prepare-zip")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PrepareZip(string analysisId, CancellationToken cancellationToken)
    {
        var command = new PrepareZipCommand(analysisId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Accepted();
        }

        if (result.HasError<NotFoundError>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId:guid}/download-zip")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadZip(string analysisId, CancellationToken cancellationToken)
    {
        var command = new DownloadZipQuery(analysisId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            var dto = result.Value;

            return new FileStreamResult(dto.FileStream, dto.ContentType)
            {
                FileDownloadName = dto.FileDownloadName
            };
        }

        if (result.HasError<NotFoundError>(out var notFoundErrors))
        {
            return Problem(
                detail: notFoundErrors.First().Message,
                statusCode: StatusCodes.Status404NotFound
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
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
