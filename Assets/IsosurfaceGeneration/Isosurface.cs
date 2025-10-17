using IsosurfaceGeneration.Meshing;
using IsosurfaceGeneration.Util;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
        public DensityGenerationMethod DensityMethod = DensityGenerationMethod.RecomputeJobs;

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
        public bool SendLogMessages = false;
        #endregion

        public bool PropertyChanged {  get; set; }

        public bool IsGenerated => enabled && m_Chunks != null;

        public int ChunkSizeCells => ChunkSize switch
        {
            ChunkCellDimentions.Low8 => 8,
            ChunkCellDimentions.Medium16 => 16,
            ChunkCellDimentions.High32 => 32,
            _ => 8
        };

        int3 m_Dimentions;
        int m_ChunkSize;
        Chunk[] m_Chunks;

        double m_DensityTimestamp;
        double m_MeshTimestamp;

        /// <summary>
        /// Initialize all chunks, does not apply shapes automatically.
        /// </summary>
        public void Generate()
        {
            m_ChunkSize = ChunkSizeCells;

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
        public void Recompute(NativeArray<Shape> shapeQueue)
        {
            BeginProfiler(ref m_DensityTimestamp);
            float baseDensity = InvertSurface ? 32.0f : -32.0f;

            if (DensityMethod == DensityGenerationMethod.Recompute || DensityMethod == DensityGenerationMethod.RecomputeJobs)
            {
                // Loop through all chunks, set initial value and then apply all shapes.
                for (int i = 0; i < m_Chunks.Length; i++)
                    m_Chunks[i].DensityMap.RecomputeDensityMap(baseDensity, shapeQueue, DensityMethod);

                EndProfilier(ref m_DensityTimestamp);

                // Update effected chunks
                m_MeshTimestamp = Time.realtimeSinceStartupAsDouble;
                for (int i = 0; i < m_Chunks.Length; i++)
                    UpdateChunk(i);
                m_MeshTimestamp = Time.realtimeSinceStartupAsDouble - m_MeshTimestamp;

                if (SendLogMessages)
                    LogTimestamps(m_Chunks.Length);
            }
            else
            {
                List<int> updateChunks = new();

                // First fill entire map with value.
                for (int i = 0; i < m_Chunks.Length; i++)
                    m_Chunks[i].DensityMap.FillDensityMap(baseDensity, DensityMethod);

                // Then loop through shapes and apply based on bounding volume.
                for (int i = 0; i < shapeQueue.Length; i++)
                {
                    ApplyShapeWithBoundingVolume(shapeQueue[i], out List<int> effectedChunks); // TODO: output effected chunk mask? bitwise || together and only update those chunks.

                    for (int j = 0; j < effectedChunks.Count; j++)
                        if (!updateChunks.Contains(effectedChunks[j]))
                            updateChunks.Add(effectedChunks[j]);
                }

                EndProfilier(ref m_DensityTimestamp);

                // Update effected chunks
                BeginProfiler(ref m_MeshTimestamp);
                foreach (int index in updateChunks)
                    UpdateChunk(index);
                EndProfilier(ref m_MeshTimestamp);

                if (SendLogMessages)
                    LogTimestamps(updateChunks.Count);
            }
        }

        /// <summary>
        /// Apply a shape at the given position.
        /// </summary>
        public void ApplyShape(Shape shape)
        {
            BeginProfiler(ref m_DensityTimestamp);
            ApplyShapeWithBoundingVolume(shape, out List<int> effectedChunks);
            EndProfilier(ref m_DensityTimestamp);

            BeginProfiler(ref m_MeshTimestamp);
            foreach (int index in effectedChunks)
                UpdateChunk(index);
            EndProfilier(ref m_MeshTimestamp);

            if (SendLogMessages)
                LogTimestamps(effectedChunks.Count);
        }

        void ApplyShapeWithBoundingVolume(Shape shape, out List<int> effectedChunks)
        {            
            ComputeIndices(PositionToIndex(shape.inverseMatrix.t), out int3 chunkIndex, out _);

            effectedChunks = new();
            int3 chunkVolume = shape.ComputeChunkVolume(this);
            
            for (int x = 0; x < chunkVolume.x; x++)
            {
                for (int y = 0; y < chunkVolume.y; y++)
                {
                    for (int z = 0; z < chunkVolume.z; z++)
                    {
                        int3 wrappedIndex = new(x, y, z);
                        wrappedIndex -= chunkVolume / 2;
                        wrappedIndex += chunkIndex;

                        if (!ChunkInBounds(wrappedIndex))
                            continue;

                        int index = IndexHelper.Wrap(wrappedIndex, m_Dimentions);
                        m_Chunks[index].DensityMap.ApplyShape(shape, DensityMethod);
                        effectedChunks.Add(index);
                    }
                }
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
                    GenerateMesh_MarchingCubesJobs(index, densityMap);
                    break;

                case IcosurfaceGenerationMethod.SurfaceNets:
                    GenerateMesh_SurfaceNets(index, densityMap, chunkOriginIndex);
                    break;

                case IcosurfaceGenerationMethod.SurfaceNetsJobs:
                    GenerateMesh_SurfaceNetsJobs(index, densityMap);
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

        void GenerateMesh_MarchingCubesJobs(int index, DensityMap densityMap)
        {
            NativeList<Vertex> vertices = new(100, Allocator.Persistent);
            NativeList<ushort> indices = new(100, Allocator.Persistent);
            NativeCounter vertexCounter = new(Allocator.Persistent);
            NativeHashMap<int2, ushort> vertexIndexMap = new (100, Allocator.Persistent);

            int shortenedPPA = densityMap.pointsPerAxis - 3;
            int numPointsToItterate = shortenedPPA * shortenedPPA * shortenedPPA;

            MarchingCubesMesherJob mesherJob = new()
            {
                density = densityMap.density,
                densityPPA = densityMap.pointsPerAxis,
                itteratePPA = shortenedPPA,
                isoLevel = IsoLevel,
                vertices = vertices,
                indices = indices,
                vertexCounter = vertexCounter,
                vertexIndexMap = vertexIndexMap
            };

            // Because the density map for each chunk extends beyond the bounds of the marching cubes space, we do not need to loop through all of the voxels.
            // So rather than having a lengthy returns statement for voxels that are out-of-bounds, we only itterate over the space required by using an index wrapping function.            
            JobHandle jobHandle = mesherJob.Schedule(numPointsToItterate, default);
            jobHandle.Complete();
            
            m_Chunks[index].SetMesh(mesherJob);

            vertices.Dispose();
            indices.Dispose();
            vertexCounter.Dispose();
            vertexIndexMap.Dispose();
        }

        void GenerateMesh_SurfaceNets(int index, DensityMap densityMap, int3 chunkOriginIndex)
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

        void GenerateMesh_SurfaceNetsJobs(int index, DensityMap densityMap)
        {
            NativeList<Vertex> vertices = new(100, Allocator.Persistent);
            NativeList<ushort> indices = new(100, Allocator.Persistent);
            NativeCounter vertexCounter = new(Allocator.Persistent);
            NativeHashMap<int3, ushort> vertexIndexMap = new(100, Allocator.Persistent);

            int shortenedPPA = densityMap.pointsPerAxis - 3;
            int numPointsToItterate = shortenedPPA * shortenedPPA * shortenedPPA;

            SurfaceNetsMesherJob mesherJob = new()
            {
                density = densityMap.density,
                densityPPA = densityMap.pointsPerAxis,
                itteratePPA = shortenedPPA,
                isoLevel = IsoLevel,
                vertices = vertices,
                indices = indices,
                vertexCounter = vertexCounter,
                vertexIndexMap = vertexIndexMap
            };

            // Because the density map for each chunk extends beyond the bounds of the marching cubes space, we do not need to loop through all of the voxels.
            // So rather than having a lengthy returns statement for voxels that are out-of-bounds, we only itterate over the space required by using an index wrapping function.            
            JobHandle jobHandle = mesherJob.Schedule(numPointsToItterate, default);
            jobHandle.Complete();

            m_Chunks[index].SetMesh(mesherJob);

            vertices.Dispose();
            indices.Dispose();
            vertexCounter.Dispose();
            vertexIndexMap.Dispose();
        }

        /// <summary>
        /// Sample the density at a given world space pos.
        /// </summary>
        public float SampleDensity(Vector3 positionWS)
        {
            return SampleDensity(PositionToIndex(positionWS));
        }

#if UNITY_EDITOR
        /// <summary>
        /// From a world space position, compute the object-space index and then break it down into a chunk index and a cell index within that chunk.
        /// For debugging.
        /// </summary>
        public void ComputeIndices(Vector3 positionWS, out int3 chunkIndex, out int3 densityIndex)
        {
            ComputeIndices(PositionToIndex(positionWS), out chunkIndex, out densityIndex);
        }

        /// <summary>
        /// Get the chunk at the given index.
        /// For debugging.
        /// </summary>
        public Chunk GetChunk(int3 chunkIndex)
        {
            return m_Chunks[IndexHelper.Wrap(chunkIndex, m_Dimentions)];
        }
#endif

        public float SampleDensity(int3 index)
        {
            ComputeIndices(index, out int3 chunkIndex, out int3 cellIndex);
            return m_Chunks[IndexHelper.Wrap(chunkIndex, m_Dimentions)].DensityMap.Sample(cellIndex);
        }

        void ComputeIndices(int3 index, out int3 chunkIndex, out int3 cellIndex)
        {
            // Clamp distance to surface bounds.
            float x = Mathf.Clamp(index.x, 0, (m_Dimentions.x * m_ChunkSize) - 0.001f);
            float y = Mathf.Clamp(index.y, 0, (m_Dimentions.y * m_ChunkSize) - 0.001f);
            float z = Mathf.Clamp(index.z, 0, (m_Dimentions.z * m_ChunkSize) - 0.001f);

            // Compute chunk array index.
            chunkIndex.x = Mathf.FloorToInt(x / m_ChunkSize);
            chunkIndex.y = Mathf.FloorToInt(y / m_ChunkSize);
            chunkIndex.z = Mathf.FloorToInt(z / m_ChunkSize);

            // Compute density map index.
            cellIndex.x = Mathf.FloorToInt(x - (chunkIndex.x * m_ChunkSize));
            cellIndex.y = Mathf.FloorToInt(y - (chunkIndex.y * m_ChunkSize));
            cellIndex.z = Mathf.FloorToInt(z - (chunkIndex.z * m_ChunkSize));
        }

        int3 PositionToIndex(Vector3 positionWS)
        {
            int x = Mathf.FloorToInt(positionWS.x);
            int y = Mathf.FloorToInt(positionWS.y);
            int z = Mathf.FloorToInt(positionWS.z);

            return new int3(x, y, z);
        }

        bool ChunkInBounds(int3 index)
        {
            return index.x >= 0 && index.x < m_Dimentions.x &&
                   index.y >= 0 && index.y < m_Dimentions.y &&
                   index.z >= 0 && index.z < m_Dimentions.z;
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

        const float k_Width = 100;
        const float k_Height = 24;
        const float k_Margin = 20;
        Rect m_TextAreaRect;
        GUIStyle m_Style;

        void OnGUI()
        {
            if (m_TextAreaRect == null)
                m_TextAreaRect = new(k_Margin, k_Margin, k_Width, k_Height);

            if (m_Style == null)
                m_Style = new GUIStyle()
                {
                    normal = new GUIStyleState()
                    {
                        textColor = Color.white,
                    },
                    fontSize = 24
                };

            GUI.enabled = false;
            GUI.TextField(m_TextAreaRect, $"\n{ToMilliseconds(m_DensityTimestamp + m_MeshTimestamp)}\nd: {ToMilliseconds(m_DensityTimestamp)}\nm: {ToMilliseconds(m_MeshTimestamp)}", m_Style);
            GUI.enabled = true;
        }

        void BeginProfiler(ref double timestamp)
        {
            timestamp = Time.realtimeSinceStartupAsDouble;
        }

        void EndProfilier(ref double timestamp)
        {
            timestamp = Time.realtimeSinceStartupAsDouble - timestamp;
        }

        void LogTimestamps(int numChunks)
        {
            Debug.Log($"Regenerated {numChunks} chunks in {ToMilliseconds(m_DensityTimestamp + m_MeshTimestamp)} milliseconds. (d: {ToMilliseconds(m_DensityTimestamp)}, m: {ToMilliseconds(m_MeshTimestamp)})");
        }

        double ToMilliseconds(double time)
        {
            double timeMiliseconds = time * 1000.0;

            timeMiliseconds *= 100.0;
            timeMiliseconds = Math.Round(timeMiliseconds);
            timeMiliseconds /= 100.0;

            return timeMiliseconds;
        }
    }

    public enum IcosurfaceGenerationMethod
    {
        MarchingCubes,
        MarchingCubesJobs,
        SurfaceNets,
        SurfaceNetsJobs
    }

    public enum DensityGenerationMethod
    {
        Recompute,
        RecomputeJobs,
        UseShapeVolumesJobs
    }

    public enum ChunkCellDimentions
    {
        Low8,
        Medium16,
        High32
    }
}
