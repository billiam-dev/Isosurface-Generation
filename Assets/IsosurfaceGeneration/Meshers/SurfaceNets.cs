using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// Resources:
// https://medium.com/@ryandremer/implementing-surface-nets-in-godot-f48ecd5f29ff
// https://github.com/bigos91/fastNaiveSurfaceNets/

namespace IsosurfaceGeneration
{
    #region Single Thread Mesher
    public struct SurfaceNetsMesher
    {
        public Isosurface surface;
        public DensityMap densityMap;
        public int3 chunkOriginIndex;

        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<int> triangles;

        int triangleIndex;
        Dictionary<int3, int> vertexIndexMap;

        public void Execute()
        {
            triangleIndex = 0;
            vertexIndexMap = new();

            // Loop through all cells filling the verticies and indices lists.
            for (int i = 0; i < densityMap.totalPoints; i++)
            {
                int3 unwrappedIndex = IndexHelper.Unwrap(i, densityMap.pointsPerAxis);

                // Stop one point before the end because each cell includes neighbouring points.
                if (unwrappedIndex.x >= densityMap.pointsPerAxis - 1 ||
                    unwrappedIndex.y >= densityMap.pointsPerAxis - 1 ||
                    unwrappedIndex.z >= densityMap.pointsPerAxis - 1)
                    continue;

                MarchCell(unwrappedIndex);
            }
        }

        void MarchCell(int3 index)
        {
            // Check the three neighboring points in each positive Axis.
            // If the sign of the current point and the neighboring point is different, the voxel intersects the surface.
            for (int i = 0; i < 3; i++)
            {
                float d1 = densityMap.Sample(index);
                float d2 = densityMap.Sample(index + NetsTables.Axis[i]);

                if (d1 < 0 && d2 >= 0)
                    MakeQuad(index, i);
                else if (d1 >= 0 && d2 < 0)
                    MakeQuad_Reversed(index, i);
            }
        }

        void MakeQuad(int3 index, int axisIndex)
        {
            int3[] points = new int3[4]
            {
                index + NetsTables.QuadPoints[axisIndex][0],
                index + NetsTables.QuadPoints[axisIndex][1],
                index + NetsTables.QuadPoints[axisIndex][2],
                index + NetsTables.QuadPoints[axisIndex][3]
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
                index + NetsTables.QuadPoints[axisIndex][0],
                index + NetsTables.QuadPoints[axisIndex][1],
                index + NetsTables.QuadPoints[axisIndex][2],
                index + NetsTables.QuadPoints[axisIndex][3]
            };

            GetOrMakeVertex(points[2]);
            GetOrMakeVertex(points[1]);
            GetOrMakeVertex(points[0]);

            GetOrMakeVertex(points[0]);
            GetOrMakeVertex(points[3]);
            GetOrMakeVertex(points[2]);
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

        Vector3 CalculateSurfacePosition(int3 index)
        {
            Vector3 position = Vector3.zero;
            int surfaceEdgeCount = 0;

            for (int i = 0; i < NetsTables.Edges.Length; i++)
            {
                int3[] edgeOffsets = NetsTables.Edges[i];

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

        Vector3 CalculateNormal(int3 index)
        {
            int3 samplePos = chunkOriginIndex + index;

            float dx = surface.SampleDensity(samplePos - NetsTables.Axis[0]) - surface.SampleDensity(samplePos + NetsTables.Axis[0]);
            float dy = surface.SampleDensity(samplePos - NetsTables.Axis[1]) - surface.SampleDensity(samplePos + NetsTables.Axis[1]);
            float dz = surface.SampleDensity(samplePos - NetsTables.Axis[2]) - surface.SampleDensity(samplePos + NetsTables.Axis[2]);

            return new Vector3(dx, dy, dz).normalized;
        }

        Vector3 Int3ToVector3(int3 i)
        {
            return new Vector3(i.x, i.y, i.z);
        }
    }
    #endregion

    #region Tables
    readonly struct NetsTables
    {
        // The 12 edges of a cube. Used to interpolate a point onto the estimated surface of the density map.
        public static readonly int3[][] Edges = new int3[12][]
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
        public static readonly int3[][] QuadPoints = new int3[3][]
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

        public static readonly int3[] Axis = new int3[3]
        {
                new(1, 0, 0),
                new(0, 1, 0),
                new(0, 0, 1)
        };
    }
    #endregion
}
