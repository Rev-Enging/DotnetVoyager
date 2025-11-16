using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStructure;

public record GetAssemblyTreeQuery(string AnalysisId) : IRequest<Result<AssemblyTreeDto>>;

public class GetAssemblyTreeHandler : IRequestHandler<GetAssemblyTreeQuery, Result<AssemblyTreeDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<GetAssemblyTreeHandler> _logger;

    public GetAssemblyTreeHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetAssemblyTreeHandler> logger)
    {
        _storageService = storageService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Result<AssemblyTreeDto>> Handle(
        GetAssemblyTreeQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string stepName = AnalysisStepNames.AssemblyTree;
            const string fileName = ProjectConstants.AssemblyTreeFileName;

            var stepStatus = await _statusService.GetStepStatusAsync(
                request.AnalysisId,
                stepName,
                cancellationToken);

            if (stepStatus.Status == AnalysisStepStatus.Failed)
            {
                return Result.Fail(new StepFailedError(
                    stepName,
                    stepStatus.ErrorMessage));
            }

            if (stepStatus.Status != AnalysisStepStatus.Completed)
            {
                return Result.Fail(new StepNotCompletedError(
                    stepName,
                    stepStatus.Status,
                    stepStatus.ErrorMessage));
            }

            var structureDto = await _storageService.ReadDataAsync<AssemblyTreeDto>(
                request.AnalysisId,
                fileName,
                cancellationToken);

            if (structureDto == null)
            {
                _logger.LogError(
                    "Assembly tree file missing for completed analysis {AnalysisId}",
                    request.AnalysisId);

                return Result.Fail(new Error(
                    "Internal error: Assembly tree step completed but file is missing."));
            }

            return Result.Ok(structureDto);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Assembly tree requested for non-existent analysis: {AnalysisId}",
                ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}
