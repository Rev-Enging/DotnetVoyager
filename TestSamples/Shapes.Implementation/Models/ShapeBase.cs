using Shapes.Core.Abstractions;

namespace Shapes.Implementation.Models
{
    /// <summary>
    /// Абстрактний базовий клас, що реалізує IShape.
    /// Демонструє успадкування та реалізацію інтерфейсу.
    /// </summary>
    public abstract class ShapeBase : IShape
    {
        // Властивість реалізована в базовому класі
        public double Area { get; protected set; }

        // Метод залишається абстрактним, щоб змусити нащадків його реалізувати
        public abstract void CalculateArea();
    }
}