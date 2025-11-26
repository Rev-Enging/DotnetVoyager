using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetAssemblyTree;

public record GetAssemblyTreeQuery(string AnalysisId) : IRequest<Result<AssemblyTreeDto>>;

public class GetAssemblyTreeHandler : BaseAnalysisResultHandler<GetAssemblyTreeQuery, AssemblyTreeDto>
{
    public GetAssemblyTreeHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetAssemblyTreeHandler> logger)
        : base(storageService, statusService, logger)
    {
    }

    public override async Task<Result<AssemblyTreeDto>> Handle(
        GetAssemblyTreeQuery request,
        CancellationToken cancellationToken)
    {
        return await ProcessAnalysisResultAsync(
            request.AnalysisId,
            AnalysisStepNames.AssemblyTree,
            ProjectConstants.AssemblyTreeFileName,
            cancellationToken);
    }
}

/*public class GetAssemblyTreeHandler : IRequestHandler<GetAssemblyTreeQuery, Result<AssemblyTreeDto>>
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
}*/