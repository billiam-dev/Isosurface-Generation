using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainGeneration
{
    public partial class ProceduralTerrain : MonoBehaviour
    {
        Mesh MakeMesh(ProceduralTerrain terrain, DensityMap densityMap, int3 chunkOriginIndex)
        {
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

                    float dx = terrain.SampleDensity(samplePos + offsetX) - terrain.SampleDensity(samplePos - offsetX);
                    float dy = terrain.SampleDensity(samplePos + offsetY) - terrain.SampleDensity(samplePos - offsetY);
                    float dz = terrain.SampleDensity(samplePos + offsetZ) - terrain.SampleDensity(samplePos - offsetZ);

                    return new Vector3(dx, dy, dz).normalized;
                }

                void GetOrMakeVertex(int3 coordA, int3 coordB)
                {
                    // Get density values.
                    float densityA = densityMap.Sample(coordA);
                    float densityB = densityMap.Sample(coordB);

                    // Since the cell size is 1, the index doubles as the position, neat.
                    float3 posA = new(coordA.x, coordA.y, coordA.z);
                    float3 posB = new(coordB.x, coordB.y, coordB.z);

                    // Find interpolated position.
                    float t = (terrain.IsoLevel - densityA) / (densityB - densityA);
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
                    if (densityMap.Sample(cornerCoords[i]) < terrain.IsoLevel)
                        cubeConfiguration |= 1 << i;

                // Exit early if there are no intersections, index.e. the cube is full or empty.
                if (cubeConfiguration == 0 || cubeConfiguration == 0xff)
                    return;

                // Create triangles for current cube configuration.
                for (int i = 0; i < 16; i += 3)
                {
                    // If edge index is -1, then no further vertices exist in this configuration.
                    if (MarchTables.triangulation[cubeConfiguration, i] == -1)
                        break;

                    // Get indices of the two corner points defining the edge that the surface passes through.
                    // (Do this for each of the three edges we're currently looking at).
                    int edgeIndexA = MarchTables.triangulation[cubeConfiguration, i];
                    int a0 = MarchTables.cornerIndexAFromEdge[edgeIndexA];
                    int a1 = MarchTables.cornerIndexBFromEdge[edgeIndexA];

                    int edgeIndexB = MarchTables.triangulation[cubeConfiguration, i + 1];
                    int b0 = MarchTables.cornerIndexAFromEdge[edgeIndexB];
                    int b1 = MarchTables.cornerIndexBFromEdge[edgeIndexB];

                    int edgeIndexC = MarchTables.triangulation[cubeConfiguration, i + 2];
                    int c0 = MarchTables.cornerIndexAFromEdge[edgeIndexC];
                    int c1 = MarchTables.cornerIndexBFromEdge[edgeIndexC];

                    GetOrMakeVertex(cornerCoords[a0], cornerCoords[a1]);
                    GetOrMakeVertex(cornerCoords[b0], cornerCoords[b1]);
                    GetOrMakeVertex(cornerCoords[c0], cornerCoords[c1]);
                }
            }

            // Loop through all cells filling the verticies and triangles lists.
            // Stop one point before the end because each cell includes neighbouring points.
            for (int x = 0; x < densityMap.x - 1; x++)
                for (int y = 0; y < densityMap.y - 1; y++)
                    for (int z = 0; z < densityMap.z - 1; z++)
                        MarchCell(x, y, z);

            Mesh mesh = new();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetNormals(normals);

            return mesh;
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
