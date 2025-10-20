using DotnetVoyager.BLL.Dtos;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStructure;

public record GetStructureQuery(string AnalysisId) : IRequest<Result<StructureNodeDto>>;
