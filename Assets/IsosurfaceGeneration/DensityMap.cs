using IsosurfaceGeneration.Util;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public struct DensityMap : IDisposable
    {
        public NativeArray<float> density; // TODO: convert to sbyte array.
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
        public void RecomputeDensityMap(float initialValue, NativeArray<Shape> shapes, DensityGenerationMethod method)
        {
            switch (method)
            {
                case DensityGenerationMethod.SingleThreaded:
                    RecomputeDensity(initialValue, shapes);
                    break;

                case DensityGenerationMethod.Jobs:
                    RecomputeDensity_Jobs(initialValue, shapes);
                    break;
            }
        }

        /// <summary>
        /// Apply a shape to this density map.
        /// </summary>
        public void ApplyShape(Shape shape, DensityGenerationMethod method)
        {
            switch (method)
            {
                case DensityGenerationMethod.SingleThreaded:
                    ApplyShape(shape);
                    break;

                case DensityGenerationMethod.Jobs:
                    ApplyShape_Jobs(shape);
                    break;
            }
        }

        #region Single Threaded
        void RecomputeDensity(float value, NativeArray<Shape> shapeQueue)
        {
            for (int i = 0; i < density.Length; i++)
            {
                density[i] = value;

                foreach (Shape shape in shapeQueue)
                {
                    int3 uwrappedIndex = IndexHelper.Unwrap(i, pointsPerAxis) + chunkOriginIndex;
                    Vector3 samplePos = shape.matrix.MultiplyPoint(new Vector3(uwrappedIndex.x, uwrappedIndex.y, uwrappedIndex.z));

                    float distance = 0;
                    switch (shape.shapeID)
                    {
                        case ShapeFuncion.Sphere:
                            distance = DistanceFunction.Sphere(samplePos, shape.dimention1);
                            break;
                    }

                    float mult = shape.blendMode == BlendMode.Subtractive ? -1.0f : 1.0f;
                    density[i] = DistanceFunction.SmoothMax(-distance, density[i] * mult, shape.sharpness) * mult;
                }
            }
        }

        void ApplyShape(Shape shape)
        {
            for (int i = 0; i < density.Length; i++)
            {
                int3 uwrappedIndex = IndexHelper.Unwrap(i, pointsPerAxis) + chunkOriginIndex;
                Vector3 samplePos = shape.matrix.MultiplyPoint(new Vector3(uwrappedIndex.x, uwrappedIndex.y, uwrappedIndex.z));

                float distance = 0;
                switch (shape.shapeID)
                {
                    case ShapeFuncion.Sphere:
                        distance = DistanceFunction.Sphere(samplePos, shape.dimention1);
                        break;
                }

                float mult = shape.blendMode == BlendMode.Subtractive ? -1.0f : 1.0f;
                density[i] = DistanceFunction.SmoothMax(-distance, density[i] * mult, shape.sharpness) * mult;
            }
        }
        #endregion

        #region Jobs
        void RecomputeDensity_Jobs(float value, NativeArray<Shape> shapeQueue)
        {
            RecomputeDensityJob recomputeDensityJob = new()
            {
                density = density,
                initValue = value,
                shapes = shapeQueue,
                pointsPerAxis = pointsPerAxis,
                chunkOriginIndex = chunkOriginIndex
            };

            recomputeDensityJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
        }

        void ApplyShape_Jobs(Shape shape)
        {
            bool subtractive = shape.blendMode == BlendMode.Subtractive;

            switch (shape.shapeID)
            {
                case ShapeFuncion.Sphere:
                    ApplyShereJob applyShapeJob = new()
                    {
                        density = density,
                        matrix = shape.matrix,
                        radius = shape.dimention1,
                        sharpness = shape.sharpness,
                        subtractive = subtractive,
                        pointsPerAxis = pointsPerAxis,
                        chunkOriginIndex = chunkOriginIndex,
                    };

                    applyShapeJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
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
}
