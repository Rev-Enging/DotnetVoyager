using DotnetVoyager.BLL.Dtos;
using FluentResults;
using MediatR;

namespace DotnetVoyager.BLL.MediatR.Commands.UploadAssembly;

public record UploadAssemblyCommand(UploadAssemblyDto uploadDto) : IRequest<Result<UploadAssemblyResultDto>>;

public class UploadAssemblyHandler : IRequestHandler<UploadAssemblyCommand, Result<UploadAssemblyResultDto>>
{

}
