namespace DotnetVoyager.WebAPI.Dtos;

public class StructureNodeDto
{
    public required string Name { get; set; }

    // Тип вузла, щоб фронтенд міг малювати різні іконки ("namespace", "class", "method")
    public required string Type { get; set; }

    // Наш знаменитий Metadata Token для майбутніх запитів
    public int Token { get; set; }

    // Список дочірніх вузлів для створення ієрархії
    public List<StructureNodeDto>? Children { get; set; }
}
