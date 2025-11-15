using DotnetVoyager.BLL.Dtos;

namespace DotnetVoyager.WebAPI.Exensions;

public static class FormFileExtensions
{
    public static FileDto ToFileDto(this IFormFile formFile)
    {
        return new FileDto
        {
            FileName = formFile.FileName,
            ContentType = formFile.ContentType,
            FileStream = formFile.OpenReadStream(),
            FileSize = formFile.Length
        };
    }
}
