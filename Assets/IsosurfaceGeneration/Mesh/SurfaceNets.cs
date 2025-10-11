using IsosurfaceGeneration;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// Resources:
// https://medium.com/@ryandremer/implementing-surface-nets-in-godot-f48ecd5f29ff
// https://github.com/bigos91/fastNaiveSurfaceNets/

public static class SurfaceNets
{
    public static Mesh MakeMesh(Isosurface surface, DensityMap densityMap, int3 chunkOriginIndex)
    {
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<int> triangles = new();

        int triangleIndex = 0;
        Dictionary<int3, int> vertexIndexMap = new();

        Vector3 Int3ToVector3(int3 i)
        {
            return new Vector3(i.x, i.y, i.z);
        }

        Vector3 CalculateNormal(int3 index)
        {
            int3 samplePos = chunkOriginIndex + index;

            float dx = surface.SampleDensity(samplePos - k_Axis[0]) - surface.SampleDensity(samplePos + k_Axis[0]);
            float dy = surface.SampleDensity(samplePos - k_Axis[1]) - surface.SampleDensity(samplePos + k_Axis[1]);
            float dz = surface.SampleDensity(samplePos - k_Axis[2]) - surface.SampleDensity(samplePos + k_Axis[2]);

            return new Vector3(dx, dy, dz).normalized;
        }

        Vector3 CalculateSurfacePosition(int3 index)
        {
            Vector3 position = Vector3.zero;
            int surfaceEdgeCount = 0;

            for (int i = 0; i < k_Edges.Length; i++)
            {
                int3[] edgeOffsets = k_Edges[i];

                Vector3 positionA = Int3ToVector3(index + edgeOffsets[0]);
                Vector3 positionB = Int3ToVector3(index + edgeOffsets[1]);

                float densityA = surface.SampleDensity(chunkOriginIndex + index + edgeOffsets[0]);
                float densityB = surface.SampleDensity(chunkOriginIndex + index + edgeOffsets[1]);

                if (densityA * densityB <= 0)
                {
                    position += Vector3.Lerp(positionA, positionB, Mathf.Abs(densityA) / (Mathf.Abs(densityA) + Mathf.Abs(densityB)));
                    surfaceEdgeCount++;
                }
            }

            if (surfaceEdgeCount == 0)
                return new Vector3(index.x, index.y, index.z) + (Vector3.one * 0.5f);

            return position / surfaceEdgeCount;
        }

        void GetOrMakeVertex(int3 index)
        {
            if (vertexIndexMap.TryGetValue(index, out int sharedVertexIndex))
            {
                triangles.Add(sharedVertexIndex);
            }
            else
            {
                vertexIndexMap.Add(index, triangleIndex);
                vertices.Add(CalculateSurfacePosition(index));
                normals.Add(CalculateNormal(index));
                triangles.Add(triangleIndex++);
            }
        }

        void MakeQuad(int3 index, int axisIndex)
        {
            int3[] points = new int3[4]
            {
                    index + k_QuadPoints[axisIndex][0],
                    index + k_QuadPoints[axisIndex][1],
                    index + k_QuadPoints[axisIndex][2],
                    index + k_QuadPoints[axisIndex][3]
            };

            GetOrMakeVertex(points[0]);
            GetOrMakeVertex(points[1]);
            GetOrMakeVertex(points[2]);

            GetOrMakeVertex(points[2]);
            GetOrMakeVertex(points[3]);
            GetOrMakeVertex(points[0]);
        }

        void MakeQuad_Reversed(int3 index, int axisIndex)
        {
            int3[] points = new int3[4]
            {
                    index + k_QuadPoints[axisIndex][0],
                    index + k_QuadPoints[axisIndex][1],
                    index + k_QuadPoints[axisIndex][2],
                    index + k_QuadPoints[axisIndex][3]
            };

            GetOrMakeVertex(points[2]);
            GetOrMakeVertex(points[1]);
            GetOrMakeVertex(points[0]);

            GetOrMakeVertex(points[0]);
            GetOrMakeVertex(points[3]);
            GetOrMakeVertex(points[2]);
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
                    MakeQuad(index, i);
                else if (d1 >= 0 && d2 < 0)
                    MakeQuad_Reversed(index, i);
            }
        }

        for (int x = 0; x < densityMap.sizeX - 1; x++)
            for (int y = 0; y < densityMap.sizeY - 1; y++)
                for (int z = 0; z < densityMap.sizeZ - 1; z++)
                    MarchCell(new int3(x, y, z));

        Mesh mesh = new();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, true);
        mesh.SetNormals(normals);

        return mesh;
    }

    // The 12 edges of a cube. Used to interpolate a point onto the estimated surface of the density map.
    static readonly int3[][] k_Edges = new int3[12][]
    {
        // Edges on min Z axis
        new int3[2] { new(0, 0, 0), new(1, 0, 0) },
        new int3[2] { new(1, 0, 0), new(1, 1, 0) },
        new int3[2] { new(1, 1, 0), new(0, 1, 0) },
        new int3[2] { new(0, 1, 0), new(0, 0, 0) },

        // Edges on max Z axis
        new int3[2] { new(0, 0, 1), new(1, 0, 1) },
        new int3[2] { new(1, 0, 1), new(1, 1, 1) },
        new int3[2] { new(1, 1, 1), new(0, 1, 1) },
        new int3[2] { new(0, 1, 1), new(0, 0, 1) },

        // Edges connecting min Z to max Z
        new int3[2] { new(0, 0, 0), new(0, 0, 1) },
        new int3[2] { new(1, 0, 0), new(1, 0, 1) },
        new int3[2] { new(1, 1, 0), new(1, 1, 1) },
        new int3[2] { new(0, 1, 0), new(0, 1, 1) }
    };

    // The 4 relative indexes of the corners of a Quad that is orthogonal to each axis.
    static readonly int3[][] k_QuadPoints = new int3[3][]
    {
            new int3[4]
            {
                new(0, 0, -1),
                new(0, -1, -1),
                new(0, -1, 0),
                new(0, 0, 0)
            },
            new int3[4]
            {
                new(0, 0, -1),
                new(0, 0, 0),
                new(-1, 0, 0),
                new(-1, 0, -1)
            },
            new int3[4]
            {
                new(0, 0, 0),
                new(0, -1, 0),
                new(-1, -1, 0),
                new(-1, 0, 0)
            }
    };

    static readonly int3[] k_Axis = new int3[3]
    {
            new(1, 0, 0),
            new(0, 1, 0),
            new(0, 0, 1)
    };
}
