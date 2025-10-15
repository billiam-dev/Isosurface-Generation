using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public class Isosurface : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Which algorithm to use when constructing the icosurface. Enable profiling to see how long each method takes.
        /// </summary>
        [Tooltip("Which algorithm to use when constructing the icosurface.Enable profiling to see how long each method takes.")]
        public IcosurfaceGenerationMethod MeshingMethod = IcosurfaceGenerationMethod.MarchingCubesJobs;

        /// <summary>
        /// Which method to use when computing the underlying densities. Enable profiling to see how long each method takes.
        /// </summary>
        [Tooltip("Which method to use when computing the underlying densities. Enable profiling to see how long each method takes.")]
        public DensityGenerationMethod DensityMethod = DensityGenerationMethod.Jobs;

        /// <summary>
        /// Dimentions of the isosurface in chunks. Will be applied upon regenerating chunks.
        /// </summary>
        [Tooltip("Dimentions of the isosurface in chunks. Will be applied upon regenerating chunks.")]
        public int3 Dimentions = new(2, 2, 2);

        /// <summary>
        /// How many cells contained on each axis within each chunk.
        /// </summary>
        [Tooltip("How many cells contained on each axis within each chunk.")]
        public ChunkCellDimentions ChunkSize = ChunkCellDimentions.Low8;

        /// <summary>
        /// The density value that the icosurface cuts through. The density functions are designed to accomodate the default value 0.
        /// </summary>
        [Range(-10, 10), Tooltip("The density value that the icosurface cuts through. The density functions are designed to accomodate the default value 0.")]
        public float IsoLevel = 0.0f;

        /// <summary>
        /// When enabled, the world is filled in by default and subtractive brushes must be used to carve holes.
        /// </summary>
        [Tooltip("When enabled, the world is filled in by default and subtractive brushes must be used to carve holes.")]
        public bool InvertSurface = false;

        /// <summary>
        /// Apply a material to the surface.
        /// </summary>
        [Tooltip("Apply a material to the surface.")]
        public Material Material;

        /// <summary>
        /// Enable to see how much time the density and meshing algorithms are taking to compute the surface.
        /// </summary>
        [Tooltip("Enable to see how much time the density and meshing algorithms are taking to compute the surface.")]
        public bool ProfilingEnabled = false;
        #endregion

        public bool PropertyChanged {  get; set; }

        public bool IsGenerated => m_Chunks != null;

        int3 m_Dimentions;
        int m_ChunkSize;
        Chunk[] m_Chunks;

        double m_ProfilingTimestamp;

        /// <summary>
        /// Initialize all chunks, does not apply shapes automatically.
        /// </summary>
        public void Generate()
        {
            m_ChunkSize = ChunkSize switch
            {
                ChunkCellDimentions.Low8 => 8,
                ChunkCellDimentions.Medium16 => 16,
                ChunkCellDimentions.High32 => 32,
                _ => 8
            };

            m_Dimentions = Dimentions;
            m_Chunks = new Chunk[m_Dimentions.x * m_Dimentions.y * m_Dimentions.z];

            for (int i = 0; i < m_Chunks.Length; i++)
            {
                int3 chunkIndex = IndexHelper.Unwrap(i, m_Dimentions);

                Chunk newChunk = Chunk.New(chunkIndex, m_ChunkSize, MeshingMethod);
                newChunk.transform.SetParent(transform);
                newChunk.transform.localPosition = new Vector3(chunkIndex.x, chunkIndex.y, chunkIndex.z) * m_ChunkSize;

                m_Chunks[i] = newChunk;
            }
        }

        /// <summary>
        /// Destroy all chunks.
        /// </summary>
        public void Destroy()
        {
            foreach (Chunk chunk in GetComponentsInChildren<Chunk>())
                chunk.Destroy();

            m_Chunks = null;
        }

        /// <summary>
        /// Apply a shape array to all chunks. For total surface regeneration.
        /// </summary>
        public void Recompute(Shape[] shapeQueue)
        {
            m_ProfilingTimestamp = Time.realtimeSinceStartupAsDouble;

            float baseDensity = InvertSurface ? 32 : -32;

            for (int i = 0; i < m_Chunks.Length; i++)
            {
                m_Chunks[i].DensityMap.FillDensityMap(baseDensity, DensityMethod);
                foreach (Shape shape in shapeQueue)
                    m_Chunks[i].DensityMap.ApplyShape(shape, DensityMethod);
            }

            if (ProfilingEnabled)
            {
                double time = Time.realtimeSinceStartupAsDouble - m_ProfilingTimestamp;
                Debug.Log($"Recomputed densities of {m_Chunks.Length} chunks in {time} seconds.");
            }

            UpdateAllChunks();
        }

        /// <summary>
        /// Apply a shape at the given position.
        /// </summary>
        public void ApplyShapeAtPosition(Shape shape, Vector3 positionWS)
        {
            m_ProfilingTimestamp = Time.realtimeSinceStartupAsDouble;

            ComputeIndices(WorldPositionToIndex(positionWS), out int3 chunkIndex, out _);
            List<int> updateChunks = new();

            // Apply density functions.
            for (int i = 0; i < 27; i++)
            {
                int3 wrappedIndex = chunkIndex + k_AdjacentChunkIndices[i];
                if (wrappedIndex.x < 0 || wrappedIndex.x > m_Dimentions.x - 1 ||
                    wrappedIndex.y < 0 || wrappedIndex.y > m_Dimentions.y - 1 ||
                    wrappedIndex.z < 0 || wrappedIndex.z > m_Dimentions.z - 1)
                    continue;

                int index = IndexHelper.Wrap(wrappedIndex, m_Dimentions);
                m_Chunks[index].DensityMap.ApplyShape(shape, DensityMethod);
                updateChunks.Add(index);
            }

            if (ProfilingEnabled)
            {
                double time = Time.realtimeSinceStartupAsDouble - m_ProfilingTimestamp;
                Debug.Log($"Recomputed densities of {updateChunks.Count} chunks in {time} seconds.");
            }

            // Update meshes.
            foreach (int index in updateChunks)
                UpdateChunk(index);
        }

        void UpdateAllChunks()
        {
            m_ProfilingTimestamp = Time.realtimeSinceStartupAsDouble;

            for (int i = 0; i < m_Chunks.Length; i++)
                UpdateChunk(i);

            if (ProfilingEnabled)
            {
                double time = Time.realtimeSinceStartupAsDouble - m_ProfilingTimestamp;
                Debug.Log($"Regenerated {m_Chunks.Length} chunk meshes in {time} seconds.");
            }
        }

        void UpdateChunk(int index)
        {
            DensityMap densityMap = m_Chunks[index].DensityMap;
            int3 chunkOriginIndex = IndexHelper.Unwrap(index, m_Dimentions) * m_ChunkSize;
            
            switch (MeshingMethod)
            {
                case IcosurfaceGenerationMethod.MarchingCubes:
                    GenerateMesh_MarchingCubes(index, densityMap, chunkOriginIndex);
                    break;

                case IcosurfaceGenerationMethod.MarchingCubesJobs:
                    GenerateMesh_MarchingCubesJobs(index, densityMap, chunkOriginIndex);
                    break;

                case IcosurfaceGenerationMethod.SurfaceNets:
                    GenerateMesh_MarchingSurfaceNets(index, densityMap, chunkOriginIndex);
                    break;
            };

            m_Chunks[index].SetMaterial(Material);
        }

        void GenerateMesh_MarchingCubes(int index, DensityMap densityMap, int3 chunkOriginIndex)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<int> triangles = new();

            MarchingCubesMesher mesher = new()
            {
                surface = this,
                densityMap = densityMap,
                chunkOriginIndex = chunkOriginIndex,
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };

            mesher.Execute();
            m_Chunks[index].SetMesh(vertices, normals, triangles);
        }

        readonly ProfilerMarker ProfileMarker = new("Marching Cubes Job");

        void GenerateMesh_MarchingCubesJobs(int index, DensityMap densityMap, int3 chunkOriginIndex)
        {
            NativeList<Vertex> vertices = new(100, Allocator.Persistent);
            NativeList<ushort> indices = new(100, Allocator.Persistent);
            NativeCounter vertexCounter = new(Allocator.Persistent);
            NativeHashMap<int2, ushort> vertexIndexMap = new (100, Allocator.Persistent);

            int shortenedPPA = densityMap.pointsPerAxis - 3;
            int numPointsToItterate = shortenedPPA * shortenedPPA * shortenedPPA;

            MarchingCubesMesherJob mesherJob = new()
            {
                marker = ProfileMarker,
                density = densityMap.density,
                densityPPA = densityMap.pointsPerAxis,
                itteratePPA = shortenedPPA,
                isoLevel = IsoLevel,
                vertices = vertices,
                indices = indices,
                vertexCounter = vertexCounter,
                vertexIndexMap = vertexIndexMap
            };

            ProfileMarker.Begin();

            // Because the density map for each chunk extends beyond the bounds of the marching cubes space, we do not need to loop through all of the voxels.
            // So rather than having a lengthy returns statement for voxels that are out-of-bounds, we only itterate over the space required by using an index wrapping function.            
            JobHandle jobHandle = mesherJob.Schedule(numPointsToItterate, default);
            jobHandle.Complete();
            
            ProfileMarker.End();

            m_Chunks[index].SetMesh(mesherJob);

            vertices.Dispose();
            indices.Dispose();
            vertexCounter.Dispose();
            vertexIndexMap.Dispose();
        }

        void GenerateMesh_MarchingSurfaceNets(int index, DensityMap densityMap, int3 chunkOriginIndex)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<int> triangles = new();

            SurfaceNetsMesher mesher = new()
            {
                surface = this,
                densityMap = densityMap,
                chunkOriginIndex = chunkOriginIndex,
                vertices = vertices,
                normals = normals,
                triangles = triangles
            };

            mesher.Execute();
            m_Chunks[index].SetMesh(vertices, normals, triangles);
        }

        /// <summary>
        /// Sample the density at a given world space pos.
        /// </summary>
        public float SampleDensity(Vector3 positionWS)
        {
            return SampleDensity(WorldPositionToIndex(positionWS));
        }

