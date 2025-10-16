using Unity.Mathematics;

namespace IsosurfaceGeneration.Util
{
    public struct DistanceFunction
    {
        public static float Sphere(float3 centre, float radius)
        {
            return math.length(centre) - radius;
        }
    }
}
