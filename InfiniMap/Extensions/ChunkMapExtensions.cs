using System;
using System.Collections.Generic;

namespace InfiniMap
{
    public static class ChunkMapExtensions
    {
        /// <summary>
        /// Provides a centered square distance on a center point including negative chunk
        /// coordinates.
        /// An odd value for range will round upwards.
        /// </summary>
        /// <remarks>
        /// Due to rounding, odd values for range will provide the same value as the next
        /// even number. That is, 'range: 5' will return the same values as 'range: 6' and
        /// 'range: 1' will return the same values as 'range: 2'
        /// </remarks>
        /// <param name="startX">Center position</param>
        /// <param name="startY">Center position</param>
        /// <param name="range">Range of search</param>
        /// <returns>A list of coordinates that are within the area</returns>
        public static IEnumerable<Tuple<int, int>> Distance<T>(this ChunkMap<T> context, int startX, int startY, int range)
        {
            range = (range % 2 == 0) ? range : range + 1;

            var topLeft = Tuple.Create(startX - (range / 2), startY - (range / 2));
            var topRight = Tuple.Create(startX + (range / 2), startY - (range / 2));
            var bottomLeft = Tuple.Create(startX - (range / 2), startY + (range / 2));

            for (int x = topLeft.Item1; x <= topRight.Item1; x++)
            {
                for (int y = topLeft.Item2; y <= bottomLeft.Item2; y++)
                {
                    yield return Tuple.Create(x, y);
                }
            }
        }
    }
}