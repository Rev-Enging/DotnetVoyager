using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Commands.RetryStep;

public record RetryStepCommand(string AnalysisId, string StepName) : IRequest<Result>;

public class RetryStepHandler : IRequestHandler<RetryStepCommand, Result>
{
    public async Task<Result> Handle(
        RetryStepCommand request,
        CancellationToken cancellationToken)
    {

    }
}
