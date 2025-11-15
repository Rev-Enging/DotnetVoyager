using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.MediatR.Commands.PrepareZip;
using DotnetVoyager.BLL.MediatR.Commands.UploadAssembly;
using DotnetVoyager.BLL.MediatR.Queries.GetDecompiledCode;
using DotnetVoyager.BLL.MediatR.Queries.GetFullDecompiledCodeInZip;
using DotnetVoyager.BLL.MediatR.Queries.GetMetadata;
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
    public async Task<IActionResult> Upload([FromForm] UploadAssemblyRequestDto request, CancellationToken cancellationToken)
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

    [HttpGet("{analysisId}/status")]
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

    [HttpGet("{analysisId}/metadata")]
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

        if (result.HasError<AnalysisNotCompletedError>(out var analysisNotCompletedErrors))
        {
            return Problem(
                detail: analysisNotCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId}/structure")]
    [ProducesResponseType(typeof(StructureNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStructure(string analysisId, CancellationToken cancellationToken)
    {
        var query = new GetStructureQuery(analysisId);
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

        if (result.HasError<AnalysisNotCompletedError>(out var analysisNotCompletedErrors))
        {
            return Problem(
                detail: analysisNotCompletedErrors.First().Message,
                statusCode: StatusCodes.Status409Conflict
            );
        }

        return Problem(
            detail: result.Errors.FirstOrDefault()?.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    [HttpGet("{analysisId}/decompile/{lookupToken:int}")]
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

    [HttpPost("{analysisId}/prepare-zip")]
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

    [HttpGet("{analysisId}/download-zip")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadZip(string analysisId, CancellationToken cancellationToken)
    {
        var command = new DownloadZipCommand(analysisId);
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
