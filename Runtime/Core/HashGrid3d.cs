using System;
using System.Runtime.CompilerServices;
namespace FloatingOffset.Runtime
{
    // public class HashGrid<T> where T : IEquatable<T>
    // {
    //     public static readonly Vector3d[] SEARCH_PATTERN = {
    //         new Vector3d(0,0,0),
    //         new Vector3d(0,0,1),
    //         new Vector3d(1,0,1),

    //         new Vector3d(1,0,0),
    //         new Vector3d(1,0,-1),
    //         new Vector3d(0,0,-1),

    //         new Vector3d(-1,0,-1),
    //         new Vector3d(-1,0,0),
    //         new Vector3d(-1,0,1),


    //         new Vector3d(-1,1,1),
    //         new Vector3d(0,1,1),
    //         new Vector3d(1,1,1),

    //         new Vector3d(1,1,0),
    //         new Vector3d(1,1,-1),
    //         new Vector3d(0,1,-1),

    //         new Vector3d(-1,1,-1),
    //         new Vector3d(-1,1,0),
    //         new Vector3d(0,1,0),


    //         new Vector3d(0,-1,0),
    //         new Vector3d(0,-1,1),
    //         new Vector3d(1,-1,1),

    //         new Vector3d(1,-1,0),
    //         new Vector3d(1,-1,-1),
    //         new Vector3d(0,-1,-1),

    //         new Vector3d(-1,-1,-1),
    //         new Vector3d(-1,-1,0),
    //         new Vector3d(-1,-1,1),

    //      };
    //     private readonly Dictionary<Vector3d, HashSet<T>> dict = new Dictionary<Vector3d, HashSet<T>>();
    //     private readonly Dictionary<T, Vector3d> original_positions = new Dictionary<T, Vector3d>();
    //     private readonly int resolution;
    //     private readonly float resolutionInverseScalar;
    //     public HashGrid(int resolution)
    //     {
    //         this.resolution = resolution;
    //         this.resolutionInverseScalar = 1 / ((float)resolution);
    //     }
    //     public int Count { get => dict.Count; }
    //     /// <summary>
    //     /// Add the given value at the given vector. <code>O(1)</code>
    //     /// </summary>
    //     /// <param name="vector"></param>
    //     /// <param name="value"></param>
    //     public void Add(Vector3d vector, T value)
    //     {
    //         Vector3d quantized = Quantize(vector);
    //         if (!dict.ContainsKey(quantized))
    //         {
    //             dict.Add(quantized, new HashSet<T>());
    //         }
    //         dict[quantized].Add(value);
    //         original_positions.Add(value, vector);
    //     }
    //     /// <summary>
    //     /// Quantize the given vector using this hash grid's resolution. <code>O(1)</code>
    //     /// </summary>
    //     /// <param name="vector"></param>
    //     /// <returns></returns>
    //     public Vector3d Quantize(Vector3d vector)
    //     {
    //         return new Vector3d(
    //                         Mathd.Floor(vector.x * resolutionInverseScalar),
    //                         Mathd.Floor(vector.y * resolutionInverseScalar),
    //                         Mathd.Floor(vector.z * resolutionInverseScalar));
    //     }
    //     /// <summary>
    //     /// Is the given Vector3d in the search grid? <code>O(1)</code>
    //     /// </summary>
    //     /// <param name="vector"></param>
    //     /// <returns></returns>
    //     public bool Has(Vector3d vector)
    //     {
    //         return dict.ContainsKey(Quantize(vector));
    //     }
    //     /// <summary>
    //     /// Is the given value present in the search grid it was originally registered in? <code>O(1)</code>
    //     /// </summary>
    //     /// <param name="vector"></param>
    //     /// <param name="value"></param>
    //     /// <returns></returns>
    //     public bool HasQuantized(Vector3d vector, T value)
    //     {
    //         return dict[Quantize(vector)].Contains(value);
    //     }
    //     /// <summary>
    //     /// The original position that the given value was registered under. <code>O(1)</code>
    //     /// </summary>
    //     /// <param name="value"></param>
    //     /// <param name="position"></param>
    //     /// <returns></returns>
    //     public bool TryGetOriginalPosition(T value, out Vector3d position)
    //     {
    //         if (!original_positions.ContainsKey(value))
    //         {
    //             position = Vector3d.zero;
    //             return false;
    //         }
    //         position = original_positions[value];
    //         return true;
    //     }
    //     /// <summary>
    //     /// Finds a set of objects in the given Bounding Box.
    //     /// </summary>
    //     /// <param name="center">
    //     /// Center of search.
    //     /// </param>
    //     /// <param name="distance">
    //     /// Search bounding box radius.
    //     /// </param>
    //     /// <returns>
    //     /// A HashSet containing all found objects.
    //     /// </returns>
    //     public T[] FindInBoundingBox(Vector3d center, double distance)
    //     {
    //         if (distance > resolution * 2)
    //         {
    //             throw new System.Exception("Distance is greater than resolution");
    //         }
    //         int range = (int)Mathd.Ceil(distance * (1d / resolution));
    //         range += range % 2;

