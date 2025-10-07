namespace DotnetVoyager.WebAPI.Dtos;

public class DecompiledCodeDto
{
    public required string CSharpCode { get; set; }
    public required string IlCode { get; set; }
}
