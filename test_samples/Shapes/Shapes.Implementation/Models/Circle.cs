namespace Shapes.Implementation.Models;

public class Circle : ShapeBase
{
    public double Radius { get; set; }

    public Circle(double radius)
    {
        Radius = radius;
    }

    public override void CalculateArea()
    {
        Area = Math.PI * Radius * Radius;
    }
}