#if UNITY_EDITOR
        /// <summary>
        /// From a world space position, compute the object-space wrappedIndex and then break it down into a chunk wrappedIndex and a cell wrappedIndex within that chunk.
        /// For debugging.
        /// </summary>
        public void ComputeIndices(Vector3 positionWS, out int3 chunkIndex, out int3 densityIndex)
        {
            ComputeIndices(WorldPositionToIndex(positionWS), out chunkIndex, out densityIndex);
        }

        /// <summary>
        /// Get the chunk at the given wrappedIndex.
        /// For debugging.
        /// </summary>
        public Chunk GetChunk(int3 chunkIndex)
        {
            return m_Chunks[IndexHelper.Wrap(chunkIndex, m_Dimentions)];
        }
#endif

        public float SampleDensity(int3 index)
        {
            // TODO: Interpolate between corner samples?

            ComputeIndices(index, out int3 chunkIndex, out int3 cellIndex);
            return m_Chunks[IndexHelper.Wrap(chunkIndex, m_Dimentions)].DensityMap.Sample(cellIndex);
        }

        void ComputeIndices(int3 index, out int3 chunkIndex, out int3 cellIndex)
        {
            // Clamp distance to surface bounds.
            float x = Mathf.Clamp(index.x, 0, (m_Dimentions.x * m_ChunkSize) - 0.001f);
            float y = Mathf.Clamp(index.y, 0, (m_Dimentions.y * m_ChunkSize) - 0.001f);
            float z = Mathf.Clamp(index.z, 0, (m_Dimentions.z * m_ChunkSize) - 0.001f);

            // Compute chunk array wrappedIndex.
            chunkIndex.x = Mathf.FloorToInt(x / m_ChunkSize);
            chunkIndex.y = Mathf.FloorToInt(y / m_ChunkSize);
            chunkIndex.z = Mathf.FloorToInt(z / m_ChunkSize);

            // Compute density map wrappedIndex.
            cellIndex.x = Mathf.FloorToInt(x - (chunkIndex.x * m_ChunkSize));
            cellIndex.y = Mathf.FloorToInt(y - (chunkIndex.y * m_ChunkSize));
            cellIndex.z = Mathf.FloorToInt(z - (chunkIndex.z * m_ChunkSize));
        }

        int3 WorldPositionToIndex(Vector3 positionWS)
        {
            Vector3 positionLS = transform.InverseTransformPoint(positionWS);
            int x = Mathf.FloorToInt(positionLS.x);
            int y = Mathf.FloorToInt(positionLS.y);
            int z = Mathf.FloorToInt(positionLS.z);

            return new int3(x, y, z);
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (m_Chunks == null)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 1, 1, 0.5f);

            Vector3 size = new Vector3(m_Dimentions.x, m_Dimentions.y, m_Dimentions.z) * m_ChunkSize;
            Gizmos.DrawWireCube(size / 2, size);
        }

        void OnValidate()
        {
            PropertyChanged = true;
        }
