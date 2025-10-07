using System.Linq;
using Shapes.Core.Abstractions;

namespace Shapes.Implementation.Utils
{
    /// <summary>
    /// Статичний клас для виконання операцій над фігурами.
    /// </summary>
    public static class ShapeCalculator
    {
        /// <summary>
        /// Статичний метод, що підсумовує площі масиву фігур.
        /// </summary>
        public static double SumAreas(IShape[] shapes)
        {
            foreach (var shape in shapes)
            {
                shape.CalculateArea();
            }

            return shapes.Sum(s => s.Area);
        }
    }
}