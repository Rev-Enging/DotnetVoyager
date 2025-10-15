using DotnetVoyager.BLL.Dtos;

namespace DotnetVoyager.WebAPI.Dtos;

public class UploadAssemblyRequestDto
{
    public IFormFile File { get; init; } = null!;

}

public static class UploadAssemblyRequestDtoExtensions
{
    public static UploadAssemblyDto ToBllDto(this UploadAssemblyRequestDto dto)
    {
        return new UploadAssemblyDto
        {
            File = new FileDto
            {
                FileName = dto.File.FileName,
                ContentType = dto.File.ContentType,
                FileStream = dto.File.OpenReadStream(),
                FileSize = dto.File.Length
            }
        };
    }
}
