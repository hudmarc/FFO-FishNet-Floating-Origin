using System;
using System.Collections.Generic;

namespace FloatingOffset.Runtime
{
    public class HashGrid<T> where T : IEquatable<T>
    {
        public static readonly Vector3d[] SEARCH_PATTERN = {
            new Vector3d(0,0,0),
            new Vector3d(0,0,1),
            new Vector3d(1,0,1),

            new Vector3d(1,0,0),
            new Vector3d(1,0,-1),
            new Vector3d(0,0,-1),

            new Vector3d(-1,0,-1),
            new Vector3d(-1,0,0),
            new Vector3d(-1,0,1),


            new Vector3d(-1,1,1),
            new Vector3d(0,1,1),
            new Vector3d(1,1,1),

            new Vector3d(1,1,0),
            new Vector3d(1,1,-1),
            new Vector3d(0,1,-1),

            new Vector3d(-1,1,-1),
            new Vector3d(-1,1,0),
            new Vector3d(0,1,0),


            new Vector3d(0,-1,0),
            new Vector3d(0,-1,1),
            new Vector3d(1,-1,1),

            new Vector3d(1,-1,0),
            new Vector3d(1,-1,-1),
            new Vector3d(0,-1,-1),

            new Vector3d(-1,-1,-1),
            new Vector3d(-1,-1,0),
            new Vector3d(-1,-1,1),

         };
        private readonly Dictionary<Vector3d, HashSet<T>> dict = new Dictionary<Vector3d, HashSet<T>>();
        private readonly Dictionary<T, Vector3d> original_positions = new Dictionary<T, Vector3d>();
        private readonly int resolution;
        private readonly float resolutionInverseScalar;
        public HashGrid(int resolution)
        {
            this.resolution = resolution;
            this.resolutionInverseScalar = 1 / ((float)resolution);
        }
        public int Count { get => dict.Count; }
        /// <summary>
        /// Add the given value at the given vector. <code>O(1)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="value"></param>
        public void Add(Vector3d vector, T value)
        {
            Vector3d quantized = Quantize(vector);
            if (!dict.ContainsKey(quantized))
            {
                dict.Add(quantized, new HashSet<T>());
            }
            dict[quantized].Add(value);
            original_positions.Add(value, vector);
        }
        /// <summary>
        /// Quantize the given vector using this hash grid's resolution. <code>O(1)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public Vector3d Quantize(Vector3d vector)
        {
            return new Vector3d(
                            Mathd.Floor(vector.x * resolutionInverseScalar),
                            Mathd.Floor(vector.y * resolutionInverseScalar),
                            Mathd.Floor(vector.z * resolutionInverseScalar));
        }
        /// <summary>
        /// Is the given Vector3d in the search grid? <code>O(1)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public bool Has(Vector3d vector)
        {
            return dict.ContainsKey(Quantize(vector));
        }
        /// <summary>
        /// Is the given value present in the search grid it was originally registered in? <code>O(1)</code>
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HasQuantized(Vector3d vector, T value)
        {
            return dict[Quantize(vector)].Contains(value);
        }
        /// <summary>
        /// The original position that the given value was registered under. <code>O(1)</code>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool TryGetOriginalPosition(T value, out Vector3d position)
        {
            if (!original_positions.ContainsKey(value))
            {
                position = Vector3d.zero;
                return false;
            }
            position = original_positions[value];
            return true;
        }
        /// <summary>
        /// Finds a set of objects in the given Bounding Box.
        /// </summary>
        /// <param name="center">
        /// Center of search.
        /// </param>
        /// <param name="distance">
        /// Search bounding box radius.
        /// </param>
        /// <returns>
        /// A HashSet containing all found objects.
        /// </returns>
        public T[] FindInBoundingBox(Vector3d center, double distance)
        {
            if (distance > resolution * 2)
            {
                throw new System.Exception("Distance is greater than resolution");
            }
            int range = (int)Mathd.Ceil(distance * (1d / resolution));
            range += range % 2;

            List<T> found = new List<T>();

            Vector3d initial = Quantize(center) * resolution;

            for (int i = 0; i < SEARCH_PATTERN.Length; i++)
            {
                if (dict.ContainsKey(Quantize(initial + SEARCH_PATTERN[i])))
                {
                    foreach (var cell_element in dict[Quantize(initial + SEARCH_PATTERN[i])])
                    {
                        if (Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
                            found.Add(cell_element);
                    }
                }
            }
            return found.ToArray();
        }
        [Obsolete("Returns default value of T if nothing is found. Use TryFindAnyInBoundingBox() instead.")]
        public T FindAnyInBoundingBox(Vector3d center, double distance, T exclude = default)
        {
            TryFindAnyInBoundingBox(center, distance, exclude, out T value);
            return value;
        }
        public bool TryFindAnyInBoundingBox(Vector3d center, double distance, T exclude, out T found)
        {
            int range = (int)Mathd.Ceil(distance * (1d / resolution));
            range += range % 2;

            Vector3d initial = Quantize(center) * resolution;

            for (int i = 0; i < SEARCH_PATTERN.Length; i++)
            {
                if (dict.ContainsKey(Quantize(initial + SEARCH_PATTERN[i])))
                {
                    foreach (var cell_element in dict[Quantize(initial + SEARCH_PATTERN[i])])
                    {
                        if (!EqualityComparer<T>.Default.Equals(cell_element, exclude) && Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
                        {
                            found = cell_element;
                            return true;
                        }
                    }
                }
            }
            found = default;
            return false;
        }
        public bool TryFindAnyInBoundingBox(Vector3d center, double distance, out T found)
        {
            int range = (int)Mathd.Ceil(distance * (1d / resolution));
            range += range % 2;

            Vector3d initial = Quantize(center) * resolution;

            for (int i = 0; i < SEARCH_PATTERN.Length; i++)
            {
                if (dict.ContainsKey(Quantize(initial + SEARCH_PATTERN[i])))
                {
                    foreach (var cell_element in dict[Quantize(initial + SEARCH_PATTERN[i])])
                    {
                        if (Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
                        {
                            found = cell_element;
                            return true;
                        }
                    }
                }
            }
            found = default;
            return false;
        }
        public HashSet<T> this[Vector3d vector] => dict[Quantize(vector)];
        public void Clear() => dict.Clear();
        public void Remove(Vector3d vector) => dict.Remove(Quantize(vector));
    }
}
