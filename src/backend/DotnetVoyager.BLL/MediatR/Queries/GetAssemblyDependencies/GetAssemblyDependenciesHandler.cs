using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetAssemblyDependencies;

public record GetAssemblyDependenciesQuery(string AnalysisId) : IRequest<Result<AssemblyDependenciesDto>>;

public class GetAssemblyDependenciesHandler : BaseAnalysisResultHandler<GetAssemblyDependenciesQuery, AssemblyDependenciesDto>
{
    public GetAssemblyDependenciesHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetAssemblyDependenciesHandler> logger)
        : base(storageService, statusService, logger)
    {
    }

    public override async Task<Result<AssemblyDependenciesDto>> Handle(
        GetAssemblyDependenciesQuery request,
        CancellationToken cancellationToken)
    {
        return await ProcessAnalysisResultAsync(
            request.AnalysisId,
            AnalysisStepNames.AssemblyDependencies,
            ProjectConstants.AssemblyDependenciesFileName,
            cancellationToken);
    }
}

/*public class GetAssemblyDependenciesHandler : IRequestHandler<GetAssemblyDependenciesQuery, Result<AssemblyDependenciesDto>>
{
    private readonly IMediator _mediator;

    public GetAssemblyDependenciesHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<Result<AssemblyDependenciesDto>> Handle(
        GetAssemblyDependenciesQuery request,
        CancellationToken cancellationToken)
    {
        return _mediator.Send(
            new GetAnalysisResultQuery<AssemblyDependenciesDto>(
                request.AnalysisId,
                AnalysisStepNames.AssemblyDependencies,
                ProjectConstants.AssemblyDependenciesFileName),
            cancellationToken);
    }
}*/