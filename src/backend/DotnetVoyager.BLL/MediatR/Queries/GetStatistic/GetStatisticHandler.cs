using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStatistic;

public record GetStatisticQuery(string AnalysisId) : IRequest<Result<AssemblyStatisticsDto>>;

public class GetStatisticHandler(IMediator mediator) : IRequestHandler<GetStatisticQuery, Result<AssemblyStatisticsDto>>
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
}