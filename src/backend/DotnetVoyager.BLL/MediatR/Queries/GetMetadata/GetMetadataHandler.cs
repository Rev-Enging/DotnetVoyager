using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetMetadata;

public record GetMetadataQuery(string AnalysisId) : IRequest<Result<AssemblyMetadataDto>>;

/*public class GetMetadataHandler(IMediator mediator) : IRequestHandler<GetMetadataQuery, Result<AssemblyMetadataDto>>
{
    private readonly IMediator _mediator = mediator;

    public Task<Result<AssemblyMetadataDto>> Handle(
        GetMetadataQuery request,
        CancellationToken cancellationToken)
    {
        return _mediator.Send(
            new GetAnalysisResultQuery<AssemblyMetadataDto>(
                request.AnalysisId,
                AnalysisStepNames.Metadata,
                ProjectConstants.AnalysisMetadataFileName),
            cancellationToken);
    }
}*/

public class GetMetadataHandler : BaseAnalysisResultHandler<GetMetadataQuery, AssemblyMetadataDto>
{
    public GetMetadataHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetMetadataHandler> logger)
        : base(storageService, statusService, logger)
    {
    }

    public override async Task<Result<AssemblyMetadataDto>> Handle(
        GetMetadataQuery request,
        CancellationToken cancellationToken)
    {
        return await ProcessAnalysisResultAsync(
            request.AnalysisId,
            AnalysisStepNames.Metadata,
            ProjectConstants.AnalysisMetadataFileName,
            cancellationToken);
    }
}