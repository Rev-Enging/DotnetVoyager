using Shapes.Core.Abstractions;

namespace Shapes.Implementation.Models;

public class Rectangle : ShapeBase, IDrawable
{
    public double Width { get; set; }
    public double Height { get; set; }

    public Rectangle(double width, double height)
    {
        Width = width;
        Height = height;
    }

    public override void CalculateArea()
    {
        Area = Width * Height;
    }

    public void Draw()
    {
        Console.WriteLine("Drawing a rectangle.");
    }
}
