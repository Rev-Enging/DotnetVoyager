using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetAssemblyDependencies;

public record GetAssemblyDependenciesQuery(string AnalysisId) : IRequest<Result<AssemblyDependenciesDto>>;

public class GetAssemblyDependenciesHandler : IRequestHandler<GetAssemblyDependenciesQuery, Result<AssemblyDependenciesDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<GetAssemblyDependenciesHandler> _logger;

    public GetAssemblyDependenciesHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetAssemblyDependenciesHandler> logger)
    {
        _storageService = storageService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Result<AssemblyDependenciesDto>> Handle(
        GetAssemblyDependenciesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string stepName = AnalysisStepNames.AssemblyDependencies;
            const string fileName = ProjectConstants.AssemblyDependenciesFileName;

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

            var assemblyDependencies = await _storageService.ReadDataAsync<AssemblyDependenciesDto>(
                request.AnalysisId,
                fileName,
                cancellationToken);

            if (assemblyDependencies == null)
            {
                _logger.LogError(
                    "Assembly dependencies file missing for completed analysis {AnalysisId}",
                    request.AnalysisId);

                return Result.Fail(new Error(
                    "Internal error: Assembly dependencies step completed but file is missing."));
            }

            return Result.Ok(assemblyDependencies);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Assembly dependencies requested for non-existent analysis: {AnalysisId}",
                ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}