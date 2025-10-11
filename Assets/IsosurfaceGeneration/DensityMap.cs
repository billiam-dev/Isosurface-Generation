using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public struct DensityMap
    {
        readonly float[,,] densityArray;

        public int sizeX => densityArray.GetLength(0);
        public int sizeY => densityArray.GetLength(1);
        public int sizeZ => densityArray.GetLength(2);

        public Vector3 sizeInWorld => new(sizeX, sizeY, sizeZ);

        int3 chunkOriginIndex; // The index in the object-space density field where this chunk begins.

        public DensityMap(int3 chunkIndex, int size)
        {
            densityArray = new float[size + 1, size + 1, size + 1];
            chunkOriginIndex = chunkIndex * size;
        }

        /// <summary>
        /// Fill the entire density field with the given value.
        /// </summary>
        public void FillDensityMap(float value)
        {
            for (int x = 0; x < densityArray.GetLength(0); x++)
                for (int y = 0; y < densityArray.GetLength(1); y++)
                    for (int z = 0; z < densityArray.GetLength(2); z++)
                        densityArray[x, y, z] = value;
        }

        /// <summary>
        /// Apply a shape to a given chunk.
        /// </summary>
        public void ApplyShape(Shape shape)
        {
            for (int x = 0; x < densityArray.GetLength(0); x++)
                for (int y = 0; y < densityArray.GetLength(1); y++)
                    for (int z = 0; z < densityArray.GetLength(2); z++)
                        ApplyShape(shape, x, y, z);
        }

        void ApplyShape(Shape shape, int x, int y, int z)
        {
            int3 index = new int3(x, y, z) + chunkOriginIndex;
            Vector3 samplePos = shape.matrix.MultiplyPoint(new Vector3(index.x, index.y, index.z));

            float distance = 0;
            switch (shape.shapeID)
            {
                case ShapeFuncion.Sphere:
                    distance = DistanceFunction.Sphere(samplePos, shape.dimention1);
                    break;
            }

            switch (shape.blendMode)
            {
                case BlendMode.Additive:
                    densityArray[x, y, z] = SmoothMax(-distance, densityArray[x, y, z], shape.sharpness);
                    break;

                case BlendMode.Subtractive:
                    densityArray[x, y, z] = SmoothMin(distance, densityArray[x, y, z], shape.sharpness);
                    break;
            }    
        }

        public float Sample(int x, int y, int z)
        {
            return densityArray[x, y, z];
        }

        public float Sample(int3 index)
        {
            return densityArray[index.x, index.y, index.z];
        }

        public int FlattenIndex(int3 i)
        {
            return (i.z * sizeZ * sizeY) + (i.y * sizeX) + i.x;
        }

        public int3 WrapIndex(int index)
        {
            int z = index / (sizeX * sizeY);
            index -= z * sizeX * sizeY;
            int y = index / sizeX;
            int x = index % sizeX;

            return new int3(x, y, z);
        }

        float SmoothMax(float a, float b, float k)
        {
            return Mathf.Log(Mathf.Exp(k * a) + Mathf.Exp(k * b)) / k;

        }

        float SmoothMin(float a, float b, float k)
        {
            return -SmoothMax(-a, -b, k);
        }
    }
}
