using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace IsosurfaceGeneration.Meshing
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float3 position;
        public float3 normal;

        public Vertex(float3 position, float3 normal)
        {
            this.position = position;
            this.normal = normal;
        }

        public static readonly VertexAttributeDescriptor[] Format = {
            new(VertexAttribute.Position, VertexAttributeFormat.Float32),
            new(VertexAttribute.Normal, VertexAttributeFormat.Float32)
        };
    }
}
