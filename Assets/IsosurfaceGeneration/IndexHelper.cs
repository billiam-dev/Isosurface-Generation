using Unity.Mathematics;

namespace IsosurfaceGeneration
{
    public static class IndexHelper
    {
        public static int Wrap(int3 index, int3 size)
        {
            return (index.z * size.x * size.y) + (index.y * size.x) + index.x;
        }

        public static int3 Unwrap(int index, int3 size)
        {
            int x = index % size.x;
            int y = index / size.x % size.y;
            int z = index / (size.x * size.y);

            return new int3(x, y, z);
        }

        public static int Wrap(int3 index, int size)
        {
            return (index.z * size * size) + (index.y * size) + index.x;
        }

        public static int3 Unwrap(int index, int size)
        {
            int x = index % size;
            int y = index / size % size;
            int z = index / (size * size);

            return new int3(x, y, z);
        }
    }
}
