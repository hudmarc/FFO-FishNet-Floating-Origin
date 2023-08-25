using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class HashGrid<T> where T : class
    {
        private Dictionary<Vector3d, Dictionary<T, Vector3d>> dict = new Dictionary<Vector3d, Dictionary<T, Vector3d>>();
        private readonly int resolution;
        private readonly float resolutionInverseScalar;
        public HashGrid(int resolution)
        {
            this.resolution = resolution;
            this.resolutionInverseScalar = 1 / ((float)resolution);
        }

        public void Add(Vector3d vector, T value)
        {
            Vector3d quantized = Quantize(vector);
            if (!dict.ContainsKey(quantized))
            {
                dict.Add(quantized, new Dictionary<T, Vector3d>());
            }
            dict[quantized].Add(value, vector);
        }
        public Vector3d Quantize(Vector3d vector)
        {
            return new Vector3d(
                            Mathd.Floor(vector.x * resolutionInverseScalar),
                            Mathd.Floor(vector.y * resolutionInverseScalar),
                            Mathd.Floor(vector.z * resolutionInverseScalar));
        }
        public void Remove(Vector3d vector)
        {
            dict.Remove(Quantize(vector));
        }
        public bool Has(Vector3d vector)
        {
            return dict.ContainsKey(Quantize(vector));
        }
        /// <summary>
        /// Finds a set of objects in the given Bounding Box. This function is not very fast.
        /// </summary>
        /// <param name="vector">
        /// Center of search.
        /// </param>
        /// <param name="distance">
        /// Search bounding box radius.
        /// </param>
        /// <returns></returns>
        public HashSet<T> FindInBoundingBox(Vector3d vector, double distance)
        {
            int range = (int)Mathd.Ceil(distance * (1d / resolution));
            range += range % 2;

            HashSet<T> found = new HashSet<T>();

            Vector3d initial = Quantize(vector - new Vector3d(resolution * (range / 2), resolution * (range / 2), resolution * (range / 2))) * resolution;
            Vector3d cell = initial;
            for (int x = 0; x < range; x++)
            {
                cell.y = initial.y;
                for (int y = 0; y < range; y++)
                {
                    cell.z = initial.z;
                    for (int z = 0; z < range; z++)
                    {
                        if (dict.ContainsKey(Quantize(cell)))
                        {
                            foreach (var cell_element in dict[Quantize(cell)])
                            {
                                if (Functions.MaxLengthScalar(cell_element.Value - vector) < distance)
                                    found.Add(cell_element.Key);
                            }
                        }
                        // Debug.Log(cell);
                        cell += new Vector3d(0, 0, resolution);
                    }
                    cell += new Vector3d(0, resolution, 0);
                }
                cell += new Vector3d(resolution, 0, 0);
            }
            return found;
        }
        public T FindAnyInBoundingBox(Vector3d vector, double distance, T exclude = null)
        {
            int range = (int)Mathd.Ceil(distance * (1d / resolution));
            range += range % 2;

            Vector3d initial = Quantize(vector - new Vector3d(resolution * (range / 2), resolution * (range / 2), resolution * (range / 2))) * resolution;
            Vector3d cell = initial;
            for (int x = 0; x < range; x++)
            {
                cell.y = initial.y;
                for (int y = 0; y < range; y++)
                {
                    cell.z = initial.z;
                    for (int z = 0; z < range; z++)
                    {
                        if (dict.ContainsKey(Quantize(cell)))
                        {
                            foreach (var cell_element in dict[Quantize(cell)])
                            {
                                if (cell_element.Key != exclude && Functions.MaxLengthScalar(cell_element.Value - vector) < distance)
                                    return cell_element.Key;
                            }
                        }
                        // Debug.Log(cell);
                        cell += new Vector3d(0, 0, resolution);
                    }
                    cell += new Vector3d(0, resolution, 0);
                }
                cell += new Vector3d(resolution, 0, 0);
            }
            return null;
        }
        public HashSet<T> this[Vector3d vector] => dict[Quantize(vector)].Keys.ToHashSet<T>();
    }
}
