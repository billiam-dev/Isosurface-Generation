using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct FillDensityJob : IJobParallelFor
    {
        [ReadOnly] public float value;
        public NativeArray<float> density;

        public void Execute(int index)
        {
            density[index] = value;
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplyShapeJob : IJobParallelFor
    {
        [ReadOnly] public Shape shape;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        public NativeArray<float> density;

        public void Execute(int index)
        {
            int3 uwrappedIndex = IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex;
            float3 samplePos = shape.matrix.MultiplyPoint(new Vector3(uwrappedIndex.x, uwrappedIndex.y, uwrappedIndex.z));

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
                    density[index] = DensityMap.SmoothMax(-distance, density[index], shape.sharpness);
                    break;

                case BlendMode.Subtractive:
                    density[index] = DensityMap.SmoothMin(distance, density[index], shape.sharpness);
                    break;
            }
        }
    }
}
