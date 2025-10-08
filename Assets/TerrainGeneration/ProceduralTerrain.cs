using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainGeneration
{
    public partial class ProceduralTerrain : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Dimentions of the terrain in chunks. Will be applied upon regenerating chunks.
        /// </summary>
        [Tooltip("Dimentions of the terrain in chunks. Will be applied upon regenerating chunks.")]
        public int3 Dimentions = new(2, 2, 2);

        /// <summary>
        /// The density value that the icosurface cuts through. The density functions are designed to accomodate the default value 0.
        /// </summary>
        [Range(-10, 10), Tooltip("The density value that the icosurface cuts through. The density functions are designed to accomodate the default value 0.")]
        public float IsoLevel = 0.0f;

        /// <summary>
        /// When enabled, the world is filled in by default and subtractive brushes must be used to carve holes.
        /// </summary>
        [Tooltip("When enabled, the world is filled in by default and subtractive brushes must be used to carve holes.")]
        public bool InvertTerrain = false;

        public Material Material;
        #endregion

        public bool PropertyChanged;

        public bool IsGenerated => m_Chunks != null;

        int3 m_ChunkDimentions;
        Chunk[,,] m_Chunks;

        const int k_ChunkSize = 8;

        /// <summary>
        /// Initialize all chunks, does not apply shapes automatically.
        /// </summary>
        public void Generate()
        {
            m_ChunkDimentions = Dimentions;
            m_Chunks = new Chunk[m_ChunkDimentions.x, m_ChunkDimentions.y, m_ChunkDimentions.z];
            for (int x = 0; x < m_ChunkDimentions.x; x++)
            {
                for (int y = 0; y < m_ChunkDimentions.y; y++)
                {
                    for (int z = 0; z < m_ChunkDimentions.z; z++)
                    {
                        Chunk newChunk = Chunk.New(x, y, z, k_ChunkSize);
                        newChunk.transform.SetParent(transform);
                        newChunk.transform.localPosition = new Vector3(x, y, z) * k_ChunkSize;

                        m_Chunks[x, y, z] = newChunk;
                    }
                }
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
        /// Apply a shape array to all chunks. For total terrain regeneration.
        /// </summary>
        public void Recompute(TerrainShape[] shapeQueue)
        {
            float baseDensity = InvertTerrain ? -32 : 32;

            for (int x = 0; x < m_ChunkDimentions.x; x++)
            {
                for (int y = 0; y < m_ChunkDimentions.y; y++)
                {
                    for (int z = 0; z < m_ChunkDimentions.z; z++)
                    {
                        m_Chunks[x, y, z].DensityMap.FillDensityMap(baseDensity);
                        foreach (TerrainShape shape in shapeQueue)
                            m_Chunks[x, y, z].DensityMap.ApplyShape(shape);
                    }
                }
            }

            UpdateAllChunks();
        }

        /// <summary>
        /// Apply a shape to the terrain at a given position.
        /// </summary>
        public void ApplyShapeAtPosition(TerrainShape shape, Vector3 positionWS)
        {
            ComputeIndices(WorldPositionToIndex(positionWS), out int3 chunkIndex, out _);
            List<int3> updateChunks = new();

            // Apply density functions.
            for (int i = 0; i < 27; i++)
            {
                int3 index = chunkIndex + k_AdjacentChunkIndices[i];
                if (index.x < 0 || index.x > m_ChunkDimentions.x - 1 ||
                    index.y < 0 || index.y > m_ChunkDimentions.y - 1 ||
                    index.z < 0 || index.z > m_ChunkDimentions.z - 1)
                    continue;

                m_Chunks[index.x, index.y, index.z].DensityMap.ApplyShape(shape);
                updateChunks.Add(index);
            }

            // Update meshes.
            foreach (int3 index in updateChunks)
                UpdateChunk(index.x, index.y, index.z);
        }

        void UpdateAllChunks()
        {
            for (int x = 0; x < m_ChunkDimentions.x; x++)
                for (int y = 0; y < m_ChunkDimentions.y; y++)
                    for (int z = 0; z < m_ChunkDimentions.z; z++)
                        UpdateChunk(x, y, z);
        }

        void UpdateChunk(int x, int y, int z)
        {
            Mesh mesh = MakeMesh(this, m_Chunks[x, y, z].DensityMap, new int3(x, y, z) * k_ChunkSize);

            m_Chunks[x, y, z].SetMesh(mesh);
            m_Chunks[x, y, z].SetMaterial(Material);
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
        /// From a world space position, compute the terrain-space index and then break it down into a chunk index and an inter-chunk index.
        /// For debugging.
        /// </summary>
        public void ComputeIndices(Vector3 positionWS, out int3 chunkIndex, out int3 densityIndex)
        {
            ComputeIndices(WorldPositionToIndex(positionWS), out chunkIndex, out densityIndex);
        }

        /// <summary>
        /// Get the chunk at the given index.
        /// For debugging.
        /// </summary>
        public Chunk GetChunk(int3 index)
        {
            return m_Chunks[index.x, index.y, index.z];
        }
#endif

        float SampleDensity(int3 index)
        {
            // TODO: Interpolate between corner samples?

            ComputeIndices(index, out int3 chunkIndex, out int3 densityIndex);
            return m_Chunks[chunkIndex.x, chunkIndex.y, chunkIndex.z].DensityMap.Sample(densityIndex.x, densityIndex.y, densityIndex.z);
        }

        void ComputeIndices(int3 index, out int3 chunkIndex, out int3 densityIndex)
        {
            // Clamp distance to terrain bounds.
            float x = Mathf.Clamp(index.x, 0, (m_ChunkDimentions.x * k_ChunkSize) - 0.001f);
            float y = Mathf.Clamp(index.y, 0, (m_ChunkDimentions.y * k_ChunkSize) - 0.001f);
            float z = Mathf.Clamp(index.z, 0, (m_ChunkDimentions.z * k_ChunkSize) - 0.001f);

            // Compute chunk array index.
            chunkIndex.x = Mathf.FloorToInt(x / k_ChunkSize);
            chunkIndex.y = Mathf.FloorToInt(y / k_ChunkSize);
            chunkIndex.z = Mathf.FloorToInt(z / k_ChunkSize);

            // Compute density map index.
            densityIndex.x = Mathf.FloorToInt(x - (chunkIndex.x * k_ChunkSize));
            densityIndex.y = Mathf.FloorToInt(y - (chunkIndex.y * k_ChunkSize));
            densityIndex.z = Mathf.FloorToInt(z - (chunkIndex.z * k_ChunkSize));
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

            Vector3 size = new Vector3(m_ChunkDimentions.x, m_ChunkDimentions.y, m_ChunkDimentions.z) * k_ChunkSize;
            Gizmos.DrawWireCube(size / 2, size);
        }

        void OnValidate()
        {
            PropertyChanged = true;
        }
#endif
    }

    public struct TerrainShape
    {
        public Matrix4x4 matrix;
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
