using DotnetVoyager.BLL.Dtos;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStatus;

public record GetStatusQuery(string AnalysisId) : IRequest<Result<AnalysisStatusDto>>;
