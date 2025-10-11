using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public class Isosurface : MonoBehaviour
    {
        #region Properties
        public enum IcosurfaceGenerationMethod
        {
            MarchingCubes,
            SurfaceNets
        }

        /// <summary>
        /// Which algorithm to use when constructing the icosurface. For performance comparison.
        /// </summary>
        [Tooltip("Which algorithm to use when constructing the icosurface. For performance comparison.")]
        public IcosurfaceGenerationMethod MeshingMethod;

        /// <summary>
        /// Dimentions of the isosurface in chunks. Will be applied upon regenerating chunks.
        /// </summary>
        [Tooltip("Dimentions of the isosurface in chunks. Will be applied upon regenerating chunks.")]
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
        public bool InvertSurface = false;

        public Material Material;
        #endregion

        public bool PropertyChanged;

        public bool IsGenerated => m_Chunks != null;

        int3 m_ChunkDimentions;
        Chunk[] m_Chunks;

        const int k_ChunkSize = 8;

        /// <summary>
        /// Initialize all chunks, does not apply shapes automatically.
        /// </summary>
        public void Generate()
        {
            m_ChunkDimentions = Dimentions;
            m_Chunks = new Chunk[m_ChunkDimentions.x * m_ChunkDimentions.y * m_ChunkDimentions.z];
            for (int i = 0; i < m_Chunks.Length; i++)
            {
                int3 chunkIndex = WrapChunkIndex(i);

                Chunk newChunk = Chunk.New(chunkIndex, k_ChunkSize);
                newChunk.transform.SetParent(transform);
                newChunk.transform.localPosition = new Vector3(chunkIndex.x, chunkIndex.y, chunkIndex.z) * k_ChunkSize;

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
            float baseDensity = InvertSurface ? -32 : 32;

            for (int i = 0; i < m_Chunks.Length; i++)
            {
                m_Chunks[i].DensityMap.FillDensityMap(baseDensity);
                foreach (Shape shape in shapeQueue)
                    m_Chunks[i].DensityMap.ApplyShape(shape);
            }

            UpdateAllChunks();
        }

        /// <summary>
        /// Apply a shape at the given position.
        /// </summary>
        public void ApplyShapeAtPosition(Shape shape, Vector3 positionWS)
        {
            ComputeIndices(WorldPositionToIndex(positionWS), out int3 chunkIndex, out _);
            List<int> updateChunks = new();

            // Apply density functions.
            for (int i = 0; i < 27; i++)
            {
                int3 wrappedIndex = chunkIndex + k_AdjacentChunkIndices[i];
                if (wrappedIndex.x < 0 || wrappedIndex.x > m_ChunkDimentions.x - 1 ||
                    wrappedIndex.y < 0 || wrappedIndex.y > m_ChunkDimentions.y - 1 ||
                    wrappedIndex.z < 0 || wrappedIndex.z > m_ChunkDimentions.z - 1)
                    continue;

                int index = FlattenChunkIndex(wrappedIndex);
                m_Chunks[index].DensityMap.ApplyShape(shape);
                updateChunks.Add(index);
            }

            // Update meshes.
            foreach (int index in updateChunks)
                UpdateChunk(index);
        }

        void UpdateAllChunks()
        {
            for (int i = 0; i < m_Chunks.Length; i++)
                UpdateChunk(i);
        }

        void UpdateChunk(int index)
        {
            DensityMap densityMap = m_Chunks[index].DensityMap;
            int3 chunkOriginIndex = WrapChunkIndex(index) * k_ChunkSize;
            Mesh mesh = MeshingMethod switch
            {
                IcosurfaceGenerationMethod.MarchingCubes => MarchingCubes.MakeMesh(this, densityMap, chunkOriginIndex),
                IcosurfaceGenerationMethod.SurfaceNets => SurfaceNets.MakeMesh(this, densityMap, chunkOriginIndex),
                _ => new(),
            };

            m_Chunks[index].SetMesh(mesh);
            m_Chunks[index].SetMaterial(Material);
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
        public Chunk GetChunk(int3 index)
        {
            return m_Chunks[FlattenChunkIndex(index)];
        }
#endif

        public float SampleDensity(int3 index)
        {
            // TODO: Interpolate between corner samples?

            ComputeIndices(index, out int3 chunkIndex, out int3 cellIndex);
            return m_Chunks[FlattenChunkIndex(chunkIndex)].DensityMap.Sample(cellIndex.x, cellIndex.y, cellIndex.z);
        }

        void ComputeIndices(int3 index, out int3 chunkIndex, out int3 cellIndex)
        {
            // Clamp distance to surface bounds.
            float x = Mathf.Clamp(index.x, 0, (m_ChunkDimentions.x * k_ChunkSize) - 0.001f);
            float y = Mathf.Clamp(index.y, 0, (m_ChunkDimentions.y * k_ChunkSize) - 0.001f);
            float z = Mathf.Clamp(index.z, 0, (m_ChunkDimentions.z * k_ChunkSize) - 0.001f);

            // Compute chunk array wrappedIndex.
            chunkIndex.x = Mathf.FloorToInt(x / k_ChunkSize);
            chunkIndex.y = Mathf.FloorToInt(y / k_ChunkSize);
            chunkIndex.z = Mathf.FloorToInt(z / k_ChunkSize);

            // Compute density map wrappedIndex.
            cellIndex.x = Mathf.FloorToInt(x - (chunkIndex.x * k_ChunkSize));
            cellIndex.y = Mathf.FloorToInt(y - (chunkIndex.y * k_ChunkSize));
            cellIndex.z = Mathf.FloorToInt(z - (chunkIndex.z * k_ChunkSize));
        }

        int3 WorldPositionToIndex(Vector3 positionWS)
        {
            Vector3 positionLS = transform.InverseTransformPoint(positionWS);
            int x = Mathf.FloorToInt(positionLS.x);
            int y = Mathf.FloorToInt(positionLS.y);
            int z = Mathf.FloorToInt(positionLS.z);

            return new int3(x, y, z);
        }


        int FlattenChunkIndex(int3 index)
        {
            return (index.z * m_ChunkDimentions.x * m_ChunkDimentions.y) + (index.y * m_ChunkDimentions.x) + index.x;
        }

        int3 WrapChunkIndex(int index)
        {
            int z = index / (m_ChunkDimentions.x * m_ChunkDimentions.y);
            index -= z * m_ChunkDimentions.x * m_ChunkDimentions.y;
            int y = index / m_ChunkDimentions.x;
            int x = index % m_ChunkDimentions.x;

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

    public struct Shape
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
