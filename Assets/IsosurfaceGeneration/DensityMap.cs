using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Build;
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

        const int k_InterloopBatchCount = 128;

        public DensityMap(int3 chunkIndex, int chunkSize, IcosurfaceGenerationMethod generator)
        {
            pointsPerAxis = chunkSize + 1;

            // Since we need access to adjacent cells when computing normals, we expand the density map by 1 on each size.
            // In the single-threaded methods we do not need to do this, since we can just get adjacent chunks from the isosurface,
            // however all data used by JOBS must be blittable, so we cannot have a reference to the isosurface.
            if (generator == IcosurfaceGenerationMethod.MarchingCubesJobs)
                pointsPerAxis += 2;

            density = new NativeArray<float>(pointsPerAxis * pointsPerAxis * pointsPerAxis, Allocator.Persistent);
            chunkOriginIndex = chunkIndex * chunkSize;
        }

        public void Dispose()
        {
            density.Dispose();
        }

        /// <summary>
        /// Fills the entire density field with the given value, use to initialize the field as solid or empty.
        /// </summary>
        public void FillDensityMap(float value, DensityGenerationMethod method)
        {
            switch (method)
            {
                case DensityGenerationMethod.SingleThreaded:
                    FillDensityMap(value);
                    break;

                case DensityGenerationMethod.Jobs:
                    FillDensityMap_Jobs(value);
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

        void FillDensityMap(float value)
        {
            for (int i = 0; i < density.Length; i++)
                density[i] = value;
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

                switch (shape.blendMode)
                {
                    case BlendMode.Additive:
                        density[i] = DistanceFunction.SmoothMax(-distance, density[i], shape.sharpness);
                        break;

                    case BlendMode.Subtractive:
                        density[i] = DistanceFunction.SmoothMin(distance, density[i], shape.sharpness);
                        break;
                }
            }
        }

        void FillDensityMap_Jobs(float value)
        {
            FillDensityJob fillDensityJob = new()
            {
                density = density,
                value = value
            };

            fillDensityJob.Schedule(totalPoints, k_InterloopBatchCount).Complete();
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

        public float Sample(int3 index)
        {
            return density[IndexHelper.Wrap(index, pointsPerAxis)];
        }
    }
}
