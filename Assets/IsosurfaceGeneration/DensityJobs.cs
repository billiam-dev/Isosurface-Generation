using IsosurfaceGeneration.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace IsosurfaceGeneration
{
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct FillDensityJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public float value;

        public void Execute(int index)
        {
            density[index] = value;
        }
    }


    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct RecomputeDensityJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        [ReadOnly] public float initialValue;
        [ReadOnly] public NativeArray<Shape> shapes;

        public void Execute(int index)
        {
            density[index] = initialValue;

            for (int i = 0; i < shapes.Length; i++)
            {
                float4 pos = new(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex, 1);
                pos = math.mul(shapes[i].matrix, pos);

                float distance = 0;
                switch (shapes[i].shapeID)
                {
                    case ShapeFuncion.Sphere:
                        distance = DistanceFunction.Sphere(new float3(pos.x, pos.y, pos.z), shapes[i].dimention1);
                        break;
                }

                DensityHelpers.ApplyDistanceFunction(density, index, distance, shapes[i].sharpness, shapes[i].blendMode == BlendMode.Subtractive);
            }
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplyShereJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        [ReadOnly] public AffineTransform matrix;
        [ReadOnly] public float sharpness;
        [ReadOnly] public bool subtractive;

        [ReadOnly] public float radius;

        public void Execute(int index)
        {
            float4 pos = new(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex, 1);
            pos = math.mul(matrix, pos);
            float distance = DistanceFunction.Sphere(new float3(pos.x, pos.y, pos.z), radius);
            DensityHelpers.ApplyDistanceFunction(density, index, distance, sharpness, subtractive);
        }
    }
}
