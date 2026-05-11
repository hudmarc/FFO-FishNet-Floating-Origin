using System.Collections.Generic;
using UnityEngine;

namespace FloatingOffset.Runtime
{
    public class HashGrid<T> where T : class
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
        public Vector3d Quantize(Vector3d vector)
        {
            return new Vector3d(
                            Mathd.Floor(vector.x * resolutionInverseScalar),
                            Mathd.Floor(vector.y * resolutionInverseScalar),
                            Mathd.Floor(vector.z * resolutionInverseScalar));
        }
        public bool Has(Vector3d vector)
        {
            return dict.ContainsKey(Quantize(vector));
        }
        public bool Contains(Vector3d vector, T value)
        {
            return dict[Quantize(vector)].Contains(value);
        }
        public Vector3d GetOriginalPosition(T value)
        {
            return original_positions[value];
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
        public HashSet<T> FindInBoundingBox(Vector3d center, double distance)
        {
            if (distance > resolution * 2)
            {
                throw new System.Exception("Distance is greater than resolution");
            }
            int range = (int)Mathd.Ceil(distance * (1d / resolution));
            range += range % 2;

            HashSet<T> found = new HashSet<T>();

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
            return found;
        }
        public T FindAnyInBoundingBox(Vector3d center, double distance, T exclude = null)
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
                        if (cell_element != exclude && Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
                            return cell_element;
                    }
                }
            }
            return null;
        }
        public HashSet<T> this[Vector3d vector] => dict[Quantize(vector)];
        public void Clear() => dict.Clear();
        public void Remove(Vector3d vector) => dict.Remove(Quantize(vector));
    }
}
