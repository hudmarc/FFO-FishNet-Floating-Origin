using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public class HashGrid<T>
    {
        private Dictionary<Vector3d, HashSet<T>> dict = new Dictionary<Vector3d, HashSet<T>>();
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
                dict.Add(quantized, new HashSet<T>());
            }
            dict[quantized].Add(value);
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
        public HashSet<T> FindInBoundingBox(Vector3d vector, double range)
        {
            HashSet<T> found = new HashSet<T>();
            double quantizedHalfRange = Mathd.Floor(range * resolutionInverseScalar * 0.5);

            Vector3d cell = Quantize(vector) - new Vector3d(quantizedHalfRange, quantizedHalfRange, quantizedHalfRange);

            for (int x = 0; x < Mathd.Ceil(range); x++)
            {
                for (int y = 0; y < Mathd.Ceil(range); y++)
                {
                    for (int z = 0; z < Mathd.Ceil(range); z++)
                    {
                        if (dict.ContainsKey(cell))
                        {
                            foreach (var cell_element in dict[cell])
                            {
                                found.Add(cell_element);
                            }
                        }
                        cell += new Vector3d(0, 0, 1);
                    }
                    cell += new Vector3d(0, 1, 0);
                }
                cell += new Vector3d(1, 0, 0);
            }
            return found;
        }
        public HashSet<T> this[Vector3d vector] => dict[Quantize(vector)];
    }
}
