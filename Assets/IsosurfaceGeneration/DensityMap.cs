using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public struct DensityMap
    {
        public readonly float[] density;
        public readonly int pointsPerAxis;
        public readonly int totalPoints => pointsPerAxis * pointsPerAxis * pointsPerAxis;

        // The index cell index where this chunk begins, relative to the whole icosurface.
        // Chunk Index * Chunk Size
        int3 chunkOriginIndex;

        public DensityMap(int3 chunkIndex, int chunkSize)
        {
            pointsPerAxis = chunkSize + 1;
            density = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            chunkOriginIndex = chunkIndex * chunkSize;
        }

        /// <summary>
        /// Fills the entire density field with the given value, use to initialize the field as solid or empty.
        /// </summary>
        public void FillDensityMap(float value)
        {
            for (int i = 0; i < density.Length; i++)
                density[i] = value;
        }

        /// <summary>
        /// Apply a shape to this density map.
        /// </summary>
        public void ApplyShape(Shape shape)
        {
            for (int i = 0; i < density.Length; i++)
            {
                int3 uwrappedIndex = UnwrapCellIndex(i) + chunkOriginIndex;
                Vector3 samplePos = shape.matrix.MultiplyPoint(new Vector3(uwrappedIndex.x, uwrappedIndex.y, uwrappedIndex.z));

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
                        density[i] = SmoothMax(-distance, density[i], shape.sharpness);
                        break;

                    case BlendMode.Subtractive:
                        density[i] = SmoothMin(distance, density[i], shape.sharpness);
                        break;
                }
            }
        }

        public float Sample(int3 index)
        {
            return density[WrapCellIndex(index)];
        }

        public int WrapCellIndex(int3 i)
        {
            return (i.z * pointsPerAxis * pointsPerAxis) + (i.y * pointsPerAxis) + i.x;
        }

        public int3 UnwrapCellIndex(int index)
        {
            int x = index % pointsPerAxis;
            int y = index / pointsPerAxis % pointsPerAxis;
            int z = index / (pointsPerAxis * pointsPerAxis);

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
