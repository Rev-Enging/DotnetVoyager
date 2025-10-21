namespace DotnetVoyager.BLL.Dtos;

public class DecompiledCodeDto
{
    public required string CSharpCode { get; init; }
    public required string IlCode { get; init; }
}
