using IsosurfaceGeneration.Util;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// Resources:
// https://medium.com/@ryandremer/implementing-surface-nets-in-godot-f48ecd5f29ff
// https://github.com/bigos91/fastNaiveSurfaceNets/

namespace IsosurfaceGeneration.Meshing
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
            int3 point1 = index + NetsTables.QuadPoints[axisIndex][0];
            int3 point2 = index + NetsTables.QuadPoints[axisIndex][1];
            int3 point3 = index + NetsTables.QuadPoints[axisIndex][2];
            int3 point4 = index + NetsTables.QuadPoints[axisIndex][3];

            GetOrMakeVertex(point1);
            GetOrMakeVertex(point2);
            GetOrMakeVertex(point3);

            GetOrMakeVertex(point3);
            GetOrMakeVertex(point4);
            GetOrMakeVertex(point1);
        }

        void MakeQuad_Reversed(int3 index, int axisIndex)
        {
            int3 point1 = index + NetsTables.QuadPoints[axisIndex][0];
            int3 point2 = index + NetsTables.QuadPoints[axisIndex][1];
            int3 point3 = index + NetsTables.QuadPoints[axisIndex][2];
            int3 point4 = index + NetsTables.QuadPoints[axisIndex][3];

            GetOrMakeVertex(point3);
            GetOrMakeVertex(point2);
            GetOrMakeVertex(point1);

            GetOrMakeVertex(point1);
            GetOrMakeVertex(point4);
            GetOrMakeVertex(point3);
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

        float3 CalculateSurfacePosition(int3 coord)
        {
            float3 position = 0.0f;
            int surfaceEdgeCount = 0;

            for (int i = 0; i < NetsTables.Edges.Length; i++)
            {
                int3[] edgeOffsets = NetsTables.Edges[i];

                float3 positionA = coord + edgeOffsets[0];
                float3 positionB = coord + edgeOffsets[1];

                float densityA = surface.SampleDensity(chunkOriginIndex + coord + edgeOffsets[0]);
                float densityB = surface.SampleDensity(chunkOriginIndex + coord + edgeOffsets[1]);

                if (densityA * densityB <= 0)
                {
                    position += math.lerp(positionA, positionB, math.abs(densityA) / (math.abs(densityA) + math.abs(densityB)));
                    surfaceEdgeCount++;
                }
            }

            if (surfaceEdgeCount == 0)
                return (float3)coord + 0.5f;

            return position / surfaceEdgeCount;
        }

        float3 CalculateNormal(int3 coord)
        {
            int3 samplePos = chunkOriginIndex + coord;

            float3 normal;
            normal.x = surface.SampleDensity(samplePos - NetsTables.Axis[0]) - surface.SampleDensity(samplePos + NetsTables.Axis[0]);
            normal.y = surface.SampleDensity(samplePos - NetsTables.Axis[1]) - surface.SampleDensity(samplePos + NetsTables.Axis[1]);
            normal.z = surface.SampleDensity(samplePos - NetsTables.Axis[2]) - surface.SampleDensity(samplePos + NetsTables.Axis[2]);

            return math.normalize(normal);
        }
    }
    #endregion

    #region Jobs Mesher
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct SurfaceNetsMesherJob : IJobFor
    {
        [ReadOnly] public NativeArray<float> density;
        [ReadOnly] public int densityPPA;
        [ReadOnly] public int itteratePPA;
        [ReadOnly] public float isoLevel;

        [NativeDisableParallelForRestriction, WriteOnly] public NativeList<Vertex> vertices;
        [NativeDisableParallelForRestriction, WriteOnly] public NativeList<ushort> indices;

        public NativeCounter vertexCounter;
        [NativeDisableParallelForRestriction] public NativeHashMap<int3, ushort> vertexIndexMap;

        public void Execute(int index)
        {
            int3 unwrappedIndex = IndexHelper.Unwrap(index, itteratePPA);
            index = IndexHelper.Wrap(unwrappedIndex, densityPPA);
            index += (densityPPA * densityPPA) + densityPPA + 1;

            MarchCell(index);
        }

        void MarchCell(int index)
        {
            CellAxis<int> cellAxis = new()
            {
                axis1 = index + 1,
                axis2 = index + densityPPA,
                axis3 = index + (densityPPA * densityPPA),
            };

            // Check the three neighboring points in each positive Axis.
            // If the sign of the current point and the neighboring point is different, the voxel intersects the surface.
            for (int i = 0; i < 3; i++)
            {
                float d1 = density[index];
                float d2 = density[cellAxis[i]];

                int3 unwrappedIndex = IndexHelper.Unwrap(index, densityPPA);
                if (d1 < 0 && d2 >= 0)
                    MakeQuad(unwrappedIndex, i);
                else if (d1 >= 0 && d2 < 0)
                    MakeQuad_Reversed(unwrappedIndex, i);
            }
        }

        void MakeQuad(int3 index, int axisIndex)
        {
            int3 point1 = index + NetsTables.QuadPoints[axisIndex][0];
            int3 point2 = index + NetsTables.QuadPoints[axisIndex][1];
            int3 point3 = index + NetsTables.QuadPoints[axisIndex][2];
            int3 point4 = index + NetsTables.QuadPoints[axisIndex][3];

            GetOrMakeVertex(point1);
            GetOrMakeVertex(point2);
            GetOrMakeVertex(point3);

            GetOrMakeVertex(point3);
            GetOrMakeVertex(point4);
            GetOrMakeVertex(point1);
        }

        void MakeQuad_Reversed(int3 index, int axisIndex)
        {
            int3 point1 = index + NetsTables.QuadPoints[axisIndex][0];
            int3 point2 = index + NetsTables.QuadPoints[axisIndex][1];
            int3 point3 = index + NetsTables.QuadPoints[axisIndex][2];
            int3 point4 = index + NetsTables.QuadPoints[axisIndex][3];

            GetOrMakeVertex(point3);
            GetOrMakeVertex(point2);
            GetOrMakeVertex(point1);

            GetOrMakeVertex(point1);
            GetOrMakeVertex(point4);
            GetOrMakeVertex(point3);
        }

        void GetOrMakeVertex(int3 index)
        {
            if (vertexIndexMap.TryGetValue(index, out ushort sharedVertexIndex))
            {
                indices.Add(sharedVertexIndex);
            }
            else
            {
                ushort vertexIndex = (ushort)vertexCounter.Count;
                vertexIndexMap.Add(index, vertexIndex);
                indices.Add(vertexIndex);
                vertices.Add(new Vertex(CalculateSurfacePosition(index), CalculateNormal(index)));
                vertexCounter.Increment();
            }
        }

        float3 CalculateSurfacePosition(int3 coord)
        {
            float3 position = 0;
            int surfaceEdgeCount = 0;

            for (int i = 0; i < NetsTables.Edges.Length; i++)
            {
                int3[] edgeOffsets = NetsTables.Edges[i];

                float3 positionA = coord + edgeOffsets[0];
                float3 positionB = coord + edgeOffsets[1];

                float densityA = density[IndexHelper.Wrap(coord + edgeOffsets[0], densityPPA)];
                float densityB = density[IndexHelper.Wrap(coord + edgeOffsets[1], densityPPA)];

                if (densityA * densityB <= 0)
                {
                    position += math.lerp(positionA, positionB, math.abs(densityA) / (math.abs(densityA) + math.abs(densityB)));
                    surfaceEdgeCount++;
                }
            }

            if (surfaceEdgeCount == 0)
                return (float3)coord + 0.5f;

            return position / surfaceEdgeCount;
        }

        float3 CalculateNormal(int3 coord)
        {
            float3 normal;
            normal.x = density[IndexHelper.Wrap(coord - NetsTables.Axis[0], densityPPA)] - density[IndexHelper.Wrap(coord + NetsTables.Axis[0], densityPPA)];
            normal.y = density[IndexHelper.Wrap(coord - NetsTables.Axis[1], densityPPA)] - density[IndexHelper.Wrap(coord + NetsTables.Axis[1], densityPPA)];
            normal.z = density[IndexHelper.Wrap(coord - NetsTables.Axis[2], densityPPA)] - density[IndexHelper.Wrap(coord + NetsTables.Axis[2], densityPPA)];

            return math.normalize(normal);
        }
    }

    struct CellAxis<T>
    {
        public T axis1;
        public T axis2;
        public T axis3;

        public T this[int index]
        {
            get
            {
                return index switch
                {
                    0 => axis1,
                    1 => axis2,
                    2 => axis3,
                    _ => throw new System.IndexOutOfRangeException(),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        axis1 = value;
                        break;
                    case 1:
                        axis2 = value;
                        break;
                    case 2:
                        axis3 = value;
                        break;
                    default: throw new System.IndexOutOfRangeException();
                }
            }
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
