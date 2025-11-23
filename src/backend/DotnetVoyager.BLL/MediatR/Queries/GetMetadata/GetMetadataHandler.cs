using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.MediatR.Queries.Common;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetMetadata
{
    public record GetMetadataQuery(string AnalysisId) : IRequest<Result<AssemblyMetadataDto>>;

    public class GetMetadataHandler(IMediator mediator) : IRequestHandler<GetMetadataQuery, Result<AssemblyMetadataDto>>
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
    }
}