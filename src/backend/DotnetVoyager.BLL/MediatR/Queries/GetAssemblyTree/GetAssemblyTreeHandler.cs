using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetAssemblyTree;

public record GetAssemblyTreeQuery(string AnalysisId) : IRequest<Result<AssemblyTreeDto>>;

public class GetAssemblyTreeHandler : IRequestHandler<GetAssemblyTreeQuery, Result<AssemblyTreeDto>>
{
    private readonly IMediator _mediator;

    public GetAssemblyTreeHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<Result<AssemblyTreeDto>> Handle(
        GetAssemblyTreeQuery request,
        CancellationToken cancellationToken)
    {
        return _mediator.Send(
            new GetAnalysisResultQuery<AssemblyTreeDto>(
                request.AnalysisId,
                AnalysisStepNames.AssemblyTree,
                ProjectConstants.AssemblyTreeFileName),
            cancellationToken);
    }
}