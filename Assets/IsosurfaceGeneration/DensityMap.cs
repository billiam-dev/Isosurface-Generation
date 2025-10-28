using IsosurfaceGeneration.Util;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace IsosurfaceGeneration
{
    public struct DensityMap : IDisposable
    {
        public NativeArray<float> density;
        public readonly int pointsPerAxis;
        public readonly int totalPoints => pointsPerAxis * pointsPerAxis * pointsPerAxis;

        // The index cell index where this chunk begins, relative to the whole icosurface.
        // Chunk Index * Chunk Size
        int3 chunkOriginIndex;

        const int k_InterloopBatchCount = 32;

        public DensityMap(int3 chunkIndex, int chunkSize, IcosurfaceGenerationMethod generator)
        {
            pointsPerAxis = chunkSize + 1;

            // Since we need access to adjacent cells when computing normals, we expand the density map by 1 on each size.
            // This is not neccessary in the single-threaded methods since we can just fetch adjacent chunks from the isosurface.
            // However since all data used by JOBS must be blittable, we cannot have a reference to the isosurface so we just recompute the densities on the edge of chunks.
            if (generator == IcosurfaceGenerationMethod.MarchingCubesJobs || generator == IcosurfaceGenerationMethod.SurfaceNetsJobs)
                pointsPerAxis += 2;

            density = new NativeArray<float>(pointsPerAxis * pointsPerAxis * pointsPerAxis, Allocator.Persistent);
            chunkOriginIndex = chunkIndex * chunkSize;
        }

        public void Dispose()
        {
            density.Dispose();
        }

        /// <summary>
        /// Recompute this density map with a shape queue. For total regeneration.
        /// </summary>
        public void RecomputeDensityMap(float initialValue, NativeArray<Shape> shapes, float3 surfacePosition, DensityGenerationMethod method)
        {
            switch (method)
            {
                case DensityGenerationMethod.Recompute:
                    RecomputeDensity(initialValue, shapes, surfacePosition);
                    break;

                case DensityGenerationMethod.RecomputeJobs:
                    RecomputeDensity_Jobs(initialValue, shapes, surfacePosition);
                    break;
            }
        }

        public void FillDensityMap(float value, DensityGenerationMethod method)
        {
            switch (method)
            {
                case DensityGenerationMethod.Recompute:
                    FillDensity(value);
                    break;

                case DensityGenerationMethod.RecomputeJobs:
                    FillDensity_Jobs(value);
                    break;

                case DensityGenerationMethod.UseShapeVolumesJobs:
                    FillDensity_Jobs(value);
                    break;
            }
        }

        /// <summary>
        /// Apply a shape to this density map.
        /// </summary>
        public void ApplyShape(Shape shape, AffineTransform localMatrix, DensityGenerationMethod method)
        {
            switch (method)
            {
                case DensityGenerationMethod.Recompute:
                    ApplyShape(shape, localMatrix);
                    break;

                case DensityGenerationMethod.RecomputeJobs:
                    ApplyShape_Jobs(shape, localMatrix);
                    break;

                case DensityGenerationMethod.UseShapeVolumesJobs:
                    ApplyShape_Jobs(shape, localMatrix);
                    break;
            }
        }

        #region Single Threaded
        void FillDensity(float value)
        {
            for (int i = 0; i < density.Length; i++)
                density[i] = value;
        }

        void RecomputeDensity(float value, NativeArray<Shape> shapeQueue, float3 surfacePosition)
        {
            for (int i = 0; i < density.Length; i++)
            {
                density[i] = value;

                foreach (Shape shape in shapeQueue)
                {
                    AffineTransform localMatrix = shape.matrix;
                    localMatrix.t -= surfacePosition;

                    float3 pos = shape.TransformPosition(IndexHelper.Unwrap(i, pointsPerAxis) + chunkOriginIndex, math.inverse(localMatrix));
                    DensityHelpers.ApplyDistanceFunction(density, i, shape.Distance(pos), shape.sharpness, shape.blendMode == BlendMode.Subtractive);
                }
            }
        }

        void ApplyShape(Shape shape, AffineTransform localMatrix)
        {
            for (int i = 0; i < density.Length; i++)
            {
                float3 pos = shape.TransformPosition(IndexHelper.Unwrap(i, pointsPerAxis) + chunkOriginIndex, localMatrix);
                DensityHelpers.ApplyDistanceFunction(density, i, shape.Distance(pos), shape.sharpness, shape.blendMode == BlendMode.Subtractive);
            }
        }
        #endregion

        #region Jobs
        void FillDensity_Jobs(float value)
        {
            FillDensityJob fillDensityJob = new()
            {
                density = density,
                value = value
            };

            fillDensityJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
        }

        void RecomputeDensity_Jobs(float initialValue, NativeArray<Shape> shapeQueue, float3 surfacePosition)
        {
            RecomputeDensityJob recomputeDensityJob = new()
            {
                density = density,
                pointsPerAxis = pointsPerAxis,
                chunkOriginIndex = chunkOriginIndex,
                initialValue = initialValue,
                shapes = shapeQueue,
                surfacePosition = surfacePosition
            };

            recomputeDensityJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
        }

        void ApplyShape_Jobs(Shape shape, AffineTransform localMatrix)
        {
            switch (shape.shapeID)
            {
                case ShapeFunction.Sphere:
                    ApplyShereJob applyShereJob = new()
                    {
                        density = density,
                        pointsPerAxis = pointsPerAxis,
                        chunkOriginIndex = chunkOriginIndex,
                        shape = shape,
                        localMatrix = localMatrix
                    };
                    applyShereJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
                    break;
                case ShapeFunction.SemiSphere:
                    ApplySemiSphereJob applySemiSphereJob = new()
                    {
                        density = density,
                        pointsPerAxis = pointsPerAxis,
                        chunkOriginIndex = chunkOriginIndex,
                        shape = shape,
                        localMatrix = localMatrix
                    };
                    applySemiSphereJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
                    break;
                case ShapeFunction.Capsule:
                    ApplyCapsuleJob applyCapsuleJob = new()
                    {
                        density = density,
                        pointsPerAxis = pointsPerAxis,
                        chunkOriginIndex = chunkOriginIndex,
                        shape = shape,
                        localMatrix = localMatrix
                    };
                    applyCapsuleJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
                    break;
                case ShapeFunction.Torus:
                    ApplyTorusJob applyTorusJob = new()
                    {
                        density = density,
                        pointsPerAxis = pointsPerAxis,
                        chunkOriginIndex = chunkOriginIndex,
                        shape = shape,
                        localMatrix = localMatrix
                    };
                    applyTorusJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
                    break;
                case ShapeFunction.Cube:
                    ApplyCubeJob applyCubeJob = new()
                    {
                        density = density,
                        pointsPerAxis = pointsPerAxis,
                        chunkOriginIndex = chunkOriginIndex,
                        shape = shape,
                        localMatrix = localMatrix
                    };
                    applyCubeJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
                    break;
            }
        }
        #endregion

        /// <summary>
        /// Sample the density map at a given 3D index.
        /// </summary>
        public float Sample(int3 index)
        {
            return density[IndexHelper.Wrap(index, pointsPerAxis)];
        }
    }

    struct DensityHelpers
    {
        public static void ApplyDistanceFunction(NativeArray<float> density, int index, float distance, float sharpness, bool subtractive)
        {
            // To avoid a branch here, we can use math.select to create a -1 multiplier in subtractive cases.
            // In this case, we want to use a smooth min function, which can be attained by negating the inputs to smooth max, and then negating the result.
            float mult = math.select(1.0f, -1.0f, subtractive);
            density[index] = SmoothMax(-distance, density[index] * mult, sharpness) * mult;
        }

        static float SmoothMax(float a, float b, float k)
        {
            return math.log(math.exp(k * a) + math.exp(k * b)) / k;
        }

        //static float SmoothMin(float a, float b, float k)
        //{
        //    return -SmoothMax(-a, -b, k);
        //}
    }
}
