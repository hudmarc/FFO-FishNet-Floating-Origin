using System;
using System.Runtime.CompilerServices;

namespace FloatingOffset.Runtime
{
    public sealed class FastUnionFind
    {
        public ScenedUnion[] unions;

        public ref ScenedUnion this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref unions[index];
        }
        public FastUnionFind(int initialCapacity)
        {
            unions = new ScenedUnion[initialCapacity];
        }

        public void EnsureCapacity(int count)
        {
            if (unions.Length < count)
            {
                int newSize = count * 2; // Or NextPowerOfTwo
                Array.Resize(ref unions, newSize);
            }
        }

        // Force the compiler to paste this inside your loop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeNode(int member_index, int scene_index)
        {
            unions[member_index] = new ScenedUnion { scene_index = scene_index, representative = Find(member_index) };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Find(int i)
        {
            int root = i;
            while (root != unions[root].representative)
                root = unions[root].representative;

            int curr = i;
            while (curr != root)
            {
                int nxt = unions[curr].representative;
                unions[curr].representative = root;
                curr = nxt;
            }
            return root;
        }

        /// <summary>
        /// Merges j's union into i's union. Representative will be union i's representative.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);

            if (rootI != rootJ)
            {
                unions[rootJ].representative = rootI;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AggregateData(int member_index, int scene_index)
        {
            int root = Find(member_index);
            unions[member_index].scene_index = scene_index;
        }

        internal ScenedUnion[] Sorted()
        {
            ScenedUnion[] sorted = new ScenedUnion[unions.Length];
            unions.CopyTo(sorted,0);
            Array.Sort(sorted);
            return sorted;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unions.Length;
        }
        public struct ScenedUnion : IComparable<ScenedUnion>
        {
            public int scene_index;
            public int representative;

            public int CompareTo(ScenedUnion other)
            {
                // Default generic comparers are safe and optimized here
                int cmp = System.Collections.Generic.Comparer<int>.Default.Compare(scene_index, other.scene_index);
                if (cmp == 0)
                {
                    cmp = representative.CompareTo(other.representative);
                }
                return cmp;
            }
        }
    }
}
