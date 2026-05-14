using System;
using System.Runtime.CompilerServices;

namespace FloatingOffset.Runtime
{
    public sealed class FastUnionFind
    {
        private int[] parent;
        private int[] counts;
        private Vector3d[] sums;

        public FastUnionFind(int initialCapacity)
        {
            parent = new int[initialCapacity];
            counts = new int[initialCapacity];
            sums = new Vector3d[initialCapacity];
        }

        public void EnsureCapacity(int count)
        {
            if (parent.Length < count)
            {
                int newSize = count * 2; // Or NextPowerOfTwo
                Array.Resize(ref parent, newSize);
                Array.Resize(ref counts, newSize);
                Array.Resize(ref sums, newSize);
            }
        }

        // Force the compiler to paste this inside your loop
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitializeNode(int index)
        {
            parent[index] = index;
            counts[index] = 0;
            sums[index] = Vector3d.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Find(int i)
        {
            int root = i;
            while (root != parent[root])
                root = parent[root];

            int curr = i;
            while (curr != root)
            {
                int nxt = parent[curr];
                parent[curr] = root;
                curr = nxt;
            }
            return root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);

            if (rootI != rootJ)
            {
                parent[rootJ] = rootI;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AggregateData(int index, Vector3d position)
        {
            int root = Find(index);
            counts[root]++;
            sums[root] += position;
        }

        // Getters for the final pass
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetCount(int root) => counts[root];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3d GetSum(int root) => sums[root];
    }
}
