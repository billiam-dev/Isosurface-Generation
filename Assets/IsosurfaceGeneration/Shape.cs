using IsosurfaceGeneration.Util;
using Unity.Mathematics;

namespace IsosurfaceGeneration
{
    public struct Shape
    {
        public AffineTransform matrix; // worldToLocal
        public AffineTransform inverseMatrix => math.inverse(matrix); // localToWorld
        public ShapeFuncion shapeID;
        public BlendMode blendMode;
        public float sharpness;
        public float dimention1;
        public float dimention2;

        public float Distance(float3 position)
        {
            switch (shapeID)
            {
                case ShapeFuncion.Sphere:
                    return DistanceFunction.Sphere(position, dimention1);
                case ShapeFuncion.SemiSphere:
                    return DistanceFunction.SemiSphere(position, dimention1, dimention2);
                case ShapeFuncion.Capsule:
                    return DistanceFunction.Capsule(position, dimention1, dimention2);
                case ShapeFuncion.Torus:
                    return DistanceFunction.Torus(position, dimention1, dimention2);
                default:
                    return -32;
            }
        }

        public float3 TransformPosition(float3 position)
        {
            float4 transformedPos = math.mul(matrix, new float4(position, 1)); // The input position is in local space, attained via the index of the voxels, so we multiply it by the shape's worldToLocal matrix.
            return new float3(transformedPos.x, transformedPos.y, transformedPos.z);
        }

        /// <summary>
        /// Returns a bounding volume that represents which chunks this shape function will effect.
        /// This is determined by the shape, size and sharpness of the shape.
        /// </summary>
        public int3 ComputeChunkVolume(Isosurface isosurface)
        {
            // Note that even on low sharpness values there exists a point where the SDF has a negligable or no effect.
            // The closer we get to that point the more accurate the terrain will be, but the less performance will be saved.
            // The minimum bounding volume is a 27 chunk area in situations where the shape is less than the size of one chunk.

            float4 boundsVolume = 0;

            // Base size
            switch (shapeID)
            {
                case ShapeFuncion.Sphere:
                    boundsVolume = dimention1 * 2.0f;
                    break;

                case ShapeFuncion.SemiSphere:
                    boundsVolume = dimention1 * 2.0f;
                    break;

                case ShapeFuncion.Capsule:
                case ShapeFuncion.Torus:
                    boundsVolume = (dimention1 + dimention2) * 2.5f;
                    break;
            }

            // m_Sharpness factor
            boundsVolume *= 2.0f / sharpness;

            // TODO
            //boundsVolume.w = 1;
            //boundsVolume = math.mul(math.inverse(matrix), boundsVolume);

            int x = (int)math.ceil(boundsVolume.x / isosurface.ChunkSizeCells);
            int y = (int)math.ceil(boundsVolume.y / isosurface.ChunkSizeCells);
            int z = (int)math.ceil(boundsVolume.z / isosurface.ChunkSizeCells);

            x = math.max(3, x + 1);
            y = math.max(3, y + 1);
            z = math.max(3, z + 1);

            return new int3(x, y, z);
        }
    }

    public enum ShapeFuncion
    {
        Sphere,
        SemiSphere,
        Capsule,
        Torus
    }

    public enum BlendMode
    {
        Additive,
        Subtractive
    }
}
