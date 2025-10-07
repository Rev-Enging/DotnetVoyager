namespace Shapes.Core.Abstractions
{
    /// <summary>
    /// Базовий інтерфейс для будь-якої геометричної фігури.
    /// </summary>
    public interface IShape
    {
        /// <summary>
        /// Площа фігури. Має бути обчислена після виклику CalculateArea().
        /// </summary>
        double Area { get; }

        /// <summary>
        /// Метод для обчислення площі фігури.
        /// </summary>
        void CalculateArea();
    }
}
