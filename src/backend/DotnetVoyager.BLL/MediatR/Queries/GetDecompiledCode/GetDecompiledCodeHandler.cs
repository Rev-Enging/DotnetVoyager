using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Services;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetDecompiledCode;

public record GetDecompiledCodeQuery(string AnalysisId, int lookupToken) : IRequest<Result<DecompiledCodeDto>>;

public class GetDecompiledCodeHandler : IRequestHandler<GetDecompiledCodeQuery, Result<DecompiledCodeDto>>
{
    private readonly IStorageService _storageService;
    private readonly ILogger<GetDecompiledCodeHandler> _logger;
    private readonly ICodeDecompilationService _decompilationService;

    public GetDecompiledCodeHandler(
        IStorageService storageService,
        ICodeDecompilationService decompilationService,
        ILogger<GetDecompiledCodeHandler> logger)
    {
        _logger = logger;
        _decompilationService = decompilationService;
        _storageService = storageService;
    }


    public async Task<Result<DecompiledCodeDto>> Handle(GetDecompiledCodeQuery request, CancellationToken cancellationToken)
    {
        var analysisId = request.AnalysisId;
        var lookupToken = request.lookupToken;

        var assemblyPath = await _storageService.FindAssemblyFilePathAsync(analysisId, cancellationToken);

        if (assemblyPath == null)
        {
            _logger.LogWarning("No assembly file found for Analysis ID: {AnalysisId}.", analysisId);
            return Result.Fail(new NotFoundError($"Assembly file not found for Analysis ID: {analysisId}."));
        }

        try
        {
            var decompiledCode = await _decompilationService.DecompileCodeAsync(assemblyPath, lookupToken);

            return Result.Ok(decompiledCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Decompilation failed for Analysis ID: {AnalysisId} and Token: {LookupToken}.", analysisId, lookupToken);
            return Result.Fail(new Error($"Decompilation failed for token {lookupToken}: {ex.Message}"));
        }
    }
}