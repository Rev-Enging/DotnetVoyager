using Shapes.Core.Abstractions;

namespace Shapes.Implementation.Utils;

public static class ShapeCalculator
{
    public static double SumAreas(IShape[] shapes)
    {
        foreach (var shape in shapes)
        {
            shape.CalculateArea();
        }

        return shapes.Sum(s => s.Area);
    }
}