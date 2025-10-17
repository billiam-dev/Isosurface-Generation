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
                float3 pos = shapes[i].TransformPosition(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex);
                DensityHelpers.ApplyDistanceFunction(density, index, shapes[i].Distance(pos), shapes[i].sharpness, shapes[i].blendMode == BlendMode.Subtractive);
            }
        }
    }

    // Individual shape jobs, much more performant to perform the switch outside of the job.
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplyShereJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        [ReadOnly] public Shape shape;

        public void Execute(int index)
        {
            float3 pos = shape.TransformPosition(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex);
            float distance = DistanceFunction.Sphere(pos, shape.dimention1);
            DensityHelpers.ApplyDistanceFunction(density, index, distance, shape.sharpness, shape.blendMode == BlendMode.Subtractive);
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplySemiSphereJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        [ReadOnly] public Shape shape;

        public void Execute(int index)
        {
            float3 pos = shape.TransformPosition(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex);
            float distance = DistanceFunction.SemiSphere(pos, shape.dimention1, shape.dimention2);
            DensityHelpers.ApplyDistanceFunction(density, index, distance, shape.sharpness, shape.blendMode == BlendMode.Subtractive);
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplyCapsuleJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        [ReadOnly] public Shape shape;

        public void Execute(int index)
        {
            float3 pos = shape.TransformPosition(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex);
            float distance = DistanceFunction.Capsule(pos, shape.dimention1, shape.dimention2);
            DensityHelpers.ApplyDistanceFunction(density, index, distance, shape.sharpness, shape.blendMode == BlendMode.Subtractive);
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplyTorusJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;
        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        [ReadOnly] public Shape shape;

        public void Execute(int index)
        {
            float3 pos = shape.TransformPosition(IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex);
            float distance = DistanceFunction.Torus(pos, shape.dimention1, shape.dimention2);
            DensityHelpers.ApplyDistanceFunction(density, index, distance, shape.sharpness, shape.blendMode == BlendMode.Subtractive);
        }
    }
}