    //         List<T> found = new List<T>();

    //         Vector3d initial = Quantize(center) * resolution;

    //         for (int i = 0; i < SEARCH_PATTERN.Length; i++)
    //         {
    //             if (dict.ContainsKey(Quantize(initial + SEARCH_PATTERN[i])))
    //             {
    //                 foreach (var cell_element in dict[Quantize(initial + SEARCH_PATTERN[i])])
    //                 {
    //                     if (Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
    //                         found.Add(cell_element);
    //                 }
    //             }
    //         }
    //         return found.ToArray();
    //     }
    //     [Obsolete("Returns default value of T if nothing is found. Use TryFindAnyInBoundingBox() instead.")]
    //     public T FindAnyInBoundingBox(Vector3d center, double distance, T exclude = default)
    //     {
    //         TryFindAnyInBoundingBox(center, distance, exclude, out T value);
    //         return value;
    //     }
    //     public bool TryFindAnyInBoundingBox(Vector3d center, double distance, T exclude, out T found)
    //     {
    //         int range = (int)Mathd.Ceil(distance * (1d / resolution));
    //         range += range % 2;

    //         Vector3d initial = Quantize(center) * resolution;

    //         for (int i = 0; i < SEARCH_PATTERN.Length; i++)
    //         {
    //             if (dict.ContainsKey(Quantize(initial + SEARCH_PATTERN[i])))
    //             {
    //                 foreach (var cell_element in dict[Quantize(initial + SEARCH_PATTERN[i])])
    //                 {
    //                     if (!EqualityComparer<T>.Default.Equals(cell_element, exclude) && Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
    //                     {
    //                         found = cell_element;
    //                         return true;
    //                     }
    //                 }
    //             }
    //         }
    //         found = default;
    //         return false;
    //     }
    //     public bool TryFindAnyInBoundingBox(Vector3d center, double distance, out T found)
    //     {
    //         int range = (int)Mathd.Ceil(distance * (1d / resolution));
    //         range += range % 2;

    //         Vector3d initial = Quantize(center) * resolution;

    //         for (int i = 0; i < SEARCH_PATTERN.Length; i++)
    //         {
    //             if (dict.ContainsKey(Quantize(initial + SEARCH_PATTERN[i])))
    //             {
    //                 foreach (var cell_element in dict[Quantize(initial + SEARCH_PATTERN[i])])
    //                 {
    //                     if (Mathd.MaxLengthScalar(original_positions[cell_element] - center) < distance)
    //                     {
    //                         found = cell_element;
    //                         return true;
    //                     }
    //                 }
    //             }
    //         }
    //         found = default;
    //         return false;
    //     }
    //     public HashSet<T> this[Vector3d vector] => dict[Quantize(vector)];
    //     public void Clear() => dict.Clear();
    //     public void Remove(Vector3d vector) => dict.Remove(Quantize(vector));
    // }

    public class HashGrid
    {
        private const uint PrimeX = 73856093;
        private const uint PrimeY = 19349663;
        private const uint PrimeZ = 83492791;

        private int bucketMask;
        private int[] buckets;

        private int[] entries_value;
        private int[] entries_next;

        private int entry_count;

        // Unified variable for cell sizing
        private double invCellSize;
        private double searchRadiusSq;

        public HashGrid(int bucketCount, int initialCapacity, double maxSearchRadius)
        {
            buckets = new int[bucketCount];
            bucketMask = bucketCount - 1; // Requires bucketCount to be a power of 2

            entries_value = new int[initialCapacity];
            entries_next = new int[initialCapacity];

            Array.Fill(buckets, -1);

            // The cell size MUST be exactly double the search radius for the 8-neighbor logic to hold true
            invCellSize = 1.0 / (maxSearchRadius * 2.0);
            searchRadiusSq = maxSearchRadius * maxSearchRadius;
        }

