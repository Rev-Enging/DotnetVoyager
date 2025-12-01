using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Commands.PrepareZip;

public record PrepareZipCommand(string AnalysisId) : IRequest<Result>;

public class PrepareZipHandler : IRequestHandler<PrepareZipCommand, Result>
{
    public async Task<Result> Handle(
        PrepareZipCommand request,
        CancellationToken cancellationToken)
    {
        return Result.Ok();
    }
}
