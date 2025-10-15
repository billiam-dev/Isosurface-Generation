using Unity.Mathematics;
using UnityEngine;

namespace IsosurfaceGeneration.Util
{
    public static class DistanceFunction
    {
        public static float Sphere(Vector3 centre, float radius)
        {
            return centre.magnitude - radius;
        }

        public static float SmoothMax(float a, float b, float k)
        {
            return math.log(math.exp(k * a) + math.exp(k * b)) / k;
        }

        public static float SmoothMin(float a, float b, float k)
        {
            return -SmoothMax(-a, -b, k);
        }
    }
}