#endif

        readonly int3[] k_AdjacentChunkIndices = new int3[27]
        {
            new(-1, -1, -1),
            new(-1, -1, 0),
            new(-1, -1, 1),
            new(-1, 0, -1),
            new(-1, 0, 0),
            new(-1, 0, 1),
            new(-1, 1, -1),
            new(-1, 1, 0),
            new(-1, 1, 1),
            new(0, -1, -1),
            new(0, -1, 0),
            new(0, -1, 1),
            new(0, 0, -1),
            new(0, 0, 0),
            new(0, 0, 1),
            new(0, 1, -1),
            new(0, 1, 0),
            new(0, 1, 1),
            new(1, -1, -1),
            new(1, -1, 0),
            new(1, -1, 1),
            new(1, 0, -1),
            new(1, 0, 0),
            new(1, 0, 1),
            new(1, 1, -1),
            new(1, 1, 0),
            new(1, 1, 1)
        };
    }

    public enum IcosurfaceGenerationMethod
    {
        MarchingCubes,
        MarchingCubesJobs,
        SurfaceNets
    }

    public enum DensityGenerationMethod
    {
        SingleThreaded,
        Jobs
    }

    public enum ChunkCellDimentions
    {
        Low8,
        Medium16,
        High32
    }

    public struct Shape
    {
        public Matrix4x4 matrix; // TODO: float4x4
        public ShapeFuncion shapeID;
        public BlendMode blendMode;
        public float sharpness;
        public float dimention1;
        public float dimention2;
    }

    public enum ShapeFuncion
    {
        Sphere
    }

    public enum BlendMode
    {
        Additive,
        Subtractive
    }
}
