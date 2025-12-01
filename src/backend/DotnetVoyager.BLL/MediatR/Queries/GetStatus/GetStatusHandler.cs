using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStatus;

public record GetStatusQuery(string AnalysisId) : IRequest<Result<AnalysisStatusDto>>;

public class GetStatusHandler : IRequestHandler<GetStatusQuery, Result<AnalysisStatusDto>>
{
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<GetStatusHandler> _logger;

    public GetStatusHandler(
        IAnalysisStatusService statusService, 
        ILogger<GetStatusHandler> logger)
    {
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Result<AnalysisStatusDto>> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var statusDto = await _statusService.GetStatusAsync(request.AnalysisId, cancellationToken);
            return Result.Ok(statusDto);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Requested status for analysis not found: {AnalysisId}",
                ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}