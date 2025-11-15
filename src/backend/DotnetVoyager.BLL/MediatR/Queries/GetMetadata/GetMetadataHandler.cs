using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetVoyager.BLL.MediatR.Queries.GetMetadata;

public record GetMetadataQuery(string AnalysisId) : IRequest<Result<AssemblyMetadataDto>>;

public class GetMetadataHandler : IRequestHandler<GetMetadataQuery, Result<AssemblyMetadataDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;

    public GetMetadataHandler(IStorageService storageService, IAnalysisStatusService statusService)
    {
        _storageService = storageService;
        _statusService = statusService;
    }

    public async Task<Result<AssemblyMetadataDto>> Handle(GetMetadataQuery request, CancellationToken cancellationToken)
    {
        var statusDto = await _statusService.GetStatusAsync(request.AnalysisId, cancellationToken);

        if (statusDto == null)
        {
            return Result.Fail(new NotFoundError($"Analysis with ID '{request.AnalysisId}' not found."));
        }

        if (statusDto.Status != AssemblyAnalysisStatus.Completed)
        {
            return Result.Fail(new AnalysisNotCompletedError(statusDto.Status));
        }

        var metadataDto = await _storageService.ReadDataAsync<AssemblyMetadataDto>(
            request.AnalysisId,
            ProjectConstants.AnalysisMetadataFileName,
            cancellationToken);

        if (metadataDto == null)
        {
            return Result.Fail(new Error(
                $"Internal error: Analysis '{request.AnalysisId}' is marked as Completed, but metadata file is missing."));
        }

        return Result.Ok(metadataDto);
    }
}
