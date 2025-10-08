using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TerrainGeneration
{
    public partial class ProceduralTerrain : MonoBehaviour
    {
        /*
        struct RecomputeJob : IJobFor
        {
            public NativeArray<Chunk> chunks;

            public void Execute(int index)
            {
                
            }
        }
        */

        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        struct BuildMesh : IJobFor
        {
            NativeList<float3> vertices;
            NativeList<float3> normals;
            NativeList<ushort> triangles;

            public void Execute(int index)
            {
                
            }
        }
    }
}
