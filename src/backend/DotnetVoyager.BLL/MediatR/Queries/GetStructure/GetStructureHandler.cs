using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStructure;

public class GetStructureHandler : IRequestHandler<GetStructureQuery, Result<StructureNodeDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;

    public GetStructureHandler(IStorageService storageService, IAnalysisStatusService statusService)
    {
        _storageService = storageService;
        _statusService = statusService;
    }

    public async Task<Result<StructureNodeDto>> Handle(GetStructureQuery request, CancellationToken cancellationToken)
    {
        var statusDto = await _statusService.GetStatusAsync(request.AnalysisId, cancellationToken);

        if (statusDto == null)
        {
            return Result.Fail(new NotFoundError($"Analysis with ID '{request.AnalysisId}' not found."));
        }

        if (statusDto.Status != AnalysisStatus.Completed)
        {
            return Result.Fail(new AnalysisNotCompletedError(statusDto.Status));
        }

        var structureDto = await _storageService.ReadDataAsync<StructureNodeDto>(
            request.AnalysisId,
            ProjectConstants.AnalysisNamespaceStructureFileName,
            cancellationToken);

        if (structureDto == null)
        {
            return Result.Fail(new Error($"Internal error: Analysis '{request.AnalysisId}' is marked as Completed, but structure file is missing."));
        }

        return Result.Ok(structureDto);
    }
}