        public void Clear()
        {
            Array.Fill(buckets, -1);
            entry_count = 0;
        }

        private void EnsureCapacity()
        {
            if (entry_count >= entries_value.Length)
            {
                int newSize = entries_value.Length * 2;
                Array.Resize(ref entries_value, newSize);
                Array.Resize(ref entries_next, newSize);
            }
        }

        // Mathematically correct floor that handles negative numbers safely
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FastFloor(double t)
        {
            int val = (int)t;
            return t < val ? val - 1 : val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int HashGridPosition(int gridX, int gridY, int gridZ)
        {
            uint h = (uint)(gridX * PrimeX ^ gridY * PrimeY ^ gridZ * PrimeZ);
            return (int)(h & bucketMask);
        }

        public void Add(Vector3d position, int viewIndex)
        {
            EnsureCapacity();

            // Calculate correct grid coordinates for negative/positive space
            int gridX = FastFloor(position.x * invCellSize);
            int gridY = FastFloor(position.y * invCellSize);
            int gridZ = FastFloor(position.z * invCellSize);

            int hash = HashGridPosition(gridX, gridY, gridZ);

            entries_value[entry_count] = viewIndex;
            entries_next[entry_count] = buckets[hash];
            buckets[hash] = entry_count;
            entry_count++;
        }

        // Passed view_positions array so TraverseBucket can do precise checks
        public void FindNeighbors(Vector3d position, Vector3d[] view_positions, ref int[] resultsBuffer, out int resultCount)
        {
            resultCount = 0;

            // Scale the position
            double scaledX = position.x * invCellSize;
            double scaledY = position.y * invCellSize;
            double scaledZ = position.z * invCellSize;

            // Get the Home Cell (Grid Coordinates) using FastFloor
            int gridX = FastFloor(scaledX);
            int gridY = FastFloor(scaledY);
            int gridZ = FastFloor(scaledZ);

            // Find the fractional position (0.0 to 1.0) inside the home cell
            double fracX = scaledX - gridX;
            double fracY = scaledY - gridY;
            double fracZ = scaledZ - gridZ;

            int dx = fracX < 0.5 ? -1 : 1;
            int dy = fracY < 0.5 ? -1 : 1;
            int dz = fracZ < 0.5 ? -1 : 1;

            int x0 = gridX, x1 = gridX + dx;
            int y0 = gridY, y1 = gridY + dy;
            int z0 = gridZ, z1 = gridZ + dz;

            int hash0 = HashGridPosition(x0, y0, z0);
            int hash1 = HashGridPosition(x1, y0, z0);
            int hash2 = HashGridPosition(x0, y1, z0);
            int hash3 = HashGridPosition(x1, y1, z0);
            int hash4 = HashGridPosition(x0, y0, z1);
            int hash5 = HashGridPosition(x1, y0, z1);
            int hash6 = HashGridPosition(x0, y1, z1);
            int hash7 = HashGridPosition(x1, y1, z1);

            // Pass the view_positions array down the chain
            TraverseBucket(hash0, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash1, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash2, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash3, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash4, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash5, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash6, position, view_positions, ref resultsBuffer, ref resultCount);
            TraverseBucket(hash7, position, view_positions, ref resultsBuffer, ref resultCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TraverseBucket(int bucketHash, Vector3d searchPos, Vector3d[] view_positions, ref int[] resultsBuffer, ref int resultCount)
        {
            int entryIndex = buckets[bucketHash];

            while (entryIndex != -1)
            {
                int viewIndex = entries_value[entryIndex];

                // Precise distance check using the external positions array
                Vector3d targetPos = view_positions[viewIndex];

                // Manual sqrMagnitude (assuming you have this property on Vector3d, if not, write it out)
                double diffX = searchPos.x - targetPos.x;
                double diffY = searchPos.y - targetPos.y;
                double diffZ = searchPos.z - targetPos.z;
                double distSq = (diffX * diffX) + (diffY * diffY) + (diffZ * diffZ);

                if (distSq <= searchRadiusSq)
                {
                    // guard clause
                    if (resultCount >= resultsBuffer.Length)
                    {
                        Array.Resize(ref resultsBuffer, resultsBuffer.Length * 2);
                    }

                    resultsBuffer[resultCount++] = viewIndex;
                }

                entryIndex = entries_next[entryIndex];
            }
        }
    }
}
