namespace Shapes.Implementation.Models
{
    /// <summary>
    /// Конкретний клас кола, що успадковується від ShapeBase.
    /// </summary>
    public class Circle : ShapeBase
    {
        public double Radius { get; set; }

        public Circle(double radius)
        {
            Radius = radius;
        }

        public override void CalculateArea()
        {
            // Проста логіка всередині методу
            Area = Math.PI * Radius * Radius;
        }
    }
}
