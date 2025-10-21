using Shapes.Core.Abstractions;

namespace Shapes.Implementation.Models;

public abstract class ShapeBase : IShape
{
    public double Area { get; protected set; }

    public abstract void CalculateArea();
}