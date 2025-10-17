using Unity.Mathematics;

namespace IsosurfaceGeneration.Util
{
    // SDF Primatives
    // See: https://iquilezles.org/articles/distfunctions/
    public struct DistanceFunction
    {
        public static float Sphere(float3 centre, float radius)
        {
            return math.length(centre) - radius;
        }

        public static float SemiSphere(float3 centre, float radius, float h)
        {
            float w = math.sqrt(radius * radius - h * h);

            float2 q = new float2(math.length(centre.xz), centre.y);
            float s = math.max((h - radius) * q.x * q.x + w * w * (h + radius - 2.0f * q.y), h * q.x - w * q.y);
            return (s < 0.0) ? math.length(q) - radius :
                   (q.x < w) ? h - q.y :
                   math.length(q - new float2(w, h));
        }

        public static float Capsule(float3 centre, float height, float radius)
        {
            float3 dir = new(0, height, 0);

            float3 pa = centre - dir;
            float3 ba = -dir - dir;
            float h = math.clamp(math.dot(pa, ba) / math.dot(ba, ba), 0.0f, 1.0f);
            return math.length(pa - ba * h) - radius;
        }

        public static float Torus(float3 centre, float outerRadius, float innerRadius)
        {
            float2 q = new(math.length(centre.xz) - outerRadius, centre.y);
            return math.length(q) - innerRadius;
        }
    }
}
