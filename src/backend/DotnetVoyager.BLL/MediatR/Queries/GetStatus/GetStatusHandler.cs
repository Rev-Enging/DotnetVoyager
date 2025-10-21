using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStatus;

public class GetStatusHandler : IRequestHandler<GetStatusQuery, Result<AnalysisStatusDto>>
{
    private readonly IAnalysisStatusService _statusService;

    public GetStatusHandler(IAnalysisStatusService statusService)
    {
        _statusService = statusService;
    }

    public async Task<Result<AnalysisStatusDto>> Handle(GetStatusQuery request, CancellationToken cancellationToken)
    {
        var statusDto = await _statusService.GetStatusAsync(request.AnalysisId, cancellationToken);

        if (statusDto == null)
        {
            return Result.Fail(new NotFoundError($"Analysis status for ID '{request.AnalysisId}' not found."));
        }

        return Result.Ok(statusDto);
    }
}