using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration
{
    public partial class Isosurface : MonoBehaviour
    {
        #region Surface Nets
        // The 4 relative indexes of the corners of a Quad that is orthogonal to each axis.
        readonly int3[][] k_QuadPoints = new int3[3][]
        {
            new int3[4]
            {
                new(1, 1, 0),
                new(1, 0, 0),
                new(1, 0, 1),
                new(1, 1, 1)
            },
            new int3[4]
            {
                new(1, 1, 0),
                new(1, 1, 1),
                new(0, 1, 1),
                new(0, 1, 0)
            },
            new int3[4]
            {
                new(1, 1, 1),
                new(1, 0, 1),
                new(0, 0, 1),
                new(0, 1, 1)
            }
        };

        readonly int3[] k_Axis = new int3[3]
        {
            new(1, 0, 0),
            new(0, 1, 0),
            new(0, 0, 1)
        };

        Mesh MakeMesh_SurfaceNets(DensityMap densityMap, int3 chunkOriginIndex)
        {
            // Resources:
            // https://medium.com/@ryandremer/implementing-surface-nets-in-godot-f48ecd5f29ff
            // https://github.com/bigos91/fastNaiveSurfaceNets/

            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<int> triangles = new();

            int triangleIndex = 0;

            void GetOrMakeVertex(int3 index, Vector3 normal)
            {
                Vector3 position = new(index.x, index.y, index.z);

                vertices.Add(position);
                normals.Add(normal);
                triangles.Add(triangleIndex++);
            }

            void MakeQuad(int3 index, int axisIndex, bool reversed)
            {
                int3[] points = new int3[4]
                {
                    index + k_QuadPoints[axisIndex][0],
                    index + k_QuadPoints[axisIndex][1],
                    index + k_QuadPoints[axisIndex][2],
                    index + k_QuadPoints[axisIndex][3]
                };

                int3 axis = k_Axis[axisIndex];
                Vector3 normal = new(axis.x, axis.y, axis.z);

                if (reversed)
                {
                    GetOrMakeVertex(points[0], -normal);
                    GetOrMakeVertex(points[1], -normal);
                    GetOrMakeVertex(points[2], -normal);

                    GetOrMakeVertex(points[2], -normal);
                    GetOrMakeVertex(points[3], -normal);
                    GetOrMakeVertex(points[0], -normal);
                }
                else
                {
                    GetOrMakeVertex(points[2], normal);
                    GetOrMakeVertex(points[1], normal);
                    GetOrMakeVertex(points[0], normal);

                    GetOrMakeVertex(points[0], normal);
                    GetOrMakeVertex(points[3], normal);
                    GetOrMakeVertex(points[2], normal);
                }
            }

            void MarchCell(int3 index)
            {
                // Check the three neighboring points in each positive k_Axis.
                // If the sign of the current point and the neighboring point is different, the voxel intersects the surface.
                for (int i = 0; i < 3; i++)
                {
                    float d1 = densityMap.Sample(index);
                    float d2 = densityMap.Sample(index + k_Axis[i]);

                    if (d1 < 0 && d2 >= 0)
                        MakeQuad(index, i, false);
                    else if (d1 >= 0 && d2 < 0)
                        MakeQuad(index, i, true);
                }
            }

            for (int x = 0; x < densityMap.sizeX - 1; x++)
                for (int y = 0; y < densityMap.sizeY - 1; y++)
                    for (int z = 0; z < densityMap.sizeZ - 1; z++)
                        MarchCell(new int3(x, y, z));

            Mesh mesh = new();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateNormals();

            return mesh;
        }
        #endregion

        #region Marching Cubes
        Mesh MakeMesh_MarchingCubes(DensityMap densityMap, int3 chunkOriginIndex)
        {
            // Resources:
            // https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
            // https://github.com/Fobri/Terraxel-Unity

            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<int> triangles = new();

            int triangleIndex = 0;
            Dictionary<int2, int> vertexIndexMap = new();

            void MarchCell(int x, int y, int z)
            {
                Vector3 CalculateNormal(int3 index)
                {
                    int3 samplePos = chunkOriginIndex + index;

                    int3 offsetX = new(1, 0, 0);
                    int3 offsetY = new(0, 1, 0);
                    int3 offsetZ = new(0, 0, 1);

                    float dx = SampleDensity(samplePos + offsetX) - SampleDensity(samplePos - offsetX);
                    float dy = SampleDensity(samplePos + offsetY) - SampleDensity(samplePos - offsetY);
                    float dz = SampleDensity(samplePos + offsetZ) - SampleDensity(samplePos - offsetZ);

                    return new Vector3(dx, dy, dz).normalized;
                }

                void GetOrMakeVertex(int3 coordA, int3 coordB)
                {
                    // Get density values.
                    float densityA = densityMap.Sample(coordA);
                    float densityB = densityMap.Sample(coordB);

                    // Since the cell sizeInWorld is 1, the index doubles as the position, neat.
                    float3 posA = new(coordA.x, coordA.y, coordA.z);
                    float3 posB = new(coordB.x, coordB.y, coordB.z);

                    // Find interpolated position.
                    float t = (IsoLevel - densityA) / (densityB - densityA);
                    float3 position = posA + t * (posB - posA);

                    // Normal
                    float3 normalA = CalculateNormal(coordA);
                    float3 normalB = CalculateNormal(coordB);
                    float3 normal = Vector3.Normalize(normalA + t * (normalB - normalA));
                    
                    // ID (for de-duplication)
                    int indexA = densityMap.FlattenIndex(coordA);
                    int indexB = densityMap.FlattenIndex(coordB);
                    int2 id = new(Mathf.Min(indexA, indexB), Mathf.Max(indexA, indexB));

                    if (vertexIndexMap.TryGetValue(id, out int sharedVertexIndex))
                    {
                        triangles.Add(sharedVertexIndex);
                    }
                    else
                    {
                        vertexIndexMap.Add(id, triangleIndex);
                        vertices.Add(position);
                        normals.Add(normal);
                        triangles.Add(triangleIndex++);
                    }
                }

                // Calculate coordinates of each corner of the current cell.
                int3 index = new(x, y, z);
                int3[] cornerCoords = new int3[8] {
                    index + new int3(0, 0, 0),
                    index + new int3(1, 0, 0),
                    index + new int3(1, 0, 1),
                    index + new int3(0, 0, 1),
                    index + new int3(0, 1, 0),
                    index + new int3(1, 1, 0),
                    index + new int3(1, 1, 1),
                    index + new int3(0, 1, 1)
                };

                // Calculate unique index for each cube configuration.
                // The value is used to look up the edge table, which indicates which edges of the cube the surface passes through.
                int cubeConfiguration = 0;
                for (int i = 0; i < 8; i++)
                    if (densityMap.Sample(cornerCoords[i]) < IsoLevel)
                        cubeConfiguration |= 1 << i;

                // Exit early if there are no intersections, index.e. the cube is full or empty.
                if (cubeConfiguration == 0 || cubeConfiguration == 0xff)
                    return;

                // Create triangles for current cube configuration.
                int[] edgeIndices = MarchTables.triangulation[cubeConfiguration];
                for (int i = 0; i < 16; i += 3)
                {
                    // If edge index is -1, then no further vertices exist in this configuration.
                    if (edgeIndices[i] == -1)
                        break;

                    // Get indices of the two corner points defining the edge that the surface passes through.
                    // (Do this for each of the three edges we're currently looking at).
                    int edgeIndexA = edgeIndices[i];
                    int a0 = MarchTables.cornerIndexAFromEdge[edgeIndexA];
                    int a1 = MarchTables.cornerIndexBFromEdge[edgeIndexA];

                    int edgeIndexB = edgeIndices[i + 1];
                    int b0 = MarchTables.cornerIndexAFromEdge[edgeIndexB];
                    int b1 = MarchTables.cornerIndexBFromEdge[edgeIndexB];

                    int edgeIndexC = edgeIndices[i + 2];
                    int c0 = MarchTables.cornerIndexAFromEdge[edgeIndexC];
                    int c1 = MarchTables.cornerIndexBFromEdge[edgeIndexC];

                    GetOrMakeVertex(cornerCoords[a0], cornerCoords[a1]);
                    GetOrMakeVertex(cornerCoords[b0], cornerCoords[b1]);
                    GetOrMakeVertex(cornerCoords[c0], cornerCoords[c1]);
                }
            }

            // Loop through all cells filling the verticies and triangles lists.
            // Stop one point before the end because each cell includes neighbouring points.
            for (int x = 0; x < densityMap.sizeX - 1; x++)
                for (int y = 0; y < densityMap.sizeY - 1; y++)
                    for (int z = 0; z < densityMap.sizeZ - 1; z++)
                        MarchCell(x, y, z);

            Mesh mesh = new();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetNormals(normals);

            return mesh;
        }
        #endregion

        // 3D to 1D
        int FlattenChunkIndex(int3 index)
        {
            return (index.z * m_ChunkDimentions.x * m_ChunkDimentions.y) + (index.y * m_ChunkDimentions.x) + index.x;
        }

        // 1D to 3D
        int3 WrapChunkIndex(int index)
        {
            int z = index / (m_ChunkDimentions.x * m_ChunkDimentions.y);
            index -= z * m_ChunkDimentions.x * m_ChunkDimentions.y;
            int y = index / m_ChunkDimentions.x;
            int x = index % m_ChunkDimentions.x;

            return new int3(x, y, z);
        }

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
}
