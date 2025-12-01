using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetInheritanceGraph;

public record GetInheritanceGraphQuery(string AnalysisId) : IRequest<Result<InheritanceGraphDto>>;

public class GetInheritanceGraphHandler : BaseAnalysisResultHandler<GetInheritanceGraphQuery, InheritanceGraphDto>
{
    public GetInheritanceGraphHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetInheritanceGraphHandler> logger)
        : base(storageService, statusService, logger)
    {
    }

    public override async Task<Result<InheritanceGraphDto>> Handle(
        GetInheritanceGraphQuery request,
        CancellationToken cancellationToken)
    {
        return await ProcessAnalysisResultAsync(
            request.AnalysisId,
            AnalysisStepNames.InheritanceGraph,
            ProjectConstants.AnalysisInheritanceGraphFileName,
            cancellationToken);
    }
}

/*public class GetInheritanceGraphHandler : IRequestHandler<GetInheritanceGraphQuery, Result<InheritanceGraphDto>>
{
    private readonly IMediator _mediator;

    public GetInheritanceGraphHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<Result<InheritanceGraphDto>> Handle(
        GetInheritanceGraphQuery request,
        CancellationToken cancellationToken)
    {
        return _mediator.Send(
            new GetAnalysisResultQuery<InheritanceGraphDto>(
                request.AnalysisId,
                AnalysisStepNames.InheritanceGraph,
                ProjectConstants.AnalysisInheritanceGraphFileName),
            cancellationToken);
    }
}*/
