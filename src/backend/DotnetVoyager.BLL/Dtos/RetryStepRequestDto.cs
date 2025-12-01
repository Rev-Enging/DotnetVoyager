using DotnetVoyager.BLL.Enums;

namespace DotnetVoyager.BLL.Dtos;

public class RetryStepRequestDto
{
    public required AnalysisStepName StepName { get; init; }
}