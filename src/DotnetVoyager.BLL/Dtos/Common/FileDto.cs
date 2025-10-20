namespace DotnetVoyager.BLL.Dtos;

public class FileDto
{
    public Stream FileStream { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = null!;
}