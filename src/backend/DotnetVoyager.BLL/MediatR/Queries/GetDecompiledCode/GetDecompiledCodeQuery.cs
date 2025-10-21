using DotnetVoyager.BLL.Dtos;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetDecompiledCode;

public record GetDecompiledCodeQuery(string AnalysisId, int lookupToken) : IRequest<Result<DecompiledCodeDto>>;
