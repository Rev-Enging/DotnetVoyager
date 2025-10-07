using Shapes.Core.Abstractions;
using Shapes.Implementation.Models;

namespace Shapes.Implementation.Models
{
    /// <summary>
    /// Клас прямокутника, що реалізує два інтерфейси (один з них через базовий клас).
    /// </summary>
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
            // Цей метод нічого не робить, але він є для демонстрації реалізації інтерфейсу.
            System.Console.WriteLine("Drawing a rectangle.");
        }
    }
}
