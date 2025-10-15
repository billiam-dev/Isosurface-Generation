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
        [NativeDisableParallelForRestriction] public NativeArray<float> density;

        public void Execute(int index)
        {
            density[index] = value;
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct ApplyShereJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<float> density;

        [ReadOnly] public Matrix4x4 matrix;
        [ReadOnly] public float radius;
        [ReadOnly] public float sharpness;
        [ReadOnly] public bool subtractive;

        [ReadOnly] public int pointsPerAxis;
        [ReadOnly] public int3 chunkOriginIndex;

        public void Execute(int index)
        {
            int3 uwrappedIndex = IndexHelper.Unwrap(index, pointsPerAxis) + chunkOriginIndex;
            float3 samplePos = matrix.MultiplyPoint(new Vector3(uwrappedIndex.x, uwrappedIndex.y, uwrappedIndex.z)); // TODO: custom fload3x4 -> float4x4 mult

            float distance = DistanceFunction.Sphere(samplePos, radius);

            // To avoid a branch here, we can use math.select to create a -1 multiplier in subtractive cases.
            // In this case, we want to use a smooth min function, which can be attained by negating the inputs to smooth max, and then negating the result.
            float mult = math.select(1.0f, -1.0f, subtractive);
            density[index] = DistanceFunction.SmoothMax(-distance, density[index] * mult, sharpness) * mult;
        }
    }
}
