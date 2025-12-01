using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStatistic;

public record GetStatisticQuery(string AnalysisId) : IRequest<Result<AssemblyStatisticsDto>>;

public class GetStatisticHandler : BaseAnalysisResultHandler<GetStatisticQuery, AssemblyStatisticsDto>
{
    public GetStatisticHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetStatisticHandler> logger)
        : base(storageService, statusService, logger)
    {
    }

    public override async Task<Result<AssemblyStatisticsDto>> Handle(
        GetStatisticQuery request,
        CancellationToken cancellationToken)
    {
        return await ProcessAnalysisResultAsync(
            request.AnalysisId,
            AnalysisStepNames.Statistics,
            ProjectConstants.AnalysisStatisticsFileName,
            cancellationToken);
    }
}

/*public class GetStatisticHandler(IMediator mediator) : IRequestHandler<GetStatisticQuery, Result<AssemblyStatisticsDto>>
{
    private readonly IMediator _mediator = mediator;

    public Task<Result<AssemblyStatisticsDto>> Handle(
        GetStatisticQuery request,
        CancellationToken cancellationToken)
    {
        return _mediator.Send(
            new GetAnalysisResultQuery<AssemblyStatisticsDto>(
                request.AnalysisId,
                AnalysisStepNames.Statistics,
                ProjectConstants.AnalysisStatisticsFileName),
            cancellationToken);
    }
}*/