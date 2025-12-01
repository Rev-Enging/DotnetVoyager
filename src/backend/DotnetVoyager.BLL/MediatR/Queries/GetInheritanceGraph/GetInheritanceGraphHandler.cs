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
